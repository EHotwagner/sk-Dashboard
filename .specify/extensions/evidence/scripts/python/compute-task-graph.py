#!/usr/bin/env python3
"""
compute-task-graph.py — DAG compute + synthetic-evidence propagation.

Parses specs/<feature>/tasks.md and specs/<feature>/tasks.deps.yml, builds
the dependency graph, validates it (acyclic, no dangling refs, every id
present in both files), computes the effective status per task under the
propagation rule, and renders task-graph.json and task-graph.md under
specs/<feature>/readiness/.

Propagation rule:
  effective(T) =
    'S'  if declared(T) == 'S'
    'S*' if declared(T) == 'X' and any d in deps(T): effective(d) in {'S','S*'}
    declared(T) otherwise

Phase-checkpoint edges are auto-injected: every task in Phase N+1 implicitly
depends on the last foundation task of Phase N (the task right before the
"Checkpoint:" line in Phase N). Authors only write non-phase cross-edges
in tasks.deps.yml.

Exit codes:
  0 — graph is valid and readiness artifacts written
  2 — validation failure (cycles, dangling refs, missing ids)
  3 — filesystem / parse failure

Usage:
  compute-task-graph.py <feature-dir>
  compute-task-graph.py specs/042-user-signup

Dependencies: stdlib only. No pyyaml requirement — we parse the simple
id -> [ids] YAML shape ourselves.
"""

from __future__ import annotations

import json
import re
import sys
from collections import defaultdict, deque
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

# ----------------------------------------------------------------------------
# Data model
# ----------------------------------------------------------------------------

TASK_ID_RE = re.compile(r"\bT\d{3,4}\b")

# Matches a task line: "- [ ] T015 [P] [US1] Implement ..."
# Captures: box content, id, rest-of-line
TASK_LINE_RE = re.compile(
    r"""^\s*
        -\s*\[(?P<box>[ X\-FS*])\]\s+
        (?P<id>T\d{3,4})\b
        (?P<rest>.*)$
    """,
    re.VERBOSE,
)

PHASE_HEADER_RE = re.compile(r"^\s*##\s+Phase\s+(\d+)\s*:", re.IGNORECASE)
CHECKPOINT_RE = re.compile(r"^\s*\*\*Checkpoint", re.IGNORECASE)

# Parsed box char → declared status
BOX_TO_STATUS = {
    " ": "pending",
    "X": "done",
    "S": "synthetic",
    "F": "failed",
    "-": "skipped",
    "*": "invalid",  # "[*]" shouldn't appear; S* is computed
}


@dataclass
class Task:
    id: str
    declared: str  # pending | done | synthetic | failed | skipped
    effective: str = ""  # filled by propagation; adds 'auto-synthetic'
    phase: Optional[int] = None
    story: Optional[str] = None  # "US1" etc.
    tier: Optional[str] = None   # "T1" or "T2"
    parallel: bool = False       # [P] annotation
    line_no: int = 0
    title: str = ""
    explicit_deps: List[str] = field(default_factory=list)
    phase_deps: List[str] = field(default_factory=list)

    @property
    def all_deps(self) -> List[str]:
        # Explicit first, then phase-edges, deduped, preserving order.
        seen: Set[str] = set()
        result: List[str] = []
        for d in self.explicit_deps + self.phase_deps:
            if d not in seen:
                seen.add(d)
                result.append(d)
        return result


@dataclass
class GraphResult:
    tasks: Dict[str, Task]
    errors: List[str]
    warnings: List[str]
    root_cause: Dict[str, List[str]]  # task_id -> list of upstream [S] ids

    @property
    def has_errors(self) -> bool:
        return len(self.errors) > 0


# ----------------------------------------------------------------------------
# Parsers
# ----------------------------------------------------------------------------

