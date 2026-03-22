# Domain model specification

## Entities

### Recipe
The core entity. Represents a single recipe.

```csharp
- Id: Guid
- Name: string (required, max 200 chars)
- Description: string (optional, max 2000 chars)
- Category: RecipeCategory (enum)
- Servings: int (default serving size the recipe is written for)
- PrepTimeMinutes: int?
- CookTimeMinutes: int?
- Instructions: string (the method, stored as text — can be multi-paragraph)
- SourceUrl: string? (if imported from a URL)
- Ingredients: List<RecipeIngredient>
- Tags: List<string> (e.g. "quick", "vegetarian", "freezer-friendly")
- CreatedAt: DateTime
- UpdatedAt: DateTime
```

### RecipeCategory (enum)
```
Breakfast, Lunch, Dinner, Snack, Dessert, Side, Drink
```

### RecipeIngredient
A value object belonging to a Recipe. Represents one line in the ingredient list.

```csharp
- Name: string (e.g. "chicken breast")
- Quantity: decimal (e.g. 2)
- Unit: string (e.g. "kg", "ml", "tbsp", "pieces", "" for unitless)
- ShoppingCategory: ShoppingCategory (enum — which aisle this belongs to)
- Optional: bool (e.g. "garnish with parsley" is optional)
```

### ShoppingCategory (enum)
Used to group items on the shopping list by supermarket section.
```
FruitAndVeg, Meat, Fish, Dairy, Bakery, Tinned, Dried, Frozen,
Condiments, Drinks, Snacks, Household, Other
```

### MealPlan
Represents a week of planned meals.

```csharp
- Id: Guid
- WeekStartDate: DateOnly (Monday of the week)
- Entries: List<MealPlanEntry>
- CreatedAt: DateTime
```

### MealPlanEntry
A single meal slot in the plan.

```csharp
- Id: Guid
- MealPlanId: Guid
- Date: DateOnly
- MealType: MealType (enum: Breakfast, Lunch, Dinner, Snack)
- RecipeId: Guid
- Servings: int (how many people are eating — overrides recipe default)
```

### ShoppingList
Generated from a MealPlan. Aggregates and deduplicates ingredients.

```csharp
- Id: Guid
- MealPlanId: Guid
- Items: List<ShoppingListItem>
- GeneratedAt: DateTime
```

### ShoppingListItem
```csharp
- Id: Guid
- ShoppingListId: Guid
- IngredientName: string
- TotalQuantity: decimal (aggregated across recipes, scaled by servings)
- Unit: string
- Category: ShoppingCategory
- IsChecked: bool (ticked off in the shop)
- FromRecipes: List<string> (which recipes need this — for reference)
```

### FridgeItem
Tracks what's currently available at home.

```csharp
- Id: Guid
- Name: string
- Quantity: decimal?
- Unit: string?
- AddedAt: DateTime
```

## Domain services

### IngredientAggregator
Takes a list of MealPlanEntries (with their recipes and serving overrides), scales each recipe's ingredients by the servings ratio, then aggregates matching ingredients across recipes (e.g. if two recipes need onions, combine them into one shopping list line).

Matching logic: ingredients match if their normalised name AND unit match. "Onion" and "onions" should match. "200g chicken" and "500g chicken" should combine to "700g chicken".

### RecipeMatcher
Given a list of FridgeItems, scores each recipe by how many of its required (non-optional) ingredients are available. Returns recipes sorted by match percentage:
- 100% = you can make this right now
- 70-99% = you're only missing a few things
- Below 70% = probably not worth it

## Domain interfaces (implemented by Infrastructure)
```csharp
IRecipeRepository
IMealPlanRepository
IShoppingListRepository
IFridgeRepository
IRecipeScraper
```
