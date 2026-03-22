# Testing specification

## Stack
- NUnit (test framework)
- Awesome Assertions (fluent assertions)
- AutoFixture (test data generation)
- NSubstitute (mocking)

## Test projects

### MealPlanner.Domain.Tests
Unit tests for domain logic. No dependencies on Infrastructure or EF Core.

Test coverage:
- **IngredientAggregator**: scaling ingredients by servings ratio, aggregating matching ingredients across recipes, handling different units, normalising ingredient names (singular/plural), handling edge cases (zero servings, empty ingredients)
- **RecipeMatcher**: scoring recipes against fridge contents, sorting by match percentage, handling optional ingredients (shouldn't count against match), edge cases (empty fridge, no recipes)
- **Entity validation**: Recipe requires a name, at least one ingredient, servings >= 1, etc.

Use AutoFixture for generating test recipes and ingredients.
Use NSubstitute only if domain services depend on interfaces (they shouldn't, mostly).

### MealPlanner.Application.Tests
Unit tests for application services / use cases.

Test coverage:
- Service methods call the correct repository methods
- DTOs map correctly to/from domain entities
- Validation logic in application layer
- Shopping list generation (end-to-end through the use case)
- Recipe import flow (scrape → parse → create)

Use NSubstitute to mock repository interfaces.
Use AutoFixture for generating DTOs and entities.

### MealPlanner.Infrastructure.Tests
Integration tests for EF Core repositories and the recipe scraper.

Test coverage:
- Repository CRUD operations against a real Postgres database (use Testcontainers for Postgres)
- EF Core migrations apply cleanly
- Recipe scraper against known URLs (may be fragile, mark as [Category("Integration")])
- Ingredient parsing from real recipe strings

### MealPlanner.Api.Tests
Integration tests for API endpoints.

Test coverage:
- All CRUD endpoints return correct status codes
- Validation errors return 400 with ProblemDetails
- Shopping list generation from a meal plan
- Recipe import endpoint
- Fridge suggestions endpoint
- Use WebApplicationFactory<Program> for in-process API testing

## Conventions
- Test class name: `{ClassUnderTest}Tests`
- Test method name: `{MethodName}_Should{ExpectedBehaviour}_When{Condition}`
- Example: `ScaleIngredients_ShouldDoubleQuantities_WhenServingsIsDoubled`
- Each test should have Arrange/Act/Assert sections (comments optional but welcome)
- When writing tests, include XML doc comments explaining WHY the test exists and what behaviour it protects. This helps future Ralph loops understand whether a test is still relevant.

## Running tests
```bash
# All tests
dotnet test

# Just domain tests (fast, no dependencies)
dotnet test tests/MealPlanner.Domain.Tests

# Just integration tests
dotnet test tests/MealPlanner.Api.Tests
```

## Test data
Use AutoFixture with customisations for domain entities:
```csharp
var fixture = new Fixture();
fixture.Customize<Recipe>(c => c
    .With(r => r.Servings, 4)
    .With(r => r.Category, RecipeCategory.Dinner)
    .With(r => r.Ingredients, fixture.CreateMany<RecipeIngredient>(3).ToList()));
```
