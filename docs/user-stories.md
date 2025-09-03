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

### User Story 1: Pantry Item from Packaged Food (e.g., Box of Crackers)

**As a** user  
**I want** to take a picture of a packaged food item, such as a box of crackers  
**So that** the system can read the packaging, identify it as "crackers," and automatically create a pantry item entry for me  

**Acceptance Criteria:**
- Given I take a photo of a packaged food item  
- When the system processes the image  
- Then it recognizes the product type (e.g., crackers)  
- And it generates a pantry item with the appropriate name and details  

**Extended Acceptance Criteria:**
- The system should attempt to extract additional details from the packaging (e.g., brand, weight, flavor).  
- If the system is highly confident (above a set threshold), it auto-populates the pantry item.  
- If confidence is low, the system should prompt the user to confirm or correct the item.  
- The pantry item should include default quantity (e.g., 1 box) unless more detail is detected.  
- The user should be able to edit the generated pantry item before saving.  

---

### User Story 2: Pantry Item from Fresh Food (e.g., Banana)

**As a** user  
**I want** to take a picture of a fresh food item, such as a banana  
**So that** the system can recognize it as "banana" and automatically create a pantry item entry for me  

**Acceptance Criteria:**
- Given I take a photo of a fresh food item  
- When the system processes the image  
- Then it recognizes the food type (e.g., banana)  
- And it generates a pantry item with the appropriate name and details  

**Extended Acceptance Criteria:**
- The system should distinguish between different fresh foods (e.g., banana vs. plantain).  
- If the system is highly confident (above a set threshold), it auto-populates the pantry item.  
- If confidence is low, the system should prompt the user to confirm or correct the item.  
- The pantry item should default to a unit type appropriate for fresh produce (e.g., “1 banana” or “1 bunch”).  
- The user should be able to edit the generated pantry item before saving.  

---

### User Story: Search Pantry Items by Name

**As a** user  
**I want** to search for pantry items by entering their name  
**So that** I can quickly find a specific item in my pantry list without scrolling  

**Acceptance Criteria:**
- Given I am on the pantry screen  
- When I type a name (full or partial) into the search bar  
- Then the pantry list updates to show only matching items  
- And the search is case-insensitive (e.g., "Crackers" matches "crackers")  

**Extended Acceptance Criteria:**
- The system should support partial matches (e.g., typing "crack" returns "crackers").  
- The system should update search results in real-time as the user types.  
- If no items match the search, the system should display a "No results found" message.  
- The user should be able to clear the search input to return to the full list.  
- The system should handle special characters and spacing gracefully (e.g., "mac & cheese" vs "mac and cheese").  

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
- Each recipe shows title and brief description.

**API**
- `GET /recipes?search=chicken`

**Response (200)**
```json
[
  { "id": 101, "title": "Chicken Curry" },
  { "id": 102, "title": "Grilled Chicken Salad" }
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

---

### User Story: Import Recipe from URL

**As a** user  
**I want** to provide the app with a URL to a recipe  
**So that** the app can extract the recipe details and let me verify them before saving to my collection  

**Acceptance Criteria:**
- Given I paste a valid recipe URL into the app  
- When the system processes the URL  
- Then it extracts recipe details (e.g., title, ingredients, steps, cooking time)  
- And it displays the extracted recipe client-side for my review  
- And I must confirm or edit the recipe before saving it to my collection  

**Extended Acceptance Criteria:**
- The system should validate that the URL is accessible before attempting extraction.  
- If the recipe site is supported, the system should extract structured data (title, ingredients, instructions, nutrition info if available).  
- If the recipe site is unsupported or parsing fails, the system should provide a clear error message.  
- The user should be able to edit any extracted fields before saving.  
- The system should not save the recipe to the database until the user explicitly confirms.  
- If the user cancels, the extracted recipe should be discarded without saving.  

---

### User Story: Import Recipe from Text

**As a** user  
**I want** to paste or type a recipe into the app as plain text  
**So that** the app can parse the text into structured recipe details and let me verify them before saving to my collection  

**Acceptance Criteria:**
- Given I paste or type a recipe into the app  
- When the system processes the text  
- Then it extracts recipe details (e.g., title, ingredients, steps, cooking time)  
- And it displays the structured recipe client-side for my review  
- And I must confirm or edit the recipe before saving it to my collection  

**Extended Acceptance Criteria:**
- The system should attempt to parse common recipe formats (e.g., ingredients list, numbered steps).  
- If parsing succeeds, structured fields (title, ingredients, instructions, etc.) are pre-filled.  
- If parsing fails or is incomplete, the system should allow me to manually adjust or add missing fields.  
- The user should be able to freely edit any extracted fields before saving.  
- The system should not save the recipe to the database until the user explicitly confirms.  
- If the user cancels, the extracted recipe should be discarded without saving.  