def parse_tasks_md(path: Path) -> Tuple[Dict[str, Task], List[str]]:
    """Parse tasks.md into id → Task, plus phase structure for edge injection."""
    errors: List[str] = []
    tasks: Dict[str, Task] = {}
    current_phase: Optional[int] = None
    # Phase -> list of task ids in order (pre-checkpoint); used to compute the
    # "last foundation task" for the next phase's implicit edges.
    phase_tasks: Dict[int, List[str]] = defaultdict(list)
    # Phase -> list of task ids seen BEFORE the first Checkpoint marker.
    phase_foundation: Dict[int, List[str]] = defaultdict(list)
    # Whether we've already passed the first checkpoint in the current phase.
    past_checkpoint = False

    try:
        text = path.read_text(encoding="utf-8")
    except OSError as e:
        return {}, [f"Cannot read {path}: {e}"]

    for i, line in enumerate(text.splitlines(), start=1):
        phase_m = PHASE_HEADER_RE.match(line)
        if phase_m:
            current_phase = int(phase_m.group(1))
            past_checkpoint = False
            continue

        if CHECKPOINT_RE.match(line):
            past_checkpoint = True
            continue

        task_m = TASK_LINE_RE.match(line)
        if not task_m:
            continue

        box = task_m.group("box")
        tid = task_m.group("id")
        rest = task_m.group("rest").strip()

        if box == "*":
            errors.append(
                f"tasks.md:{i}: invalid status [*] on {tid} — "
                f"[S*] is computed, never written manually"
            )
            continue

        declared = BOX_TO_STATUS.get(box, "pending")

        # Extract annotations from the rest of the line.
        parallel = bool(re.search(r"\[P\]", rest))
        story_m = re.search(r"\[(US\d+)\]", rest)
        tier_m = re.search(r"\[(T[12])\]", rest)

        # Title: strip all [..] annotation brackets, keep the sentence.
        title = re.sub(r"\[(P|US\d+|T[12])\]\s*", "", rest).strip()

        if tid in tasks:
            errors.append(f"tasks.md:{i}: duplicate task id {tid}")
            continue

        task = Task(
            id=tid,
            declared=declared,
            effective=declared,
            phase=current_phase,
            story=story_m.group(1) if story_m else None,
            tier=tier_m.group(1) if tier_m else None,
            parallel=parallel,
            line_no=i,
            title=title,
        )
        tasks[tid] = task

        if current_phase is not None:
            phase_tasks[current_phase].append(tid)
            if not past_checkpoint:
                phase_foundation[current_phase].append(tid)

    # Inject phase-checkpoint edges.
    # For each task in Phase N, add an implicit dep on the LAST task of
    # Phase N-1's foundation block (the last task before the first Checkpoint).
    # If Phase N-1 had no checkpoint, use its last task overall.
    sorted_phases = sorted(phase_tasks.keys())
    for idx, phase in enumerate(sorted_phases):
        if idx == 0:
            continue  # no predecessor
        prev_phase = sorted_phases[idx - 1]
        foundation = phase_foundation.get(prev_phase) or phase_tasks.get(prev_phase, [])
        if not foundation:
            continue
        anchor = foundation[-1]
        for tid in phase_tasks[phase]:
            if tid != anchor and anchor in tasks:
                tasks[tid].phase_deps.append(anchor)

    return tasks, errors


def parse_deps_yml(path: Path) -> Tuple[Dict[str, List[str]], List[str]]:
    """Parse the minimal 'id: [ids]' YAML without pyyaml.

    Supports:
        tasks:
          T001: []
          T015: [T011, T013]
          T022:
            - T019
            - T021
    """
    errors: List[str] = []
    deps: Dict[str, List[str]] = {}

    try:
        text = path.read_text(encoding="utf-8")
    except OSError as e:
        return {}, [f"Cannot read {path}: {e}"]

    in_tasks = False
    current_id: Optional[str] = None

    inline_re = re.compile(
        r"^\s{2,}(T\d{3,4})\s*:\s*(?:\[\s*(?P<bracket>.*?)\s*\])?\s*$"
    )
    list_item_re = re.compile(r"^\s{4,}-\s*(T\d{3,4})\s*$")

    for i, raw in enumerate(text.splitlines(), start=1):
        line = raw.rstrip()
        if not line or line.lstrip().startswith("#"):
            continue

        if re.match(r"^\s*tasks\s*:\s*$", line):
            in_tasks = True
            current_id = None
            continue

        # Top-level key that isn't 'tasks:' ends the tasks section.
        if in_tasks and re.match(r"^[A-Za-z_]", line):
            in_tasks = False
            current_id = None
            continue

        if not in_tasks:
            continue

        m = inline_re.match(line)
        if m:
            tid = m.group(1)
            bracket = m.group("bracket")
            if tid in deps:
                errors.append(f"tasks.deps.yml:{i}: duplicate key {tid}")
            if bracket is None:
                # Block form; subsequent list items will populate.
                deps[tid] = []
                current_id = tid
            else:
                items = [x.strip() for x in bracket.split(",") if x.strip()]
                for it in items:
                    if not re.fullmatch(r"T\d{3,4}", it):
                        errors.append(
                            f"tasks.deps.yml:{i}: {tid}: invalid dep token '{it}'"
                        )
                deps[tid] = [x for x in items if re.fullmatch(r"T\d{3,4}", x)]
                current_id = None
            continue

        li = list_item_re.match(line)
        if li and current_id is not None:
            deps[current_id].append(li.group(1))
            continue

    return deps, errors


