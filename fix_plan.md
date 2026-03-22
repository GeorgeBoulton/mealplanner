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
- [ ] Implement FridgeRepository
- [ ] Implement RecipeScraper (JSON-LD + ingredient parsing) per specs/recipe-scraper.md
- [ ] Write infrastructure integration tests for repositories

### Phase 4: Application layer
- [ ] Create DTOs for all entities (request + response DTOs)
- [ ] Implement RecipeService (CRUD + import)
- [ ] Implement MealPlanService (CRUD + entry management)
- [ ] Implement ShoppingListService (generate from meal plan, export)
- [ ] Implement FridgeService (CRUD + recipe suggestions)
- [ ] Write application layer unit tests

### Phase 5: API layer
- [ ] Implement RecipesController per specs/api.md
- [ ] Implement MealPlansController per specs/api.md
- [ ] Implement ShoppingListsController per specs/api.md
- [ ] Implement FridgeController per specs/api.md
- [ ] Add validation and error handling (ProblemDetails)
- [ ] Write API integration tests using WebApplicationFactory

### Phase 6: Blazor frontend
- [ ] Set up Web project with HttpClient configuration pointing to API
- [ ] Create typed API client services (RecipeApiClient, MealPlanApiClient, etc)
- [ ] Build recipe list page with search and category filter per specs/blazor-frontend.md
- [ ] Build recipe detail page per specs/blazor-frontend.md
- [ ] Build add/edit recipe page with dynamic ingredient rows per specs/blazor-frontend.md
- [ ] Build recipe import (URL scrape) flow per specs/blazor-frontend.md
- [ ] Build meal planner week view per specs/blazor-frontend.md
- [ ] Build shopping list page with tick-off and export per specs/blazor-frontend.md
- [ ] Build "What can I make?" suggestions page per specs/blazor-frontend.md
- [ ] Build navigation layout (sidebar/top nav) per specs/blazor-frontend.md
- [ ] Responsive polish — verify all pages work on mobile

### Phase 7: Deployment
- [ ] Create Dockerfiles for API and Web projects
- [ ] Verify full docker-compose up works end-to-end
- [ ] Add auto-migration on API startup

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

## Known issues
(none yet)
