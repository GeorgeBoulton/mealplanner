# API specification

## Overview
RESTful API built with ASP.NET Core Web API. All endpoints return JSON.
The API is the single source of truth — the Blazor frontend consumes it
via HttpClient, and it could be consumed by a mobile app in future.

## Base URL
`/api`

## Endpoints

### Recipes

```
GET    /api/recipes                    # List all recipes (supports search + category filter)
GET    /api/recipes/{id}               # Get single recipe with ingredients
POST   /api/recipes                    # Create recipe (manual entry)
PUT    /api/recipes/{id}               # Update recipe
DELETE /api/recipes/{id}               # Delete recipe
POST   /api/recipes/import             # Import recipe from URL (scrape)
GET    /api/recipes/suggestions        # Get recipe suggestions based on fridge contents
```

#### Query parameters for GET /api/recipes
- `search` (string) — search by name, description, tags
- `category` (string) — filter by RecipeCategory
- `tag` (string) — filter by tag
- `page` (int, default 1)
- `pageSize` (int, default 20)

#### POST /api/recipes body
```json
{
  "name": "Spaghetti Bolognese",
  "description": "Classic Italian meat sauce",
  "category": "Dinner",
  "servings": 4,
  "prepTimeMinutes": 15,
  "cookTimeMinutes": 45,
  "instructions": "1. Brown the mince...",
  "tags": ["italian", "freezer-friendly"],
  "ingredients": [
    {
      "name": "beef mince",
      "quantity": 500,
      "unit": "g",
      "shoppingCategory": "Meat",
      "optional": false
    }
  ]
}
```

#### POST /api/recipes/import body
```json
{
  "url": "https://www.bbcgoodfood.com/recipes/spaghetti-bolognese"
}
```
Returns the parsed recipe in the same format as GET /api/recipes/{id}.
The user can then edit it before saving.

#### GET /api/recipes/suggestions
Uses the current fridge contents to find matching recipes.
Returns recipes sorted by match percentage (highest first).

Response includes:
```json
[
  {
    "recipe": { ... },
    "matchPercentage": 85,
    "missingIngredients": ["parmesan", "fresh basil"]
  }
]
```

### Meal plans

```
GET    /api/mealplans                  # List meal plans (most recent first)
GET    /api/mealplans/{id}             # Get meal plan with entries
GET    /api/mealplans/current          # Get or create meal plan for current week
POST   /api/mealplans                  # Create meal plan for a specific week
PUT    /api/mealplans/{id}             # Update meal plan
DELETE /api/mealplans/{id}             # Delete meal plan
POST   /api/mealplans/{id}/entries     # Add entry to meal plan
PUT    /api/mealplans/{id}/entries/{entryId}  # Update entry (change servings etc)
DELETE /api/mealplans/{id}/entries/{entryId}  # Remove entry
```

#### POST /api/mealplans/{id}/entries body
```json
{
  "date": "2026-03-23",
  "mealType": "Dinner",
  "recipeId": "guid-here",
  "servings": 4
}
```

### Shopping lists

```
POST   /api/mealplans/{id}/shopping-list    # Generate shopping list from meal plan
GET    /api/shopping-lists/{id}              # Get shopping list
PUT    /api/shopping-lists/{id}/items/{itemId}  # Update item (toggle checked)
DELETE /api/shopping-lists/{id}              # Delete shopping list
GET    /api/shopping-lists/{id}/export       # Export as plain text (for copy to notes app)
```

#### GET /api/shopping-lists/{id}/export
Returns plain text grouped by category, suitable for pasting into
Google Keep or Apple Notes:

```
🥦 Fruit & Veg
- 3 onions
- 500g mushrooms
- 1 lemon

🥩 Meat
- 500g beef mince
- 4 chicken breasts

🧀 Dairy
- 200ml double cream
- 100g parmesan
```

### Fridge

```
GET    /api/fridge                     # List all fridge items
POST   /api/fridge                     # Add item to fridge
PUT    /api/fridge/{id}                # Update item
DELETE /api/fridge/{id}                # Remove item
DELETE /api/fridge                     # Clear all fridge items
```

## Error handling
- 400 for validation errors (return ProblemDetails with field-level errors)
- 404 for not found
- 500 for unexpected errors (return ProblemDetails, no stack traces)

## Validation
- Recipe name required, max 200 chars
- At least one ingredient per recipe
- Ingredient quantity must be > 0
- MealPlanEntry date must fall within the meal plan's week
- Servings must be >= 1
