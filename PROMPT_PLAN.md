Study specs/* to understand the full project specification.

Study all source code in the project using subagents. Read through every
file and build a complete picture of what currently exists.

Your task is PLANNING ONLY. Do NOT implement anything. Do NOT change any
source code. Do NOT run the build.

Compare what exists against the specifications:
- What has been fully implemented and matches the spec?
- What has been partially implemented or has gaps?
- What is completely missing?
- What has been implemented but deviates from the spec?
- Does the DDD layering hold? Is there business logic leaking into
  controllers or infrastructure code?

Search the codebase thoroughly using subagents:
- Look for TODO and FIXME comments
- Look for placeholder or stub implementations
- Look for dead code or unused files
- Look for missing tests
- Look for Domain project having dependencies it shouldn't

Delete the existing fix_plan.md and create a fresh one with:
1. A "Completed" section listing what is done and working
2. A "Priority items" section with unchecked items sorted by importance
3. A "Known issues" section for bugs or deviations from spec

Each item must be specific and actionable. Include which spec file it
relates to. Example: "- [ ] Implement IngredientAggregator domain
service per specs/domain-model.md"

Sort: foundational work first (domain entities, then repos, then
application services, then API endpoints, then Blazor pages).
Dependencies before the things that depend on them.

After writing the new fix_plan.md, stage with "git add -A" and commit
with "Planning: regenerated fix_plan.md".
