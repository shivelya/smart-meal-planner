# Meal Planner App - User Stories & API Contracts

## 🔐 Authentication & Account Management

### Story 1: User Login
*As a registered user, I want to log in with my email and password, so that I can access my account securely.*

**Acceptance Criteria**
- Given valid credentials, when I submit the login form, then I am authenticated and receive a token/session.
- Given invalid credentials, when I submit the login form, then I see an error message and I am not logged in.

**API**
- `POST /auth/login`

**Request**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response (200)**
```json
{
  "access-token": "jwt-token-here",
  "refresh-token": 
  }
}
```

**Response (401)**
```json
{ "error": "Invalid credentials" }
```

---

### Story 2: User Logout
*As a logged-in user, I want to log out, so that my session is closed and my account is secure.*

**Acceptance Criteria**
- When I log out, my session token is invalidated.
- After logging out, I cannot access protected routes without logging in again.

**API**
- `POST /auth/logout`

**Request**
```json
"refresh-token"

**Response (200)**
```json
{ "message": "Logged out successfully" }
```

---

### Story 3: Change Password
*As a logged-in user, I want to change my password, so that I can keep my account secure.*

**Acceptance Criteria**
- When I provide my old password and a new password, my password is updated.
- If my old password is incorrect, I get an error.

**API**
- `PUT /auth/change-password`

**Request**
```json
{
  "oldPassword": "Password123!",
  "newPassword": "NewPassword456!"
}
```

**Response (200)**
```json
{ "message": "Password updated successfully" }
```

---

### Story 4: Reset Forgotten Password
*As a user who forgot my password, I want to reset it, so that I can regain access to my account.*

**Acceptance Criteria**
- When I request a reset, I receive a reset link/token via email.
- When I submit a new password with a valid token, my password is updated.
- If the token is expired or invalid, I get an error.

**API**
- `POST /auth/forgot-password`
```json
{ "email": "user@example.com" }
```

- `POST /auth/reset-password`
```json
{
  "token": "reset-token-here",
  "newPassword": "Password123!"
}
```

**Response (200)**
```json
{ "message": "Password reset successful" }
```

---

### Story 5: Token Management
*As a user, I don't want to have to log in every time I visit the site.

**Acceptance Criteria**
- While using the system, I am not suddenly logged out.
- If I come back to the system the next day, I am not logged out.
- If I come back to the system a week later, I am logged out.

**API**
- `POST /auth/refresh`
```json
"refresh-token"
```

**Response (200)**
```json
{
  "refreshToken": "new-refresh-token",
  "accessToken": "new-access-token"
}
```

**Response (401)**
```json
{ "error": "Invalid token" }
```

---

## 🥫 Pantry Management

### Story 5: View Pantry
*As a logged-in user, I want to view all my pantry items, so that I can see what ingredients I currently have.*

**Acceptance Criteria**
- When I open the pantry screen, I see a list of all my pantry items.
- Each item shows name, quantity, unit, and category.

**API**
- `GET /pantry/items`

**Response (200)**
```json
[
  { "id": 1, "name": "Tomato", "quantity": 3, "unit": "pcs", "category": "Vegetable" },
  { "id": 2, "name": "Olive Oil", "quantity": 1, "unit": "bottle", "category": "Condiment" }
]
```

---

### Story 6: Add Pantry Item
*As a logged-in user, I want to add items to my pantry, so that I can track ingredients I have on hand.*

**Acceptance Criteria**
- When I submit a new item, it is added to my pantry list.
- The item must have at least a name and quantity.

**API**
- `POST /pantry/items`

**Request**
```json
{ "name": "Onion", "quantity": 2, "unit": "pcs", "category": "Vegetable" }
```

**Response (201)**
```json
{ "id": 3, "name": "Onion", "quantity": 2, "unit": "pcs", "category": "Vegetable" }
```

---

## User Story: Ingredient Selection for Pantry Items

**As a** user,  
**I want** to select or add ingredients when creating a pantry item,  
**so that** I can easily track the foods I have without worrying about duplicates or database structure.

### Acceptance Criteria

1. When creating a new pantry item, the user can start typing the name of an ingredient.
2. The system should search the database for matching ingredients and suggest them in real-time (autocomplete).
3. If the ingredient exists:
   - Selecting it will populate the pantry item with the corresponding `IngredientId`.
4. If the ingredient does not exist:
   - The user can enter the new ingredient name.
   - The system will create the new ingredient in the database before creating the pantry item.
5. From the user’s perspective, they do not need to distinguish between ingredients and pantry items; they only interact with ingredient names.
6. On the backend, all ingredients are stored in a single table for searchability and consistency.

### Notes

- Ingredient fields: `Id`, `Name`, `Category`
- PantryItem fields: `Id`, `IngredientId`, `Quantity`, `Unit`
- The backend should expose a search endpoint: `GET /ingredients?search=<text>`
- Pantry item creation should accept either an `IngredientId` or an `IngredientName`

---

### Story 7: Edit Pantry Item
*As a logged-in user, I want to edit a pantry item, so that I can adjust quantities or fix mistakes.*

**Acceptance Criteria**
- When I edit an item and save, the pantry list updates with new values.
- If I try to save invalid data (e.g., negative quantity), I get an error.

**API**
- `PUT /pantry/items/{id}`

**Request**
```json
{ "quantity": 5, "unit": "pcs" }
```

**Response (200)**
```json
{ "id": 1, "name": "Tomato", "quantity": 5, "unit": "pcs", "category": "Vegetable" }
```

---

### Story 8: Remove Pantry Item
*As a logged-in user, I want to remove items from my pantry, so that I can keep my pantry up to date.*

**Acceptance Criteria**
- When I delete an item, it no longer appears in my pantry list.
- If the item doesn’t exist, I get an error.

**API**
- `DELETE /pantry/items/{id}`

**Response (204)**
_No content_

---

## 📅 Meal Plans

### Story 9: View Saved Meal Plans
*As a logged-in user, I want to view my saved meal plans, so that I can reuse or review them.*

**Acceptance Criteria**
- When I open meal plans, I see a list of all my saved plans.
- Each plan shows its name, creation date, and meals included.

**API**
- `GET /meal-plans`

**Response (200)**
```json
[
  {
    "id": 10,
    "name": "Weekly Plan Aug 12",
    "createdAt": "2025-08-12",
    "meals": [
      { "day": "Monday", "meal": "Spaghetti" },
      { "day": "Tuesday", "meal": "Chicken Stir Fry" }
    ]
  }
]
```

---

### Story 10: Generate Meal Plan from Pantry
*As a logged-in user, I want to generate a meal plan based on my pantry items, so that I can make meals with ingredients I already have.*

**Acceptance Criteria**
- When I request a generated plan, meals are suggested that use my pantry items.
- The plan is saved and can be viewed later.
- If no suitable meals are found, I see a message.

**API**
- `POST /meal-plans/generate`

**Request**
```json
{ "days": 7, "mealsPerDay": 3 }
```

**Response (201)**
```json
{
  "id": 11,
  "name": "Generated Plan",
  "meals": [
    { "day": "Monday", "meal": "Tomato Soup" },
    { "day": "Tuesday", "meal": "Onion Frittata" }
  ]
}
```

---

## 🛒 Shopping List

### Story 11: View Shopping List
*As a logged-in user, I want to view the shopping list for a meal plan, so that I know what additional items to buy.*

**Acceptance Criteria**
- When I select a meal plan, I can see a generated shopping list.
- Items already in my pantry are marked as available (and not added to the list).
- The shopping list shows item names, quantities, and units.

**API**
- `GET /meal-plans/{id}/shopping-list`

**Response (200)**
```json
{
  "planId": 10,
  "items": [
    { "name": "Milk", "quantity": 1, "unit": "liter", "inPantry": false },
    { "name": "Onion", "quantity": 2, "unit": "pcs", "inPantry": true }
  ]
}
```

---

## 🍴 Recipes

### Story 12: Search Recipes by Keyword
*As a user, I want to search for recipes by keywords, so that I can find new meal ideas.*

**Acceptance Criteria**
- When I search by a term (e.g., “chicken”), I see a list of recipes.
- Each recipe shows title, image, and brief description.

**API**
- `GET /recipes?search=chicken`

**Response (200)**
```json
[
  { "id": 101, "title": "Chicken Curry", "image": "url.jpg" },
  { "id": 102, "title": "Grilled Chicken Salad", "image": "url.jpg" }
]
```

---

### Story 13: Search Recipes by Pantry Items
*As a logged-in user, I want to search for recipes using my pantry items, so that I can cook meals with what I have.*

**Acceptance Criteria**
- When I search by pantry ingredients, I see recipes that use those items.
- Recipes that use more of my pantry items are ranked higher.

**API**
- `GET /recipes?ingredients=tomato,onion,garlic`

**Response (200)**
```json
[
  { "id": 201, "title": "Tomato Soup", "image": "url.jpg", "matchCount": 3 },
  { "id": 202, "title": "Garlic Bread", "image": "url.jpg", "matchCount": 2 }
]
```

---

### Story 14: Save Recipes
*As a logged-in user, I want to save recipes I’ve found, so that I can access them later.*

**Acceptance Criteria**
- When I save a recipe, it appears in my saved recipes list.
- I can view saved recipes at any time.
- I can remove recipes from my saved list.

**API**
- `POST /recipes/saved`
```json
{ "recipeId": 101 }
```

- `GET /recipes/saved`
```json
[
  { "id": 101, "title": "Chicken Curry", "image": "url.jpg" }
]
```

- `DELETE /recipes/saved/{id}`

**Response (204)**
_No content_