# ----------------------------------------------------------------------------
# Graph validation + propagation
# ----------------------------------------------------------------------------

def validate_and_merge(
    tasks: Dict[str, Task],
    deps: Dict[str, List[str]],
) -> List[str]:
    errors: List[str] = []

    md_ids = set(tasks.keys())
    yml_ids = set(deps.keys())

    only_md = md_ids - yml_ids
    only_yml = yml_ids - md_ids

    for tid in sorted(only_md):
        errors.append(
            f"tasks.md declares {tid} but tasks.deps.yml has no key for it"
        )
    for tid in sorted(only_yml):
        errors.append(
            f"tasks.deps.yml declares {tid} but tasks.md has no task line"
        )

    # Check every dep reference points to a known task.
    for tid, dlist in deps.items():
        if tid not in tasks:
            continue  # already errored
        for d in dlist:
            if d not in tasks:
                errors.append(
                    f"tasks.deps.yml: {tid} depends on {d}, which does not exist"
                )
            elif d == tid:
                errors.append(f"tasks.deps.yml: {tid} depends on itself")

    # Merge explicit deps into tasks.
    for tid, dlist in deps.items():
        if tid in tasks:
            tasks[tid].explicit_deps = [d for d in dlist if d in tasks]

    return errors


def detect_cycles(tasks: Dict[str, Task]) -> List[List[str]]:
    """DFS-based cycle detection. Returns list of cycles (each a list of ids)."""
    WHITE, GRAY, BLACK = 0, 1, 2
    color: Dict[str, int] = {tid: WHITE for tid in tasks}
    stack: List[str] = []
    cycles: List[List[str]] = []

    def visit(tid: str) -> None:
        color[tid] = GRAY
        stack.append(tid)
        for d in tasks[tid].all_deps:
            if d not in color:
                continue
            if color[d] == GRAY:
                # Cycle: extract from stack position of d
                try:
                    idx = stack.index(d)
                    cycles.append(stack[idx:] + [d])
                except ValueError:
                    cycles.append([d, tid])
            elif color[d] == WHITE:
                visit(d)
        color[tid] = BLACK
        stack.pop()

    for tid in tasks:
        if color[tid] == WHITE:
            visit(tid)

    return cycles


def topo_sort(tasks: Dict[str, Task]) -> List[str]:
    """Kahn's algorithm. Assumes no cycles (caller checks)."""
    indeg: Dict[str, int] = {tid: 0 for tid in tasks}
    for tid, task in tasks.items():
        for d in task.all_deps:
            if d in tasks:
                indeg[tid] += 1

    # Reverse adjacency: for each d, who depends on d?
    rev: Dict[str, List[str]] = defaultdict(list)
    for tid, task in tasks.items():
        for d in task.all_deps:
            if d in tasks:
                rev[d].append(tid)

    queue = deque(sorted(tid for tid, n in indeg.items() if n == 0))
    order: List[str] = []
    while queue:
        tid = queue.popleft()
        order.append(tid)
        for nxt in sorted(rev[tid]):
            indeg[nxt] -= 1
            if indeg[nxt] == 0:
                queue.append(nxt)
    return order


def propagate(
    tasks: Dict[str, Task],
    topo: List[str],
) -> Dict[str, List[str]]:
    """Compute effective status + root-cause map.

    effective(T) =
      'synthetic'       if declared(T) == 'synthetic'
      'auto-synthetic'  if declared(T) == 'done' and any dep is synthetic/auto
      declared(T)       otherwise

    root_cause[T] = sorted list of upstream ids (direct deps) that are
    synthetic or auto-synthetic, only populated when effective(T) is
    auto-synthetic.
    """
    root_cause: Dict[str, List[str]] = {}
    for tid in topo:
        t = tasks[tid]
        tainted_deps = [
            d for d in t.all_deps
            if d in tasks and tasks[d].effective in ("synthetic", "auto-synthetic")
        ]
        if t.declared == "synthetic":
            t.effective = "synthetic"
        elif t.declared == "done" and tainted_deps:
            t.effective = "auto-synthetic"
            root_cause[tid] = sorted(tainted_deps)
        else:
            t.effective = t.declared
    return root_cause


