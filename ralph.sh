#!/bin/bash

# Ralph Wiggum — AI coding loop tool
#
# Usage:
#   ./ralph.sh build           # build loop, runs forever
#   ./ralph.sh build 5         # build loop, 5 iterations
#   ./ralph.sh plan            # audit codebase, regenerate fix_plan.md
#   ./ralph.sh fix "description"  # one-shot targeted bug fix
#   ./ralph.sh help            # show this help

# -- Config -------------------------------------------
CLAUDE_FLAGS="--print --dangerously-skip-permissions"
SLEEP_BETWEEN=5
# -----------------------------------------------------

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
PURPLE='\033[0;35m'
WHITE='\033[1;37m'
DIM='\033[2m'
BOLD='\033[1m'
RESET='\033[0m'

QUOTES=(
    "I'm learnding!"
    "Me fail English? That's unpossible!"
    "My cat's breath smells like cat food."
    "I bent my Wookiee."
    "When I grow up, I want to be a principal or a caterpillar."
    "Hi, Super Nintendo Chalmers!"
    "I found a moon rock in my nose!"
    "I dressed myself today!"
)

ralph_quote() {
    echo "${QUOTES[$((RANDOM % ${#QUOTES[@]}))]}"
}

banner() {
    echo -e "${YELLOW}${BOLD}"
    echo '  ____      _       _     '
    echo ' |  _ \__ _| |_ __ | |__  '
    echo ' | |_) / _` | | '\''_ \| '\''_ \ '
    echo ' |  _ < (_| | | |_) | | | |'
    echo ' |_| \_\__,_|_| .__/|_| |_|'
    echo '             _|_|'
    echo -e "${RESET}"
}

run_claude() {
    local prompt="$1"
    local label="$2"
    local start_time=$(date +%s)

    echo -e "  ${PURPLE}${DIM}\"$(ralph_quote)\"${RESET}"
    echo ""

    echo "$prompt" | claude $CLAUDE_FLAGS

    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    local minutes=$((duration / 60))
    local seconds=$((duration % 60))

    echo ""
    echo -e "  ${GREEN}${BOLD}✓${RESET} ${GREEN}$label finished${RESET} ${DIM}(${minutes}m ${seconds}s)${RESET}"
    echo ""
}

run_claude_from_file() {
    local file="$1"
    local label="$2"

    if [ ! -f "$file" ]; then
        echo -e "${RED}Error: $file not found.${RESET}"
        exit 1
    fi

    run_claude "$(cat "$file")" "$label"
}

cmd_build() {
    local max_loops=${1:-0}

    banner
    echo -e "  ${CYAN}${BOLD}MODE: BUILD${RESET}"
    if [ "$max_loops" -gt 0 ]; then
        echo -e "  ${DIM}Running $max_loops loop(s) then stopping.${RESET}"
    else
        echo -e "  ${DIM}Running until Ctrl+C.${RESET}"
    fi
    echo ""

    local loop_count=0

    while true; do
        loop_count=$((loop_count + 1))

        echo -e "${CYAN}${BOLD}  ============================================${RESET}"
        if [ "$max_loops" -gt 0 ]; then
            echo -e "  ${WHITE}${BOLD}Build #$loop_count${RESET} ${DIM}of $max_loops  $(date '+%H:%M:%S')${RESET}"
        else
            echo -e "  ${WHITE}${BOLD}Build #$loop_count${RESET}  ${DIM}$(date '+%H:%M:%S')${RESET}"
        fi
        echo -e "${CYAN}${BOLD}  ============================================${RESET}"

        run_claude_from_file "PROMPT_BUILD.md" "Build #$loop_count"

        if [ "$max_loops" -gt 0 ] && [ "$loop_count" -ge "$max_loops" ]; then
            echo -e "  ${YELLOW}${BOLD}Ralph completed $max_loops build loop(s).${RESET}"
            echo -e "  ${DIM}Total commits: $(git rev-list --count HEAD 2>/dev/null || echo '?')${RESET}"
            exit 0
        fi

        echo -e "  ${DIM}Next loop in ${SLEEP_BETWEEN}s...${RESET}"
        sleep $SLEEP_BETWEEN
    done
}

cmd_plan() {
    banner
    echo -e "  ${CYAN}${BOLD}MODE: PLAN${RESET}"
    echo ""
    run_claude_from_file "PROMPT_PLAN.md" "Planning"
}

cmd_fix() {
    local description="$1"

    if [ -z "$description" ]; then
        echo -e "${RED}Error: provide a bug description.${RESET}"
        echo -e "${DIM}Usage: ./ralph.sh fix \"description of the bug\"${RESET}"
        exit 1
    fi

    banner
    echo -e "  ${CYAN}${BOLD}MODE: FIX${RESET}"
    echo -e "  ${DIM}$description${RESET}"
    echo ""

    if [ ! -f "PROMPT_FIX.md" ]; then
        echo -e "${RED}Error: PROMPT_FIX.md not found.${RESET}"
        exit 1
    fi

    local prompt="$(cat PROMPT_FIX.md)

## Bug to fix
$description"

    run_claude "$prompt" "Fix"
}

cmd_help() {
    banner
    echo -e "  ${WHITE}${BOLD}Commands:${RESET}"
    echo ""
    echo -e "  ${CYAN}build${RESET} ${DIM}[n]${RESET}              Build loop. Runs forever or n times."
    echo -e "  ${CYAN}plan${RESET}                   Audit codebase, regenerate fix_plan.md."
    echo -e "  ${CYAN}fix${RESET}  ${DIM}\"description\"${RESET}     One-shot targeted bug fix."
    echo -e "  ${CYAN}help${RESET}                   Show this help."
    echo ""
    echo -e "  ${WHITE}${BOLD}Examples:${RESET}"
    echo ""
    echo -e "  ${DIM}./ralph.sh build${RESET}              # build forever"
    echo -e "  ${DIM}./ralph.sh build 5${RESET}            # build 5 loops"
    echo -e "  ${DIM}./ralph.sh plan${RESET}               # regenerate the plan"
    echo -e "  ${DIM}./ralph.sh fix \"shopping list bug\"${RESET}"
    echo ""
}

case "${1:-help}" in
    build)  cmd_build "$2" ;;
    plan)   cmd_plan ;;
    fix)    cmd_fix "$2" ;;
    help)   cmd_help ;;
    *)
        echo -e "${RED}Unknown command: $1${RESET}"
        cmd_help
        exit 1
        ;;
esac
