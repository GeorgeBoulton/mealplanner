#!/usr/bin/env bash
# ralph.sh — Run Claude Code in an infinite loop with readable streaming output
#
# Usage:
#   ./ralph.sh PROMPT_BUILD.md
#   ./ralph.sh PROMPT_BUILD.md --log    # also saves raw JSONL to ./logs/
#
# Requires: claude (Claude Code CLI), python3

PROMPT_FILE="${1:?Usage: ./ralph.sh PROMPT_FILE.md [--log]}"
LOG_ENABLED=false
LOG_DIR="./logs"

if [[ "${2:-}" == "--log" ]]; then
    LOG_ENABLED=true
    mkdir -p "$LOG_DIR"
fi

if ! command -v claude &>/dev/null; then
    echo "Error: claude CLI not found in PATH" >&2
    exit 1
fi

if ! command -v python3 &>/dev/null; then
    echo "Error: python3 not found" >&2
    exit 1
fi

if [[ ! -f "$PROMPT_FILE" ]]; then
    echo "Error: Prompt file not found: $PROMPT_FILE" >&2
    exit 1
fi

# ── The formatter ────────────────────────────────────────────────────────────

FORMATTER=$(cat << 'PYEOF'
import sys
import json
import os
import re
from datetime import datetime, timezone

# ── ANSI ─────────────────────────────────────────────────────────────────────
RESET   = "\033[0m"
BOLD    = "\033[1m"
DIM     = "\033[2m"
UNDERLINE = "\033[4m"

RED     = "\033[31m"
GREEN   = "\033[32m"
YELLOW  = "\033[33m"
BLUE    = "\033[34m"
MAGENTA = "\033[35m"
CYAN    = "\033[36m"
WHITE   = "\033[37m"
GREY    = "\033[90m"
BRIGHT_WHITE = "\033[97m"
BRIGHT_CYAN  = "\033[96m"

BG_GREY = "\033[48;5;236m"

try:
    cols = os.get_terminal_size().columns
except (OSError, ValueError):
    cols = 120

def hrule(char, color=GREY):
    print(f"{color}{char * min(cols, 120)}{RESET}")

def truncate(s, n=150):
    s = s.replace("\n", " ").replace("\r", "")
    return s[:n] + "..." if len(s) > n else s

def format_md(text):
    text = re.sub(r'\*\*(.+?)\*\*', f'{BOLD}{BRIGHT_WHITE}\\1{RESET}{GREY}', text)
    text = re.sub(r'`([^`]+)`', f'{BRIGHT_CYAN}\\1{RESET}{GREY}', text)
    text = re.sub(r'^(#{1,3})\s+(.+)$', f'{BOLD}{BRIGHT_WHITE}\\2{RESET}{GREY}', text, flags=re.MULTILINE)
    return text

def format_epoch(epoch):
    try:
        ts = float(epoch)
        if ts > 1e12:
            ts = ts / 1000
        dt = datetime.fromtimestamp(ts, tz=timezone.utc)
        now = datetime.now(tz=timezone.utc)
        diff = dt - now
        secs = int(diff.total_seconds())
        if secs < 0:
            return dt.strftime("%H:%M:%S")
        elif secs < 60:
            return f"{secs}s"
        elif secs < 3600:
            return f"{secs // 60}m {secs % 60}s"
        else:
            return f"{secs // 3600}h {(secs % 3600) // 60}m"
    except (ValueError, TypeError, OSError):
        return str(epoch)

# ── State ────────────────────────────────────────────────────────────────────
seen_tools = {}
subagent_ids = set()
subagent_count = 0
last_block_type = None
indent_depth = 0
last_tool_key = None
repeat_count = 0
project_root = None    # auto-detected from first absolute path we see

def detect_root(path):
    """Try to detect the project root from the first path we see."""
    global project_root
    if project_root is not None:
        return
    if not isinstance(path, str) or not path.startswith("/"):
        return
    # Heuristic: walk up until we find a common project root indicator
    # or just use the first path's directory
    parts = path.split("/")
    # Look for common patterns like repos/X, source/X, projects/X, src/X
    for i, part in enumerate(parts):
        if part.lower() in ("repos", "source", "projects", "src", "home", "code", "dev", "work"):
            if i + 1 < len(parts):
                project_root = "/".join(parts[:i+2])
                return
    # Fallback: if path has 5+ segments, use first 5
    if len(parts) >= 5:
        project_root = "/".join(parts[:5])

def shorten(path):
    """Shorten an absolute path to be relative to project root."""
    if not isinstance(path, str):
        return str(path)
    detect_root(path)
    if project_root and path.startswith(project_root):
        rel = path[len(project_root):]
        if rel.startswith("/"):
            rel = rel[1:]
        return rel or "."
    return path

SUBAGENT_TOOLS = frozenset([
    "dispatch_agent", "Agent", "agent", "Task"
])

def is_subagent(tool_name, tool_input):
    """Detect if a tool_use is actually a subagent dispatch."""
    if tool_name in SUBAGENT_TOOLS:
        return True
    # Some tools carry a subagent_type field — that's a giveaway
    if isinstance(tool_input, dict) and tool_input.get("subagent_type"):
        return True
    return False

def pad():
    return ""

def handle_event(data):
    global subagent_count, last_block_type, indent_depth, last_tool_key, repeat_count

    evt_type = data.get("type", "")

    # ─── ASSISTANT messages ──────────────────────────────────────────────
    if evt_type == "assistant":
        msg = data.get("message", {})
        content = msg.get("content", [])

        for block in content:
            btype = block.get("type", "")

            if btype == "thinking":
                thinking = block.get("thinking", "")
                if thinking and thinking.strip():
                    if last_block_type != "thinking":
                        if repeat_count > 0:
                            print()
                            repeat_count = 0
                            last_tool_key = None
                        if last_block_type is not None:
                            print()
                        print(f"{YELLOW}{BOLD}Thinking:{RESET}")
                        last_block_type = "thinking"
                    formatted = format_md(thinking)
                    print(f"{GREY}{formatted}{RESET}", end="", flush=True)

            elif btype == "text":
                text = block.get("text", "")
                if text and text.strip():
                    if last_block_type != "text":
                        if repeat_count > 0:
                            print()
                            repeat_count = 0
                            last_tool_key = None
                        print()
                        last_block_type = "text"
                    formatted = format_md(text)
                    print(f"{WHITE}{formatted}{RESET}", end="", flush=True)

            elif btype == "tool_use":
                tool_name = block.get("name", "unknown")
                tool_id = block.get("id", "")
                tool_input = block.get("input", {})
                seen_tools[tool_id] = tool_name

                # ── Subagent dispatch ────────────────────────────────
                if is_subagent(tool_name, tool_input):
                    subagent_count += 1
                    subagent_ids.add(tool_id)
                    if repeat_count > 0:
                        print()
                        repeat_count = 0
                        last_tool_key = None
                    prompt = ""
                    if isinstance(tool_input, dict):
                        for key in ("prompt", "task", "description", "text"):
                            if key in tool_input and isinstance(tool_input[key], str):
                                prompt = tool_input[key]
                                break
                        if not prompt:
                            for v in tool_input.values():
                                if isinstance(v, str) and len(v) > 10:
                                    prompt = v
                                    break
                    agent_type = ""
                    if isinstance(tool_input, dict):
                        agent_type = tool_input.get("subagent_type", "")

                    label = f"{agent_type}: " if agent_type else ""
                    print(f"\n{MAGENTA}{BOLD}  🔀 subagent #{subagent_count}: "
                          f"{label}{truncate(prompt, 100)}{RESET}\n")
                    last_block_type = "subagent"

                # ── Regular tools ────────────────────────────────────
                else:
                    summary = ""
                    if isinstance(tool_input, dict):
                        for key in ("file_path", "path", "file"):
                            if key in tool_input:
                                summary = shorten(str(tool_input[key]))
                                break
                        if not summary:
                            for key in ("command", "query", "pattern", "regex"):
                                if key in tool_input:
                                    summary = truncate(str(tool_input[key]), 100)
                                    break
                        if not summary:
                            keys = list(tool_input.keys())[:4]
                            summary = ", ".join(keys) if keys else ""

                    # Dedup: collapse consecutive same-tool-type calls
                    if tool_name == last_tool_key:
                        repeat_count += 1
                        spaces = " " * 40
                        print(f"\r{DIM}  ... {tool_name} x{repeat_count + 1} (latest: {truncate(summary, 60)}){spaces}{RESET}", end="", flush=True)
                    else:
                        # Flush previous repeat run
                        if repeat_count > 0:
                            print()  # newline after counter
                        repeat_count = 0
                        last_tool_key = tool_name

                        if tool_name in ("Bash", "bash", "execute_command"):
                            print(f"\n{GREEN}{BOLD}⚡ {tool_name}{RESET}"
                                  f"  {BG_GREY}{BRIGHT_WHITE} {summary} {RESET}")
                        elif tool_name in ("Read", "read_file", "View"):
                            print(f"\n{CYAN}{BOLD}⚡ {tool_name}{RESET}"
                                  f" {CYAN}{summary}{RESET}")
                        elif tool_name in ("Write", "write_file", "Edit",
                                           "MultiEdit", "str_replace"):
                            print(f"\n{YELLOW}{BOLD}⚡ {tool_name}{RESET}"
                                  f" {YELLOW}{summary}{RESET}")
                        else:
                            print(f"\n{GREEN}{BOLD}⚡ {tool_name}{RESET}"
                                  f" {GREEN}{summary}{RESET}")
                    last_block_type = "tool"

            elif btype == "tool_result":
                content_val = block.get("content", "")
                tool_id = block.get("tool_use_id", "")
                tool_name = seen_tools.get(tool_id, "")
                is_error = block.get("is_error", False)

                result_text = ""
                if isinstance(content_val, str):
                    result_text = content_val
                elif isinstance(content_val, list):
                    for item in content_val:
                        if isinstance(item, dict) and item.get("type") == "text":
                            result_text = item.get("text", "")
                            break

                # Subagent results — always show
                if tool_id in subagent_ids:
                    if repeat_count > 0:
                        print()
                        repeat_count = 0
                        last_tool_key = None
                    if result_text:
                        print(f"\n{MAGENTA}  ✓ subagent done: {truncate(result_text, 100)}{RESET}")
                    else:
                        print(f"\n{MAGENTA}  ✓ subagent done{RESET}")
                    print()
                    last_block_type = "subagent_result"

                # Errors — always show
                elif is_error:
                    if repeat_count > 0:
                        print()
                        repeat_count = 0
                        last_tool_key = None
                    print(f"\n{RED}  ✗ {tool_name or 'tool'} error: "
                          f"{truncate(result_text or str(content_val), 200)}{RESET}")
                    print()
                    last_block_type = "error"

                # Regular results — suppress during dedup runs
                elif repeat_count == 0:
                    if result_text:
                        lines = result_text.strip().split("\n")
                        if len(lines) <= 6:
                            for line in lines:
                                print(f"{CYAN}  | {line}{RESET}")
                        else:
                            for line in lines[:3]:
                                print(f"{CYAN}  | {line}{RESET}")
                            print(f"{DIM}  | ... ({len(lines)} lines){RESET}")
                            for line in lines[-2:]:
                                print(f"{CYAN}  | {line}{RESET}")
                    print()
                    last_block_type = "result"

    # ─── USER messages (subagent input) — suppress ───────────────────────
    elif evt_type == "user":
        pass

    # ─── RESULT — final summary ──────────────────────────────────────────
    elif evt_type == "result":
        cost = data.get("cost_usd", 0)
        duration = data.get("duration_ms", 0)
        session_id = data.get("session_id", "")
        in_tok = data.get("total_input_tokens", 0)
        out_tok = data.get("total_output_tokens", 0)

        print()
        hrule("=", GREEN)
        parts = []
        if cost:
            parts.append(f"${cost:.4f}")
        if duration:
            parts.append(f"{duration/1000:.1f}s")
        if in_tok:
            parts.append(f"in:{in_tok:,}")
        if out_tok:
            parts.append(f"out:{out_tok:,}")
        print(f"{GREEN}{BOLD}  Loop complete{RESET}  {DIM}{' | '.join(parts)}{RESET}")
        hrule("=", GREEN)
        last_block_type = "done"

    # ─── SYSTEM ──────────────────────────────────────────────────────────
    elif evt_type == "system":
        msg = data.get("message", "")
        subtype = data.get("subtype", "")
        if subtype == "init":
            sid = data.get("session_id", "")
            print(f"{DIM}Session: {sid}{RESET}")
        elif msg:
            print(f"{BLUE}i {truncate(str(msg), 150)}{RESET}")
        last_block_type = "system"

    # ─── RATE LIMIT — only show if actually blocked ──────────────────────
    elif evt_type == "rate_limit_event":
        info = data.get("rate_limit_info", {})
        status = info.get("status", "")
        ltype = info.get("rateLimitType", "")
        resets = info.get("resetsAt", "")
        if status not in ("allowed", "allowed_warning"):
            resets_str = ""
            wait_secs = 0
            if resets:
                try:
                    ts = float(resets)
                    if ts > 1e12:
                        ts = ts / 1000
                    dt = datetime.fromtimestamp(ts, tz=timezone.utc)
                    now = datetime.now(tz=timezone.utc)
                    wait_secs = max(0, int((dt - now).total_seconds()))
                    resets_str = f"  resets in {format_epoch(resets)}"
                except (ValueError, TypeError):
                    pass
            print(f"\n{RED}{BOLD}⏱ RATE LIMITED: {status} ({ltype}){resets_str}{RESET}")
            # Write wait time to temp file so bash loop can backoff
            if wait_secs > 0:
                try:
                    with open("/tmp/ralph_backoff", "w") as f:
                        f.write(str(wait_secs))
                except OSError:
                    pass
        last_block_type = "rate_limit"

    # ─── ERROR ───────────────────────────────────────────────────────────
    elif evt_type == "error":
        print(f"{RED}{BOLD}Error: {truncate(str(data.get('error', data)), 200)}{RESET}")
        last_block_type = "error"

    # ─── CATCH-ALL ───────────────────────────────────────────────────────
    elif evt_type not in ("ping", ""):
        compact = json.dumps(data, separators=(",", ":"))
        print(f"{DIM}[{evt_type}] {truncate(compact, 180)}{RESET}")


# ── Main ─────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            data = json.loads(line)
            handle_event(data)
        except json.JSONDecodeError:
            print(f"{WHITE}{line}{RESET}")
        except BrokenPipeError:
            sys.exit(0)
        except Exception as e:
            print(f"{RED}Formatter error: {e}{RESET}", file=sys.stderr)
    sys.stdout.flush()
PYEOF
)

