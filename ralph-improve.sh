#!/usr/bin/env bash
# ralph.sh — Run Claude Code in an infinite loop with readable streaming output
#
# Usage:
#   ./ralph.sh PROMPT_BUILD.md
#   ./ralph.sh PROMPT_BUILD.md --log    # also saves raw JSONL to ./logs/
#
# Requires: claude (Claude Code CLI), python3

set -euo pipefail

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

# ── ANSI ─────────────────────────────────────────────────────────────────────
RESET   = "\033[0m"
BOLD    = "\033[1m"
DIM     = "\033[2m"

RED     = "\033[31m"
GREEN   = "\033[32m"
YELLOW  = "\033[33m"
BLUE    = "\033[34m"
MAGENTA = "\033[35m"
CYAN    = "\033[36m"
WHITE   = "\033[37m"
GREY    = "\033[90m"

try:
    cols = os.get_terminal_size().columns
except (OSError, ValueError):
    cols = 120

def hrule(char, color=GREY):
    print(f"{color}{char * min(cols, 120)}{RESET}")

def truncate(s, n=150):
    s = s.replace("\n", " ").replace("\r", "")
    return s[:n] + "..." if len(s) > n else s

# ── State ────────────────────────────────────────────────────────────────────
seen_tools = {}
subagent_count = 0
last_block_type = None

def handle_event(data):
    global subagent_count, last_block_type

    evt_type = data.get("type", "")

    # ─── ASSISTANT messages ──────────────────────────────────────────────
    # Main event type from claude --output-format stream-json --verbose
    # Contains message.content[] with thinking, text, tool_use, tool_result
    if evt_type == "assistant":
        msg = data.get("message", {})
        content = msg.get("content", [])

        for block in content:
            btype = block.get("type", "")

            if btype == "thinking":
                thinking = block.get("thinking", "")
                if thinking and thinking.strip():
                    if last_block_type != "thinking":
                        print(f"\n{YELLOW}{BOLD}Thinking:{RESET}")
                        last_block_type = "thinking"
                    print(f"{GREY}{thinking}{RESET}", end="", flush=True)

            elif btype == "text":
                text = block.get("text", "")
                if text and text.strip():
                    if last_block_type != "text":
                        print()
                        last_block_type = "text"
                    print(f"{WHITE}{text}{RESET}", end="", flush=True)

            elif btype == "tool_use":
                tool_name = block.get("name", "unknown")
                tool_id = block.get("id", "")
                tool_input = block.get("input", {})
                seen_tools[tool_id] = tool_name

                # ── Subagent dispatch ────────────────────────────────
                if tool_name in ("dispatch_agent", "Agent", "agent", "Task",
                                 "Explore", "CodeEdit", "MultiTool"):
                    subagent_count += 1
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
                    print(f"\n{MAGENTA}  🔀 subagent #{subagent_count}: "
                          f"{label}{truncate(prompt, 120)}{RESET}")
                    last_block_type = "subagent"

                # ── Regular tools ────────────────────────────────────
                else:
                    summary = ""
                    if isinstance(tool_input, dict):
                        for key in ("command", "file_path", "path", "query",
                                    "pattern", "regex", "file"):
                            if key in tool_input:
                                summary = truncate(str(tool_input[key]), 100)
                                break
                        if not summary:
                            keys = list(tool_input.keys())[:4]
                            summary = ", ".join(keys) if keys else ""

                    print(f"\n{GREEN}{BOLD}⚡ {tool_name}{RESET}"
                          f"{GREEN} {summary}{RESET}")
                    last_block_type = "tool"

            elif btype == "tool_result":
                content_val = block.get("content", "")
                tool_id = block.get("tool_use_id", "")
                tool_name = seen_tools.get(tool_id, "")
                is_error = block.get("is_error", False)

                # Extract text from content
                result_text = ""
                if isinstance(content_val, str):
                    result_text = content_val
                elif isinstance(content_val, list):
                    for item in content_val:
                        if isinstance(item, dict) and item.get("type") == "text":
                            result_text = item.get("text", "")
                            break

                # Subagent results — compact
                if tool_name in ("dispatch_agent", "Agent", "agent", "Task",
                                 "Explore", "CodeEdit", "MultiTool"):
                    if result_text:
                        print(f"{MAGENTA}  ✓ done: {truncate(result_text, 100)}{RESET}")
                    else:
                        print(f"{MAGENTA}  ✓ done{RESET}")
                    last_block_type = "subagent_result"

                elif is_error:
                    print(f"{RED}  ✗ {tool_name or 'tool'} error: "
                          f"{truncate(result_text or str(content_val), 200)}{RESET}")
                    last_block_type = "error"

                else:
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
                    last_block_type = "result"

    # ─── USER messages (subagent input) ──────────────────────────────────
    # These are the noisy JSON blobs — suppress them entirely
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

    # ─── RATE LIMIT ──────────────────────────────────────────────────────
    elif evt_type == "rate_limit_event":
        info = data.get("rate_limit_info", {})
        status = info.get("status", "")
        ltype = info.get("rateLimitType", "")
        resets = info.get("resetsAt", "")
        print(f"{YELLOW}Rate limit: {status} ({ltype})"
              f"{f'  resets: {resets}' if resets else ''}{RESET}")
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

    if $LOG_ENABLED; then
        LOGFILE="${LOG_DIR}/ralph-loop-${LOOP}-$(date +'%Y%m%d-%H%M%S').jsonl"
        claude -p "$PROMPT_CONTENT" \
            --dangerously-skip-permissions \
            --output-format stream-json \
            --verbose \
            2>&1 | tee "$LOGFILE" | python3 -u -c "$FORMATTER"
        echo -e "\033[90m  Log: ${LOGFILE}\033[0m"
    else
        claude -p "$PROMPT_CONTENT" \
            --dangerously-skip-permissions \
            --output-format stream-json \
            --verbose \
            2>&1 | python3 -u -c "$FORMATTER"
    fi

    EXIT_CODE=$?
    if [[ $EXIT_CODE -ne 0 ]]; then
        echo -e "\033[31mClaude exited with code ${EXIT_CODE}, continuing...\033[0m"
        sleep 2
    fi

    echo -e "\033[90m  Next loop in 3s... (Ctrl+C to stop)\033[0m"
    sleep 3
done
