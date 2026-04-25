#!/usr/bin/env bash
#
# run-audit.sh — synthetic-evidence audit runner.
#
# Combines two signals:
#   1. Task graph — via compute-task-graph.py. Any [S] (synthetic) or [S*]
#      (auto-synthetic) task counts against merge-readiness.
#   2. Diff scan — greps git diff <feature-base>..HEAD against the patterns
#      in audit-patterns.yml. Block-severity hits count against
#      merge-readiness. Advisory-severity hits print but do not block.
#
# Exit codes:
#   0 — PASS: no [S], no [S*], no blocking diff-scan hits.
#   2 — NEEDS-EVIDENCE: at least one blocking signal.
#   3 — graph compute failed (cycles, dangling refs, parse errors).
#   4 — usage / filesystem error.
#
# The --accept-synthetic flag is recorded to readiness/synthetic-evidence.json
# but NEVER changes the exit code. The human decides to merge despite the
# failure; the audit still reports failure.
#
# Usage:
#   run-audit.sh <feature-dir>
#   run-audit.sh <feature-dir> --graph-only
#   run-audit.sh <feature-dir> --accept-synthetic "justification text"
#   run-audit.sh <feature-dir> --base <ref>     # override feature-base (default: main|master)
#   run-audit.sh <feature-dir> --patterns <path> # override audit-patterns.yml

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
GRAPH_SCRIPT="$EXT_ROOT/scripts/python/compute-task-graph.py"
DEFAULT_PATTERNS="$EXT_ROOT/audit-patterns.yml"

FEATURE_DIR=""
GRAPH_ONLY=0
ACCEPT_SYNTHETIC=""
BASE_REF=""
PATTERNS_FILE="$DEFAULT_PATTERNS"

die() { echo "run-audit: $*" >&2; exit 4; }

