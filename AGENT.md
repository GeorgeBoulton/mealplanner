# Meal Planner

## Tech stack
- .NET 8
- Blazor Web App (Server)
- ASP.NET Core Web API
- Entity Framework Core + PostgreSQL (Npgsql)
- NUnit + Awesome Assertions + AutoFixture + NSubstitute
- Docker Compose

## Solution structure
```
MealPlanner.sln
├── src/
│   ├── MealPlanner.Domain/          # Entities, value objects, domain services, interfaces
│   ├── MealPlanner.Application/     # Use cases, DTOs, mapping
│   ├── MealPlanner.Infrastructure/  # EF Core, repos, scraper
│   ├── MealPlanner.Api/             # REST API
│   └── MealPlanner.Web/             # Blazor frontend
├── tests/
│   ├── MealPlanner.Domain.Tests/
│   ├── MealPlanner.Application.Tests/
│   ├── MealPlanner.Infrastructure.Tests/
│   └── MealPlanner.Api.Tests/
└── docker-compose.yml
```

## Build
```bash
dotnet build
```

## Test
```bash
# All tests
dotnet test

# Domain tests only (fast, no external deps)
dotnet test tests/MealPlanner.Domain.Tests

# API integration tests
dotnet test tests/MealPlanner.Api.Tests
```

## Run (local development)
```bash
# Start Postgres
docker compose up -d postgres

# Apply migrations
dotnet ef database update -p src/MealPlanner.Infrastructure -s src/MealPlanner.Api

# Run API
dotnet run --project src/MealPlanner.Api

# Run Blazor (separate terminal)
dotnet run --project src/MealPlanner.Web
```

## Run (Docker Compose — everything)
```bash
docker compose up --build
```
- API: http://localhost:5100
- Web: http://localhost:5200

## Migrations
# Note: requires dotnet-ef installed globally: dotnet tool install --global dotnet-ef
```bash
# Create
dotnet ef migrations add <Name> -p src/MealPlanner.Infrastructure -s src/MealPlanner.Api

# Apply
dotnet ef database update -p src/MealPlanner.Infrastructure -s src/MealPlanner.Api
```

## Key conventions
- Domain project has ZERO dependencies on other projects
- All business logic lives in Domain
- Application layer contains use cases (services) and DTOs
- Infrastructure implements Domain interfaces
- API is a thin HTTP layer — controllers call Application services
- Web consumes the API via HttpClient, never references Domain directly
- One class per file
- All tests use NUnit + Awesome Assertions + AutoFixture + NSubstitute
