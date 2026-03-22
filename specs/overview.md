# Meal Planner — overview

## Purpose
A meal planning and shopping list app for a small household. Users add recipes, plan meals for the week, scale portions based on how many people are eating, and generate a categorised shopping list they can take to the supermarket. Supports checking what meals can be made with what's currently in the fridge.

## Users
Small household (2-4 people). No authentication — everyone sees everything. Default serving size is 2, adjustable per meal.

## Architecture

### Solution structure (DDD)
```
MealPlanner.sln
├── src/
│   ├── MealPlanner.Domain/          # Entities, value objects, domain services, interfaces
│   ├── MealPlanner.Application/     # Use cases, DTOs, service interfaces, mapping
│   ├── MealPlanner.Infrastructure/  # EF Core, PostgreSQL, recipe scraping, external integrations
│   ├── MealPlanner.Api/             # REST API (ASP.NET Core Web API)
│   └── MealPlanner.Web/             # Blazor Web App (Server), consumes the API via HttpClient
├── tests/
│   ├── MealPlanner.Domain.Tests/
│   ├── MealPlanner.Application.Tests/
│   ├── MealPlanner.Infrastructure.Tests/
│   └── MealPlanner.Api.Tests/       # Integration tests
└── docker-compose.yml
```

### Key principles
- **Domain project has ZERO dependencies** on Infrastructure, Api, or Web. It defines interfaces that other layers implement.
- **Application project** depends only on Domain. Contains use cases (services), DTOs, and mapping logic.
- **Infrastructure project** implements Domain interfaces. Contains EF Core DbContext, repository implementations, the recipe scraper, and any external service clients.
- **Api project** is a thin HTTP layer over Application services. Controllers accept DTOs, call Application services, return DTOs.
- **Web project** is a Blazor Web App (Server) that consumes the API via HttpClient. It does NOT reference Domain or Infrastructure directly.

### Tech stack
- .NET 8
- Blazor Web App (Server-side rendering with interactive server components)
- ASP.NET Core Web API
- Entity Framework Core with PostgreSQL (Npgsql)
- Docker Compose for local development (Postgres + API + Web)
- NUnit + Awesome Assertions + AutoFixture + NSubstitute for testing

### Hosting
Designed to run in Docker Compose on a small VPS so it's accessible from a phone at the supermarket. The Blazor frontend and API both run in containers alongside Postgres.
