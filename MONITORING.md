# Monitoring Ralph — cheat sheet

All commands from the project root in a second terminal.

## Quick health check
```bash
git log --oneline
cat fix_plan.md
dotnet build
dotnet test tests/MealPlanner.Domain.Tests
```

## Watch progress live
```bash
watch -n 5 'git log --oneline -15'
watch -n 2 cat fix_plan.md
```

## Inspect last loop
```bash
git diff HEAD~1
git diff HEAD~1 --stat
```

## Check DDD layering
```bash
# Domain should NOT reference any other project
grep -r "MealPlanner.Infrastructure\|MealPlanner.Api\|MealPlanner.Web\|MealPlanner.Application" src/MealPlanner.Domain/ --include="*.cs" --include="*.csproj"

# Should return nothing. If it returns results, layering is broken.
```

## Check for problems
```bash
# Placeholders
grep -r "TODO\|NotImplemented\|throw new\|placeholder" src/ --include="*.cs" | head -20

# Missing tests
find tests/ -name "*Tests.cs" | wc -l

# Component libraries that shouldn't be there
grep -r "MudBlazor\|Radzen" src/ --include="*.csproj" --include="*.cs"
```

## Rolling back
```bash
git log --oneline -20
git reset --hard HEAD~3       # go back 3 commits
# Then run: ./ralph.sh plan   # regenerate the plan
```

## Docker
```bash
docker compose up -d postgres          # just the database
docker compose up --build              # everything
docker compose logs -f api             # watch API logs
docker compose down                    # stop all
docker compose down -v                 # stop + delete data
```

## Run tests
```bash
dotnet test                                    # all
dotnet test tests/MealPlanner.Domain.Tests     # domain only (fast)
dotnet test tests/MealPlanner.Api.Tests        # API integration
```
