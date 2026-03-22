# Infrastructure and deployment specification

## Docker Compose (local development)

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: mealplanner
      POSTGRES_USER: mealplanner
      POSTGRES_PASSWORD: mealplanner_dev
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  api:
    build:
      context: .
      dockerfile: src/MealPlanner.Api/Dockerfile
    ports:
      - "5100:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=mealplanner;Username=mealplanner;Password=mealplanner_dev"
    depends_on:
      - postgres

  web:
    build:
      context: .
      dockerfile: src/MealPlanner.Web/Dockerfile
    ports:
      - "5200:8080"
    environment:
      ApiBaseUrl: "http://api:8080"
    depends_on:
      - api

volumes:
  pgdata:
```

## EF Core configuration

### DbContext
```csharp
public class MealPlannerDbContext : DbContext
{
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<MealPlan> MealPlans { get; set; }
    public DbSet<MealPlanEntry> MealPlanEntries { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }
    public DbSet<FridgeItem> FridgeItems { get; set; }
}
```

### Migrations
- Use code-first migrations
- Migration commands run from the Infrastructure project
- On startup, auto-apply pending migrations (fine for a small household app)

```bash
# Create migration
dotnet ef migrations add InitialCreate -p src/MealPlanner.Infrastructure -s src/MealPlanner.Api

# Apply migration
dotnet ef database update -p src/MealPlanner.Infrastructure -s src/MealPlanner.Api
```

### Entity configuration
- Use fluent API in separate IEntityTypeConfiguration<T> classes
- Recipe.Ingredients stored as owned entities (or a separate table with FK)
- Recipe.Tags stored as a JSON column (PostgreSQL supports this natively)
- ShoppingListItem.FromRecipes stored as a JSON column
- All string columns have explicit MaxLength set
- All entities have CreatedAt/UpdatedAt where applicable

## Connection string
Configured via environment variable in Docker Compose and appsettings.json for local dev:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mealplanner;Username=mealplanner;Password=mealplanner_dev"
  }
}
```

## Hosting (production)
Target: a cheap VPS (Hetzner, DigitalOcean, or similar) running Docker Compose.
The same docker-compose.yml works in production with:
- Stronger Postgres password via environment variable
- HTTPS via Caddy or nginx reverse proxy in front
- Volumes for persistent Postgres data

This is a household app, not enterprise — keep deployment simple.