# --- arg parsing ------------------------------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --graph-only) GRAPH_ONLY=1; shift ;;
    --accept-synthetic)
      [[ $# -lt 2 ]] && die "--accept-synthetic requires justification text"
      ACCEPT_SYNTHETIC="$2"; shift 2 ;;
    --base)
      [[ $# -lt 2 ]] && die "--base requires a ref"
      BASE_REF="$2"; shift 2 ;;
    --patterns)
      [[ $# -lt 2 ]] && die "--patterns requires a path"
      PATTERNS_FILE="$2"; shift 2 ;;
    -h|--help)
      sed -n 's/^# \{0,1\}//p' "${BASH_SOURCE[0]}" | head -30; exit 0 ;;
    -*)
      die "unknown flag: $1" ;;
    *)
      [[ -n "$FEATURE_DIR" ]] && die "feature-dir already set to '$FEATURE_DIR'"
      FEATURE_DIR="$1"; shift ;;
  esac
done

[[ -z "$FEATURE_DIR" ]] && die "usage: run-audit.sh <feature-dir> [flags]"
[[ ! -d "$FEATURE_DIR" ]] && die "$FEATURE_DIR is not a directory"

FEATURE_DIR="$(cd "$FEATURE_DIR" && pwd)"
READINESS_DIR="$FEATURE_DIR/readiness"
mkdir -p "$READINESS_DIR"

# --- 1. graph compute -------------------------------------------------------
echo "=== speckit.evidence.audit ==="
echo "feature: $FEATURE_DIR"
echo

echo "[1/2] Computing task graph..."
if ! python3 "$GRAPH_SCRIPT" "$FEATURE_DIR"; then
  echo
  echo "✗ Graph compute failed. See $READINESS_DIR/task-graph.md for details." >&2
  exit 3
fi
echo

if [[ $GRAPH_ONLY -eq 1 ]]; then
  echo "graph-only mode; skipping diff scan."
  exit 0
fi

# --- 2. read graph effective statuses ---------------------------------------
GRAPH_JSON="$READINESS_DIR/task-graph.json"
[[ ! -f "$GRAPH_JSON" ]] && die "task-graph.json not produced"

# Extract counts of synthetic and auto-synthetic tasks.
SYNTHETIC_COUNT=$(python3 -c "
import json,sys
d = json.load(open('$GRAPH_JSON'))
print(sum(1 for t in d['tasks'] if t['effective'] == 'synthetic'))
")
AUTO_SYNTHETIC_COUNT=$(python3 -c "
import json,sys
d = json.load(open('$GRAPH_JSON'))
print(sum(1 for t in d['tasks'] if t['effective'] == 'auto-synthetic'))
")

# --- 3. diff scan -----------------------------------------------------------
echo "[2/2] Diff scan against feature base..."

# Resolve feature base.
if [[ -z "$BASE_REF" ]]; then
  if git -C "$FEATURE_DIR" show-ref --verify --quiet refs/heads/main; then
    BASE_REF="main"
  elif git -C "$FEATURE_DIR" show-ref --verify --quiet refs/heads/master; then
    BASE_REF="master"
  else
    echo "  warning: neither main nor master exists; using HEAD~1"
    BASE_REF="HEAD~1"
  fi
fi

# Repo root (diff must be run from the top-level of the repo).
if REPO_ROOT="$(git -C "$FEATURE_DIR" rev-parse --show-toplevel 2>/dev/null)"; then
  :
else
  echo "  warning: not in a git repo; skipping diff scan"
  REPO_ROOT=""
fi

DIFF_HITS_JSON="$READINESS_DIR/diff-scan-hits.json"
BLOCK_HITS=0
ADVISORY_HITS=0

if [[ -n "$REPO_ROOT" ]]; then
  # Dump the diff once; re-scan per pattern in Python for readable reporting.
  DIFF_FILE="$(mktemp)"
  trap 'rm -f "$DIFF_FILE"' EXIT
  git -C "$REPO_ROOT" diff "$BASE_REF"...HEAD --unified=0 >"$DIFF_FILE" || true

  set +e
  python3 - "$PATTERNS_FILE" "$DIFF_FILE" "$DIFF_HITS_JSON" <<'PYEOF'
import json, re, sys
from pathlib import Path

patterns_path = Path(sys.argv[1])
diff_path = Path(sys.argv[2])
out_path = Path(sys.argv[3])

# Minimal YAML parsing — we only need the patterns + whitelist blocks.
# This is NOT a general YAML parser; it's tuned to the audit-patterns.yml
# shape defined in the extension. pyyaml isn't a hard dep.

def parse(text):
    patterns = []
    whitelist = []
    severity_overrides = {}
    state = None
    cur = None
    for raw in text.splitlines():
        line = raw.rstrip()
        if not line or line.lstrip().startswith("#"):
            continue
        if line.startswith("patterns:"):
            state = "patterns"; continue
        if line.startswith("whitelist:"):
            state = "whitelist"; continue
        if line.startswith("severity_overrides:"):
            state = "severity_overrides"; continue
        if state == "patterns":
            if line.startswith("  - "):
                if cur: patterns.append(cur)
                cur = {}
                # first field is on the same line
                field = line[4:]
                if ":" in field:
                    k, v = field.split(":", 1)
                    cur[k.strip()] = v.strip().strip("'\"")
            elif line.startswith("    ") and cur is not None and ":" in line:
                k, v = line.strip().split(":", 1)
                cur[k.strip()] = v.strip().strip("'\"")
        elif state == "whitelist":
            if line.startswith("  - "):
                if cur and "pattern_id" in cur: whitelist.append(cur)
                cur = {}
                field = line[4:]
                if ":" in field:
                    k, v = field.split(":", 1)
                    cur[k.strip()] = v.strip().strip("'\"")
            elif line.startswith("    ") and cur is not None and ":" in line:
                k, v = line.strip().split(":", 1)
                cur[k.strip()] = v.strip().strip("'\"")
    if state == "patterns" and cur:
        patterns.append(cur)
    if state == "whitelist" and cur and "pattern_id" in cur:
        whitelist.append(cur)
    return patterns, whitelist, severity_overrides

patterns, whitelist, overrides = parse(patterns_path.read_text())
for p in patterns:
    if p["id"] in overrides:
        p["severity"] = overrides[p["id"]]

# Parse diff to get (path, line_no, content) tuples.
hits = []
cur_file = None
cur_lineno = 0
for raw in diff_path.read_text(errors="replace").splitlines():
    if raw.startswith("+++ b/"):
        cur_file = raw[6:]
        continue
    if raw.startswith("+++ "):
        cur_file = raw[4:]
        continue
    m = re.match(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@", raw)
    if m:
        cur_lineno = int(m.group(1)) - 1
        continue
    if raw.startswith("+") and not raw.startswith("+++"):
        cur_lineno += 1
        content = raw[1:]
        if cur_file is None:
            continue
        for p in patterns:
            try:
                if re.search(p["regex"], content):
                    wl_hit = False
                    import fnmatch
                    for wl in whitelist:
                        if wl.get("pattern_id") != p["id"]:
                            continue
                        fg = wl.get("file_glob")
                        lr = wl.get("line_regex")
                        if fg and not fnmatch.fnmatch(cur_file, fg):
                            continue
                        if lr and not re.search(lr, content):
                            continue
                        wl_hit = True; break
                    if wl_hit:
                        continue
                    hits.append({
                        "file": cur_file,
                        "line": cur_lineno,
                        "pattern": p["id"],
                        "severity": p.get("severity", "block"),
                        "reason": p.get("reason", ""),
                        "match": content.strip()[:120],
                    })
            except re.error:
                pass
    elif raw.startswith(" "):
        cur_lineno += 1

# Summary to stderr for the caller, and write JSON.
block = [h for h in hits if h["severity"] == "block"]
adv = [h for h in hits if h["severity"] == "advisory"]

print(f"  diff-scan: {len(block)} blocking, {len(adv)} advisory")
out_path.write_text(json.dumps({
    "base_ref": None,
    "blocking": block,
    "advisory": adv,
}, indent=2) + "\n")

# Print blockers to stderr so the audit output is rich.
for h in block:
    print(f"    [BLOCK] {h['pattern']} at {h['file']}:{h['line']}  ({h['reason']})", file=sys.stderr)
    print(f"            {h['match']}", file=sys.stderr)
for h in adv:
    print(f"    [adv]   {h['pattern']} at {h['file']}:{h['line']}  ({h['reason']})", file=sys.stderr)

# Return count for the shell caller via exit code: 0 = no blockers, 2 = blockers.
sys.exit(0 if not block else 2)
PYEOF

  DIFF_EXIT=$?
  set -e
  if [[ $DIFF_EXIT -eq 2 ]]; then
    BLOCK_HITS=1
  elif [[ $DIFF_EXIT -ne 0 ]]; then
    die "diff-scan helper failed (exit $DIFF_EXIT)"
  fi

  if [[ -f "$DIFF_HITS_JSON" ]]; then
    ADVISORY_HITS=$(python3 -c "import json; d=json.load(open('$DIFF_HITS_JSON')); print(len(d['advisory']))")
  fi
else
  echo "  diff scan skipped (not a git repo)"
  echo '{"base_ref": null, "blocking": [], "advisory": []}' > "$DIFF_HITS_JSON"
fi
echo

# --- 4. verdict -------------------------------------------------------------
SYNTHETIC_JSON="$READINESS_DIR/synthetic-evidence.json"

if [[ -n "$ACCEPT_SYNTHETIC" ]]; then
  python3 - "$SYNTHETIC_JSON" "$GRAPH_JSON" "$DIFF_HITS_JSON" <<PYEOF
import json, sys
from datetime import datetime, timezone
out = sys.argv[1]; graph = sys.argv[2]; diff = sys.argv[3]
payload = {
    "schema_version": "1.0",
    "accept_synthetic": {
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "justification": """$ACCEPT_SYNTHETIC""",
    },
    "graph_summary": {
        "synthetic": $SYNTHETIC_COUNT,
        "auto_synthetic": $AUTO_SYNTHETIC_COUNT,
    },
    "diff_scan": json.load(open(diff)),
}
with open(out, "w") as f:
    json.dump(payload, f, indent=2)
    f.write("\n")
PYEOF
fi

TOTAL_BLOCKERS=$((SYNTHETIC_COUNT + AUTO_SYNTHETIC_COUNT + BLOCK_HITS))

echo "=== Verdict ==="
if [[ $TOTAL_BLOCKERS -eq 0 ]]; then
  echo "✓ PASS — no synthetic tasks, no blocking diff-scan hits."
  exit 0
fi

echo "✗ NEEDS-EVIDENCE"
echo "  synthetic tasks (declared):     $SYNTHETIC_COUNT"
echo "  auto-synthetic tasks (computed): $AUTO_SYNTHETIC_COUNT"
if [[ $BLOCK_HITS -gt 0 ]]; then
  echo "  blocking diff-scan hits:         (see above)"
fi
if [[ $ADVISORY_HITS -gt 0 ]]; then
  echo "  advisory diff-scan hits:         $ADVISORY_HITS (printed, not blocking)"
fi

echo
if [[ -n "$ACCEPT_SYNTHETIC" ]]; then
  echo "  --accept-synthetic recorded: $ACCEPT_SYNTHETIC"
  echo "  logged to $SYNTHETIC_JSON"
  echo "  audit still reports failure; merge is a human decision."
else
  echo "  To override, rerun with:  --accept-synthetic \"justification\""
fi

exit 2
