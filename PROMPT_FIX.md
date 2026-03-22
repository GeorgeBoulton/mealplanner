Study the codebase using subagents to understand the current implementation.

Your task is to fix ONE specific bug described below. Do not do anything
else — no refactoring, no new features, no "while I'm here" improvements.

Steps:
1. Read the bug description below.
2. Use subagents to search the codebase and find the relevant code.
3. Identify the root cause.
4. Spawn a builder subagent to fix it.
5. The builder must run `dotnet build` and `dotnet test` to verify.
6. Spawn a reviewer subagent to verify the fix addresses the bug and
   hasn't broken DDD layering or introduced regressions.
7. If the fix resolves any items in fix_plan.md, update it.
8. Stage with "git add -A" and commit describing the bug and fix.

Rules:
- Fix ONLY the described bug. Nothing else.
- Do not refactor surrounding code.
- Do not install any new packages.
- If you discover other issues, note them in fix_plan.md but do not
  fix them now.
- Tests must pass before committing.