# ----------------------------------------------------------------------------
# Rendering
# ----------------------------------------------------------------------------

STATUS_BOX = {
    "pending": "[ ]",
    "done": "[X]",
    "synthetic": "[S]",
    "failed": "[F]",
    "skipped": "[-]",
    "auto-synthetic": "[S*]",
}

STATUS_MERMAID_CLASS = {
    "pending": "pending",
    "done": "done",
    "synthetic": "synthetic",
    "failed": "failed",
    "skipped": "skipped",
    "auto-synthetic": "autoSynthetic",
}


def render_json(
    tasks: Dict[str, Task],
    root_cause: Dict[str, List[str]],
    errors: List[str],
    warnings: List[str],
    cycles: List[List[str]],
) -> str:
    payload = {
        "schema_version": "1.0",
        "verdict": "ok" if not errors and not cycles else "error",
        "errors": errors,
        "warnings": warnings,
        "cycles": cycles,
        "tasks": [
            {
                "id": t.id,
                "declared": t.declared,
                "effective": t.effective,
                "phase": t.phase,
                "story": t.story,
                "tier": t.tier,
                "parallel": t.parallel,
                "title": t.title,
                "explicit_deps": t.explicit_deps,
                "phase_deps": t.phase_deps,
                "root_cause": root_cause.get(t.id, []),
            }
            for t in sorted(tasks.values(), key=lambda x: x.id)
        ],
    }
    return json.dumps(payload, indent=2) + "\n"


def render_mermaid(tasks: Dict[str, Task]) -> str:
    lines = ["```mermaid", "graph TD"]
    for t in sorted(tasks.values(), key=lambda x: x.id):
        label = t.title.replace('"', "'")[:50]
        node_id = t.id
        lines.append(f'  {node_id}["{node_id} {label}"]:::{STATUS_MERMAID_CLASS[t.effective]}')
    for t in sorted(tasks.values(), key=lambda x: x.id):
        for d in t.all_deps:
            if d in tasks:
                lines.append(f"  {d} --> {t.id}")
    lines.extend([
        "  classDef pending fill:#eeeeee,stroke:#999",
        "  classDef done fill:#c8e6c9,stroke:#2e7d32",
        "  classDef synthetic fill:#ffe0b2,stroke:#e65100,stroke-width:2px",
        "  classDef autoSynthetic fill:#ffab91,stroke:#bf360c,stroke-width:2px,stroke-dasharray:5 3",
        "  classDef failed fill:#ffcdd2,stroke:#b71c1c,stroke-width:2px",
        "  classDef skipped fill:#f5f5f5,stroke:#666,stroke-dasharray:3 3",
        "```",
    ])
    return "\n".join(lines)


def render_ascii(tasks: Dict[str, Task], root_cause: Dict[str, List[str]]) -> str:
    lines = []
    for t in sorted(tasks.values(), key=lambda x: x.id):
        box = STATUS_BOX[t.effective]
        marker = ""
        if t.effective == "auto-synthetic":
            marker = "   ← auto-synthetic"
        elif t.effective == "synthetic":
            marker = "   ← root cause"
        lines.append(f"{t.id} {box} {t.title}{marker}")
        if t.id in root_cause:
            for rc in root_cause[t.id]:
                rc_box = STATUS_BOX[tasks[rc].effective]
                lines.append(f"    └── {rc} {rc_box} {tasks[rc].title}")
    return "\n".join(lines)