# ── Main loop ────────────────────────────────────────────────────────────────

LOOP=0

trap 'echo -e "\n\033[33mRalph stopped after $LOOP loops.\033[0m"; exit 0' INT

while :; do
    LOOP=$((LOOP + 1))
    NOW=$(date '+%Y-%m-%d %H:%M:%S')

    echo ""
    echo -e "\033[44m\033[1m RALPH LOOP #${LOOP}  |  ${NOW} \033[0m"
    echo -e "\033[90m$(printf -- '-%.0s' $(seq 1 $(tput cols 2>/dev/null || echo 80)))\033[0m"
    echo ""

    PROMPT_CONTENT="$(cat "$PROMPT_FILE")"

    # Check if we got rate limited and need to back off
    rm -f /tmp/ralph_backoff 2>/dev/null  # clean before run

    if $LOG_ENABLED; then
        LOGFILE="${LOG_DIR}/ralph-loop-${LOOP}-$(date +'%Y%m%d-%H%M%S').jsonl"
        claude -p "$PROMPT_CONTENT" \
            --dangerously-skip-permissions \
            --output-format stream-json \
            --verbose \
            2>&1 | tee "$LOGFILE" | python3 -u -c "$FORMATTER" || true
        echo -e "\033[90m  Log: ${LOGFILE}\033[0m"
    else
        claude -p "$PROMPT_CONTENT" \
            --dangerously-skip-permissions \
            --output-format stream-json \
            --verbose \
            2>&1 | python3 -u -c "$FORMATTER" || true
    fi

    # If rate limited, wait until the limit resets
    if [[ -f /tmp/ralph_backoff ]]; then
        WAIT_SECS=$(cat /tmp/ralph_backoff 2>/dev/null || echo "0")
        rm -f /tmp/ralph_backoff
        if [[ "$WAIT_SECS" -gt 0 ]] 2>/dev/null; then
            WAIT_MINS=$(( WAIT_SECS / 60 ))
            echo ""
            echo -e "\033[33m  ⏱ Rate limited. Waiting ${WAIT_MINS}m ${WAIT_SECS}s until reset...\033[0m"
            echo -e "\033[90m  (Ctrl+C to stop)\033[0m"

            # Countdown
            REMAINING=$WAIT_SECS
            while [[ $REMAINING -gt 0 ]]; do
                MINS=$(( REMAINING / 60 ))
                SECS=$(( REMAINING % 60 ))
                printf "\r\033[33m  ⏱ %02d:%02d remaining...\033[0m" "$MINS" "$SECS"
                sleep 1
                REMAINING=$(( REMAINING - 1 ))
            done
            echo -e "\r\033[32m  ✓ Rate limit reset, resuming...                    \033[0m"
            continue
        fi
    fi

    echo -e "\033[90m  Next loop in 3s... (Ctrl+C to stop)\033[0m"
    sleep 3
done
