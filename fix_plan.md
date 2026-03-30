# Fix plan

## Priority items (not yet implemented)

### Phase 1: Solution scaffolding
- [x] Create solution and all project skeletons with correct dependencies (Domain has zero refs to other projects)

### Phase 2: Domain layer
- [x] Implement IngredientAggregator domain service per specs/domain-model.md
- [x] Implement RecipeMatcher domain service per specs/domain-model.md
- [x] Write domain unit tests for IngredientAggregator
- [x] Write domain unit tests for RecipeMatcher

### Phase 3: Infrastructure layer
- [x] Implement EF Core entity configurations (fluent API)
- [x] Implement RecipeRepository
- [x] Implement MealPlanRepository
- [x] Implement ShoppingListRepository
- [x] Implement FridgeRepository
- [x] Implement RecipeScraper (JSON-LD + ingredient parsing) per specs/recipe-scraper.md
- [x] Write infrastructure integration tests for repositories

### Phase 4: Application layer
- [x] Create DTOs for all entities (request + response DTOs)
- [x] Implement RecipeService (CRUD + import)
- [x] Implement MealPlanService (CRUD + entry management)
- [x] Implement ShoppingListService (generate from meal plan, export)
- [x] Implement FridgeService (CRUD + recipe suggestions)
- [x] Write application layer unit tests

### Phase 5: API layer
- [x] Implement RecipesController per specs/api.md
- [x] Implement MealPlansController per specs/api.md
- [x] Implement ShoppingListsController per specs/api.md
- [x] Implement FridgeController per specs/api.md
- [x] Add validation and error handling (ProblemDetails)
- [x] Write API integration tests using WebApplicationFactory

### Phase 6: Blazor frontend
- [x] Set up Web project with HttpClient configuration pointing to API
- [x] Create typed API client services (RecipeApiClient, MealPlanApiClient, etc)
- [x] Build recipe list page with search and category filter per specs/blazor-frontend.md
- [x] Build recipe detail page per specs/blazor-frontend.md
- [x] Build add/edit recipe page with dynamic ingredient rows per specs/blazor-frontend.md
- [x] Build recipe import (URL scrape) flow per specs/blazor-frontend.md
- [x] Build meal planner week view per specs/blazor-frontend.md
- [x] Build shopping list page with tick-off and export per specs/blazor-frontend.md
- [x] Build "What can I make?" suggestions page per specs/blazor-frontend.md
- [x] Build navigation layout (sidebar/top nav) per specs/blazor-frontend.md
- [x] Responsive polish — verify all pages work on mobile

### Phase 7: Deployment
- [x] Create Dockerfiles for API and Web projects
- [x] Verify full docker-compose up works end-to-end
- [x] Add auto-migration on API startup

## Completed
- [x] Implement MealPlanRepository
- [x] Implement RecipeMatcher domain service per specs/domain-model.md
- [x] Write domain unit tests for RecipeMatcher
- [x] Implement IngredientAggregator domain service per specs/domain-model.md
- [x] Write domain unit tests for IngredientAggregator
- [x] Create solution and all project skeletons with correct dependencies
- [x] Set up docker-compose.yml with Postgres
- [x] Configure EF Core DbContext and initial migration
- [x] Set up test projects with NUnit + Awesome Assertions + AutoFixture + NSubstitute
- [x] Implement Recipe entity and RecipeIngredient value object per specs/domain-model.md
- [x] Implement RecipeCategory and ShoppingCategory enums
- [x] Implement MealPlan and MealPlanEntry entities per specs/domain-model.md
- [x] Implement ShoppingList and ShoppingListItem entities per specs/domain-model.md
- [x] Implement FridgeItem entity per specs/domain-model.md
- [x] Define repository interfaces in Domain (IRecipeRepository, IMealPlanRepository, etc)
- [x] Write infrastructure integration tests for repositories (41 tests across 4 repositories using Testcontainers/PostgreSQL)
- [x] Implement RecipeService (CRUD + import) with IRecipeService interface, DI registration, and 10 unit tests
- [x] Implement ShoppingListService (generate from meal plan, export, toggle-checked) with IShoppingListService interface and DI registration
- [x] Implement FridgeService (CRUD + recipe suggestions) with IFridgeService interface, DI registration, and 9 unit tests
- [x] Write application layer unit tests (48 tests across 4 services: RecipeService, MealPlanService, ShoppingListService, FridgeService)
- [x] Write API integration tests using WebApplicationFactory (35 tests across 4 controllers using Testcontainers/PostgreSQL)
- [x] Add auto-migration on API startup (MealPlannerDbContext.Database.MigrateAsync() called in Program.cs)

## Backlog

- [ ] **Meal plan mobile layout** — the week table is a horizontal scroll on mobile, hard to use. Consider a day-by-day stacked view on small screens instead.
- [ ] **Numbered list for recipe instructions** — instructions are stored/displayed as a plain text blob. Support a numbered list format (either newline-delimited steps or markdown-style) so users can enter and read step-by-step instructions clearly.
- [ ] **500 error on recipe import** — POST to `/api/recipes/import` returns 500. Check RecipesController Import action, RecipeScraper (JSON-LD parsing), and any unhandled exceptions for URLs that don't match expected schema.

## Known issues
- tag, page, pageSize query params on GET /api/recipes accepted but not forwarded to service (service interface doesn't support them)
- 4 pre-existing API integration test failures (MealPlanner.Api.Tests): Create_WithMissingName_Returns400 (RecipesController), Create_WithMissingInstructions_Returns400 (RecipesController), Export_WithExistingId_ReturnsPlainText, UpdateItem_ToggleChecked_Returns204 — these failures predate the responsive polish changes

## Resolved issues
- ~~DELETE /api/fridge (clear all) not implemented — IFridgeService has no ClearAllAsync method~~ — fixed: ClearAllAsync added to IFridgeService, FridgeService, IFridgeRepository, FridgeRepository, and FridgeController; returns 204 NoContent
- ~~Web project imports MealPlanner.Domain.Enums directly (_Imports.razor) — violates DDD layering~~ — fixed: Application.DTOs enums (RecipeCategory, ShoppingCategory, MealType) already exist; Web _Imports.razor references MealPlanner.Application.DTOs only
- ~~500 error when adding meal plan entry~~ — fixed: UpdateAsync in MealPlanRepository now explicitly adds detached entries to the DbSet before SaveChangesAsync, preventing EF Core from marking new entries as Modified instead of Added