def render_markdown(
    feature_dir: Path,
    tasks: Dict[str, Task],
    root_cause: Dict[str, List[str]],
    errors: List[str],
    warnings: List[str],
    cycles: List[List[str]],
) -> str:
    out = [f"# Task Graph — {feature_dir.name}", ""]

    # Verdict block
    if errors or cycles:
        out.append("## ✗ Graph validation failed")
        if cycles:
            out.append("")
            out.append("### Cycles detected")
            for cy in cycles:
                out.append(f"- {' → '.join(cy)}")
        if errors:
            out.append("")
            out.append("### Errors")
            for e in errors:
                out.append(f"- {e}")
        out.append("")
    else:
        out.append("## ✓ Graph is acyclic and consistent")
        out.append("")

    if warnings:
        out.append("### Warnings")
        for w in warnings:
            out.append(f"- {w}")
        out.append("")

    # Counts by effective status
    counts: Dict[str, int] = defaultdict(int)
    for t in tasks.values():
        counts[t.effective] += 1
    out.append("## Status counts (effective)")
    out.append("")
    out.append("| Status | Count |")
    out.append("|--------|-------|")
    for key in ("pending", "done", "synthetic", "auto-synthetic", "failed", "skipped"):
        if counts[key] or key in ("synthetic", "auto-synthetic"):
            out.append(f"| {STATUS_BOX[key]} {key} | {counts[key]} |")
    out.append("")

    # Mermaid
    out.append("## Graph")
    out.append("")
    out.append(render_mermaid(tasks))
    out.append("")

    # ASCII view
    out.append("## ASCII view")
    out.append("")
    out.append("```")
    out.append(render_ascii(tasks, root_cause))
    out.append("```")
    out.append("")

    # Propagation report
    if root_cause:
        out.append("## Propagation report")
        out.append("")
        out.append(
            "The following tasks are marked `[S*]` because at least one of "
            "their dependencies is synthetic-only. Clearing the upstream "
            "`[S]` tasks (real evidence) will automatically clear these."
        )
        out.append("")
        for tid in sorted(root_cause.keys()):
            rcs = root_cause[tid]
            rc_str = ", ".join(rcs)
            out.append(f"- **{tid}** ([S*]) ← {rc_str}")
        out.append("")

    return "\n".join(out) + "\n"


# ----------------------------------------------------------------------------
# Main
# ----------------------------------------------------------------------------

def main(argv: List[str]) -> int:
    if len(argv) != 2:
        print("usage: compute-task-graph.py <feature-dir>", file=sys.stderr)
        return 3

    feature_dir = Path(argv[1]).resolve()
    if not feature_dir.is_dir():
        print(f"error: {feature_dir} is not a directory", file=sys.stderr)
        return 3

    tasks_md = feature_dir / "tasks.md"
    deps_yml = feature_dir / "tasks.deps.yml"
    readiness_dir = feature_dir / "readiness"
    readiness_dir.mkdir(parents=True, exist_ok=True)

    errors: List[str] = []
    warnings: List[str] = []

    if not tasks_md.exists():
        errors.append(f"{tasks_md} does not exist")
    if not deps_yml.exists():
        errors.append(f"{deps_yml} does not exist")

    tasks: Dict[str, Task] = {}
    deps: Dict[str, List[str]] = {}

    if tasks_md.exists():
        tasks, task_errs = parse_tasks_md(tasks_md)
        errors.extend(task_errs)
    if deps_yml.exists():
        deps, dep_errs = parse_deps_yml(deps_yml)
        errors.extend(dep_errs)

    if tasks and deps is not None:
        errors.extend(validate_and_merge(tasks, deps))

    cycles: List[List[str]] = []
    root_cause: Dict[str, List[str]] = {}
    if tasks and not errors:
        cycles = detect_cycles(tasks)
        if not cycles:
            order = topo_sort(tasks)
            if len(order) != len(tasks):
                errors.append(
                    f"topological sort incomplete: expected {len(tasks)} tasks, "
                    f"got {len(order)}. A cycle may have been missed."
                )
            else:
                root_cause = propagate(tasks, order)

    # Write artifacts even on failure — the audit wants to see the report.
    json_out = readiness_dir / "task-graph.json"
    md_out = readiness_dir / "task-graph.md"

    try:
        json_out.write_text(render_json(tasks, root_cause, errors, warnings, cycles))
        md_out.write_text(
            render_markdown(feature_dir, tasks, root_cause, errors, warnings, cycles)
        )
    except OSError as e:
        print(f"error: failed to write readiness artifacts: {e}", file=sys.stderr)
        return 3

    # Console summary
    print(f"task-graph: {len(tasks)} tasks parsed")
    print(f"  wrote {json_out}")
    print(f"  wrote {md_out}")

    if errors or cycles:
        print("", file=sys.stderr)
        print("VALIDATION FAILED", file=sys.stderr)
        for c in cycles:
            print(f"  cycle: {' -> '.join(c)}", file=sys.stderr)
        for e in errors:
            print(f"  {e}", file=sys.stderr)
        return 2

    # Summary line
    counts: Dict[str, int] = defaultdict(int)
    for t in tasks.values():
        counts[t.effective] += 1
    summary = ", ".join(
        f"{counts[k]}{STATUS_BOX[k]}"
        for k in ("done", "synthetic", "auto-synthetic", "pending", "failed", "skipped")
        if counts[k]
    )
    print(f"  status: {summary}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
