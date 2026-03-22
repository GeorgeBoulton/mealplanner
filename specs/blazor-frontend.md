# Blazor frontend specification

## Overview
Blazor Web App (Server) that consumes the API via HttpClient.
The Web project does NOT reference Domain or Infrastructure.
It has its own view models and maps from API DTOs.

## Pages

### Recipe book (`/recipes`)
- Grid/list of recipe cards showing: name, category badge, prep+cook time, servings, tags
- Search bar at top (searches name, description, tags)
- Category filter pills (All, Breakfast, Lunch, Dinner, etc)
- Click a card to view the full recipe
- "Add recipe" button in the top right
- Pagination or infinite scroll

### Recipe detail (`/recipes/{id}`)
- Full recipe view: name, description, category, times, servings
- Ingredient list with quantities and units
- Instructions (rendered as formatted text)
- "Edit" and "Delete" buttons
- "Add to meal plan" button — opens a quick picker (select day + meal type + servings)
- If imported from URL, show source link

### Add/edit recipe (`/recipes/new`, `/recipes/{id}/edit`)
- Form with all recipe fields
- Dynamic ingredient list: add/remove ingredient rows
- Each ingredient row: name, quantity, unit dropdown, shopping category dropdown, optional checkbox
- "Import from URL" option at the top — paste a URL, it fetches and pre-fills the form
- Save and Cancel buttons
- Validation matching the API rules

### Meal planner (`/mealplan`)
- Week view: 7 columns (Mon–Sun), rows for Breakfast, Lunch, Dinner, Snack
- Shows the current week by default, with prev/next week navigation
- Each cell shows the recipe name and servings count (e.g. "Spag Bol × 4")
- Click an empty cell to add a meal (opens recipe picker + servings input)
- Click a filled cell to edit servings or remove it
- Default servings: 2 (configurable somewhere, maybe a settings area or inline)
- "Generate shopping list" button — creates the list from all meals in this week's plan

### Shopping list (`/shopping-list`)
- Grouped by ShoppingCategory with headers (emoji + category name)
- Each item shows: ingredient name, total quantity + unit, which recipes need it (small text)
- Checkbox to tick items off (persisted immediately via API)
- Checked items move to bottom of their category or get struck through
- "Export" button — copies plain text version to clipboard (formatted for notes apps)
- "Clear checked" button to remove ticked items
- Mobile optimised — this is the page you'll use at the shop on your phone

### What can I make? (`/suggestions`)
- Shows current fridge contents as editable tags/pills (add/remove items)
- Quick add: text input, type ingredient name, press enter to add
- "Clear all" button
- Below the fridge contents, shows recipe suggestions sorted by match %
- Each suggestion shows: recipe name, match percentage bar, list of missing ingredients
- Click a suggestion to go to the recipe detail page
- "Add to meal plan" button on each suggestion

### Navigation
- Sidebar or top nav with links: Recipes, Meal Plan, Shopping List, What Can I Make?
- Active page highlighted
- Mobile: collapsible hamburger menu

## Design
- Clean, functional, not flashy. This is a utility app.
- Light theme (you're reading this in a supermarket under fluorescent lights)
- Readable font sizes (mobile-first)
- Responsive: works on phone, tablet, and desktop
- Use Bootstrap (it ships with the Blazor template) — don't fight the defaults
- Accent colour for interactive elements (buttons, links, active states)

## API communication
- Register HttpClient in Program.cs pointing to the API base URL
- Create a typed service layer in the Web project (e.g. RecipeService, MealPlanService)
  that wraps HttpClient calls and handles serialisation/error mapping
- In development, API and Web run on different ports via Docker Compose
