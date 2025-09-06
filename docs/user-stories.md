# Meal Planner App - User Stories & API Contracts

Since this is a solo project, the main stakeholders I wrote user stories for are the end users (to capture their needs) and myself in the developer/maintainer role (to capture technical and sustainability needs).

## 🔐 Authentication & Account Management

### Story: User Registration
**As a** Non-registered user
**I want** to create an account
**so that** I can access the website.

**Acceptance Criteria**
- [ ] Given valid credentials, I create an account and am authenticated and receive a token/session.
- [ ] Given invalid credentials, when I submit the login form, then I see an error message and an account is not created.

**API**
- `POST /api/auth/register`

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
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here"
}
```

---

### Story: User Login
**As a** registered user
**I want** to log in with my email and password
**so that** I can access my information securely and others cannot.

**Acceptance Criteria**
- [ ] Given valid credentials, when I submit the login form, then I am authenticated and receive a token/session.
- [ ] Given invalid credentials, when I submit the login form, then I see an error message and I am not logged in.

**API**
- `POST /api/auth/login`

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
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here"
}
```

---

### Story: User Logout
**As a** logged-in user
**I want** to log out
**so that** my session is closed and my account is secure.

**Acceptance Criteria**
- [ ] When I log out, my session token is invalidated.
- [ ] After logging out, I cannot access protected routes without logging in again.

**API**
- `POST /api/auth/logout`

**Request**
```json
{ "refreshToken": "refresh-token" }
```

**Response (200)**

---

### Story: Change Password
**As a** logged-in user
**I want** to change my password
**so that** I can keep my account secure.

**Acceptance Criteria**
- [ ] When I provide my old password and a new password, my password is updated.
- [ ] If my old password is incorrect, I get an error.

**API**
- `PUT /api/auth/change-password`

**Request**
```json
{
  "oldPassword": "Password123!",
  "newPassword": "NewPassword456!"
}
```

**Response (200)**

---

### Story: Reset Forgotten Password
**As a** Registered user who forgot my password
**I want** to reset it
**so that** I can regain access to my account.

**Acceptance Criteria**
- [ ] When I request a reset, I receive a reset link via email.
- [ ] When I submit a new password with a valid token, my password is updated.
- [ ] If the token is expired or invalid, I get an error.

**API**
- `POST /api/auth/forgot-password`
```json
{ "email": "user@example.com" }
```

**Response (200)**

- `POST /api/auth/reset-password`
```json
{
  "resetCode": "reset-token-here",
  "newPassword": "Password123!"
}
```

**Response (200)**

---

### Story: Token Management
**As a** registered user
**I want** to not have to log in every time I visit the site
**so that** my time isn't wasted on unndeeded tasks.

**Acceptance Criteria**
- [ ] While using the system, I am not suddenly logged out.
- [ ] If I come back to the system the next day, I am not logged out.
- [ ] If I come back to the system a week later, I am logged out.

**API**
- `POST /api/auth/refresh`
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

---

## 🥫 Pantry Management

### Story: View Pantry
**As a** logged-in user
**I want** to view all my pantry items
**so that** I can see what ingredients I currently have.

**Acceptance Criteria**
- [ ] When I open the pantry screen, I see a list of all my pantry items.
- [ ] Each item shows name, quantity, unit, and category.

**API**
- `GET /api/pantryItem?skip=1&take=1`

**Response (200)**
```json
{
  "totalCount": 0,
  "items": [
    {
      "id": 0,
      "food": {
        "id": 0,
        "name": "string",
        "category": {
          "id": 0,
          "name": "string"
        }
      },
      "quantity": 0,
      "unit": "string"
    }
  ]
}
```

---

### Story: Add Pantry Item
**As a** logged-in user
**I want** to add items to my pantry
**so that** I can track ingredients I have on hand.

**Acceptance Criteria**
- [ ] When I submit a new item, it is added to my pantry list.
- [ ] The item must have at least a name and quantity.

**API Single**
- `POST /pantryItem`

**Request**
```json
{
  "id": 0,
  "food": {
    "id": 0
  },
  "quantity": 0,
  "unit": "string"
}
```

**Response (201)**
```json
{
  "id": 0,
  "food": {
    "id": 0,
    "name": "string",
    "category": {
      "id": 0,
      "name": "string"
    }
  },
  "quantity": 0,
  "unit": "string"
}
```

**API Bulk**
- `POST /pantryItem/bulk`

**Request**
```json
[
  {
    "id": 0,
    "food": {
      "id": 0
    },
    "quantity": 0,
    "unit": "string"
  }
]
```

**Response (201)**
```json
[
  {
    "id": 0,
    "food": {
      "id": 0,
      "name": "string",
      "category": {
        "id": 0,
        "name": "string"
      }
    },
    "quantity": 0,
    "unit": "string"
  }
]
```

---

**As a** user,
**I want** to select or add ingredients when creating a pantry item,
**so that** I can easily track the foods I have without worrying about duplicates or database structure.

**Acceptance Criteria**

- [ ]  When creating a new pantry item, the user can start typing the name of an ingredient.
- [ ]  The system should search the database for matching ingredients and suggest them in real-time (autocomplete).
- [ ]  If the ingredient exists, selecting it will populate the pantry item with the corresponding `IngredientId`.
- [ ]  If the ingredient does not exist:
   - The user can enter the new ingredient name.
   - The system will create the new ingredient in the database before creating the pantry item.
- [ ]  From the user’s perspective, they do not need to distinguish between ingredients and pantry items; they only interact with ingredient names.
- [ ]  On the backend, all ingredients are stored in a single table for searchability and consistency.

**API**
- `GET /api/food?query={search}&skip={skip}&take={take}`

**Response (200)**
```json
{
  "totalCount": 0,
  "items": [
    {
      "id": 0,
      "name": "string",
      "category": {
        "id": 0,
        "name": "string"
      }
    }
  ]
}
```

---

### Story: Edit Pantry Item
**As a** logged-in user
**I want** to edit a pantry item
**So that** I can adjust quantities or fix mistakes.

**Acceptance Criteria**
- [ ] When I edit an item and save, the pantry list updates with new values.
- [ ] If I try to save invalid data (e.g., negative quantity), I get an error.

**API**
- `PUT /api/pantryItem/{id}`

**Request**
```json
{
  "id": 0,
  "food": {
    "id": 0
  },
  "quantity": 0,
  "unit": "string"
}
```

**Response (200)**
```json
{
  "id": 0,
  "food": {
    "id": 0,
    "name": "string",
    "category": {
      "id": 0,
      "name": "string"
    }
  },
  "quantity": 0,
  "unit": "string"
}
```

---

### Story: Remove Pantry Item
**As a** logged-in user
**I want** to remove items from my pantry
**so that** I can keep my pantry up to date.

**Acceptance Criteria**
- [ ] When I delete an item, it no longer appears in my pantry list.
- [ ] If the item doesn’t exist, I get an error.

**API Single**
- `DELETE /api/pantryItem/{id}`

**Response (204)**
_No content_

---

**API Bulk**
- `DELETE /api/pantryItem/bulk`

**Request**
```json
{
  "ids": [
    0
  ]
}
```

**Response (204)**
```json
{
  "ids": [
    0
  ]
}
```

---

### Story: Pantry Item from Packaged Food (e.g., Box of Crackers)

**As a** user
**I want** to take a picture of a packaged food item, such as a box of crackers
**So that** the system can read the packaging, identify it as "crackers," and automatically create a pantry item entry for me

**Acceptance Criteria:**
- [ ] Given I take a photo of a packaged food item
- [ ] When the system processes the image
- [ ] Then it recognizes the product type (e.g., crackers)
- [ ] And it generates a pantry item with the appropriate name and details

**Extended Acceptance Criteria:**
- [ ] The system should attempt to extract additional details from the packaging (e.g., brand, weight, flavor).
- [ ] The system should auto-populate the pantry item.
- [ ] The system should prompt the user to confirm or correct the item.
- [ ] The pantry item should include default quantity (e.g., 1 box) unless more detail is detected.

---

### User Story: Pantry Item from Fresh Food (e.g., Banana)

**As a** user
**I want** to take a picture of a fresh food item, such as a banana
**So that** the system can recognize it as "banana" and automatically create a pantry item entry for me

**Acceptance Criteria:**
- [ ] The user can upload a photo for detection.
- [ ] The system should attempt to detect the type of food in the image.
- [ ] The system should auto-populate the pantry item.
- [ ] The system should prompt the user to confirm or correct the item.
- [ ] The pantry item should include default quantity (e.g., 1 box) unless more detail is detected.

---

### User Story: Search Pantry Items by Name

**As a** user
**I want** to search for pantry items by entering their name
**So that** I can quickly find a specific item in my pantry list without scrolling

**Acceptance Criteria:**
- [ ] Given I am on the pantry screen
- [ ] When I type a name (full or partial) into the search bar
- [ ] Then the pantry list updates to show only matching items
- [ ] And the search is case-insensitive (e.g., "Crackers" matches "crackers")
- [ ] The system should support partial matches (e.g., typing "crack" returns "crackers").
- [ ] The system should update search results in real-time as the user types.
- [ ] If no items match the search, the system should display a "No results found" message.
- [ ] The user should be able to clear the search input to return to the full list.
- [ ] The system should handle special characters and spacing gracefully (e.g., "mac & cheese" vs "mac and cheese").

**API**
- `GET /api/pantryItem/search?query={search}&skip={skip}&take={take}`

**Response (200)**
```json
{
  "totalCount": 0,
  "items": [
    {
      "id": 0,
      "food": {
        "id": 0,
        "name": "string",
        "category": {
          "id": 0,
          "name": "string"
        }
      },
      "quantity": 0,
      "unit": "string"
    }
  ]
}
```

---

## 📅 Meal Plans

### Story: View Saved Meal Plans
**As a** logged-in user
**I want** to view my saved meal plans
**so that** I can reuse or review them.

**Acceptance Criteria**
- [ ] When I open meal plans, I see a list of all my saved plans.
- [ ] Each plan shows its creation date meals included.

**API**
- `GET /api/meal-plan?skip={skip}&take={take}`

**Response (200)**
```json
[
  {
    "id": 10,
    "name": "Weekly Plan for Aug 12",
    "updatedAt": "2025-08-12",
    "meals": [
      {
        "note": "",
        "meal": {
          "id": 0,
          "source": "string",
          "title": "string",
          "instructions": "string",
          "ingredients": [
            {
              "id": 0,
              "food": {
                "id": 0
              },
              "quantity": 0,
              "unit": "string"
            }
          ]
        }
      },
      {
        "note": "",
        "meal": {
          "id": 0,
          "source": "string",
          "title": "string",
          "instructions": "string",
          "ingredients": [
            {
              "id": 0,
              "food": {
                "id": 0
              },
              "quantity": 0,
              "unit": "string"
            }
          ]
        }
      }
    ]
  }
]
```

---

### Story: Add Meal Plan

**As a** user
**I want** to add meal plans
**So that** I can create my own based on what I feel like eating

**Acceptance criteria**
- [ ] When I go to the meal plan screen, I can create a new one with no recipes
- [ ] I can search for recipes and add them to a chosen meal plan

**API**
- `POST /api/meal-plan`

**Request**
```json
{
  "start-date": "1-3-25",
  "meals": [ 
    {
      "note": "",
      "meal": { "id": 1 }
    },
    {
      "note": "",
      "meal": { "id": 2 }
    }
  ]
}
```

**Response 200**
```json
{
  "id": 10,
  "start-date": "1-3-25",
  "meals": [ 
    {
      "note": "",
      "meal": { "id": 1 }
    },
    {
      "note": "",
      "meal": { "id": 2 }
    }
  ]
}
```

---

### Story: Update Meal Plan

**As a** user
**I want** to customize my meal plans
**So that** I can best accommodate my needs

**Acceptance criteria**
- [ ] When I choose a meal plan, I can select recipes
- [ ] I can delete those selected recipes
- [ ] I can search for recipes and add them to a chosen meal plan

**API**
- `PUT /api/meal-plan/{id}`

**Request**
```json
[
  {
    "id": 10,
    "start-date": "1-3-25",
    "meals": [ 
      {
        "note": "",
        "meal": { "id": 1 }
      },
      {
        "note": "",
        "meal": { "id": 2 }
      }
    ]
  }
]
```

**Response 200**
```json
[
  {
    "id": 10,
    "start-date": "1-3-25",
    "meals": [ 
      {
        "note": "",
        "meal": { "id": 1 }
      },
      {
        "note": "",
        "meal": { "id": 2 }
      }
    ]
  }
]
```

---

### Story: Delete Meal Plan

**As a** user
**I want** to delete my meal plans
**So that** I can keep my list of meal plans clean

**Acceptance criteria**
- [ ] When I choose a meal plan, I can select recipes
- [ ] I can delete those selected recipes
- [ ] I can search for recipes and add them to a chosen meal plan

**API**
- `DELETE /api/meal-plan/{id}`

**Response 204**
_No Content_

---

### Story: Generate Meal Plan from Pantry
**As a** logged-in user
**I want** to generate a meal plan based on my pantry items
**So that** I can make meals with ingredients I already have.

**Acceptance Criteria**
- [ ] When I request a generated plan, meals are suggested that use my pantry items.
- [ ] The plan is saved and can be viewed later.
- [ ] If no suitable meals are found, I see a message.

**API**
- `POST /meal-plan/generate`

**Request**
```json
{ "days": 7, "start-date": "1-7-25" }
```

**Response (201)**
```json
{
  "id": 11,
  "name": "Generated Plan",
  "meals": [
    { "day": "Monday", "note": "", "meal": { "id": 1 } },
    { "day": "Tuesday", "note": "", "meal": { "id": 2 } }
  ]
}
```

---

### Story: Customize Meal Plan

**As a** user,
**I want** to be able to write blobs of text instead of add recipes
**so that** I can remind myself what I'm making without needing a whole recipe.
- Maybe I'm just making spaghetti and I know I have the ingredients on hand, so I just need to write "spaghetti" as a remidner of what I'm doing Tuesday night.
- Maybe I just need to write in a reminder about a dinner out with friends, or that I'll be too busy to cook that night and need a reminder to pick something up.

**Acceptance Criteria**
- [ ] User can add recipes to certain days, or just add notes to certain days.
- [ ] User can later edit that text, or delete it and add a recipe.
- [ ] User can also have both text and a recipe.

---

## 🛒 Shopping List

### Story 11: View Shopping List
*As a logged-in user, I want to view the shopping list for a meal plan, so that I know what additional items to buy.*

**Acceptance Criteria**
- [ ] When I select a meal plan, I can see a generated shopping list.
- [ ] Items already in my pantry are marked as available (and not added to the list).
- [ ] The shopping list shows item names, quantities, and units.

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

### Story: View Recipes

**As a** user
**I want** to view my recipes
**so that** I can look through what I have saved

**Acceptance criteria**
- [ ] User can navigate to the recipes page
- [ ] User can view all their recipes

**API
- `GET /api/recipe?skip={skip}&take={take}`

**Response 200**
```json
[
  { "id": 101, "title": "Chicken Curry", "image": "url.jpg" }
]
```

---

### Story: Update Recipe

**As a** user
**I want** to be able to update my recipes
**so that** I can make changes as I see fit.

**Acceptance criteria**
- [ ] I can make changes to titles, ingredients, and instructions
- [ ] Changes are saved to the DB and able to be retrieved.

**API**
- `PUT /api/recipe/{id}`

**Request**
```json
{
  "id": 0,
  "source": "string",
  "title": "string",
  "instructions": "string",
  "ingredients": [
    {
      "id": 0,
      "food": {
        "id": 0
      },
      "quantity": 0,
      "unit": "string"
    }
  ]
}
```

**Response 200**
```json
{
  "id": 0,
  "userId": 0,
  "title": "string",
  "source": "string",
  "instructions": "string",
  "ingredients": [
    {
      "id": 0,
      "food": {
        "id": 0,
        "name": "string",
        "category": {
          "id": 0,
          "name": "string"
        }
      },
      "quantity": 0,
      "unit": "string"
    }
  ]
}
```

---

### Story: Search Recipes by Keyword

**As a** user
**I want** to search for recipes by title and ingredient
**So that** I can find new meal ideas.

**Acceptance Criteria**
- [ ] When I search by a term (e.g., “chicken”), I see a list of recipes.
- [ ] Each recipe shows title and brief description.

**API**
- `GET /api/recipe/search?title={search}&ingredient={ingredient}&skip={skip}&take={take}`

**Response (200)**
```json
{
  "totalCount": 0,
  "items": [
    {
      "id": 0,
      "userId": 0,
      "title": "string",
      "source": "string",
      "instructions": "string",
      "ingredients": [
        {
          "id": 0,
          "food": {
            "id": 0,
            "name": "string",
            "category": {
              "id": 0,
              "name": "string"
            }
          },
          "quantity": 0,
          "unit": "string"
        }
      ]
    }
  ]
}
```

---

### Story: Search Recipes by Pantry Items
**As a** logged-in user
**I want** to search for recipes using my pantry items
**So that** I can cook meals with what I have.

**Acceptance Criteria**
- [ ] When I search by pantry ingredients, I see recipes that use those items.
- [ ] Recipes that use more of my pantry items are ranked higher.

---

### Story: Save Recipes
**As a** logged-in user
**I want** to save recipes I’ve found
**so that** I can access them later.

**Acceptance Criteria**
- [ ] When I save a recipe, it appears in my saved recipes list.
- [ ] I can view saved recipes at any time.
- [ ] I can remove recipes from my saved list.

**API**
- `POST /api/recipe`

**Request**
```json
{
  "source": "string",
  "title": "string",
  "instructions": "string",
  "ingredients": [
    {
      "id": 0,
      "food": {
        "id": 0
      },
      "quantity": 0,
      "unit": "string"
    }
  ]
}
```

---

### Story: Delete Recipes

**As a** user
**I want** to delete recipes I no longer want
**So that** I can keep my list of recipes clean

**Acceptance criteria**

- [ ] A user selects one or more recipes and deltes them
- [ ] Deleted recipes no longer exist within their account for meal plans or searches

**API**
- `DELETE /api/recipe/{id}`

**Response (204)**
_No content_

---

### User Story: Import Recipe from URL

**As a** user
**I want** to provide the app with a URL to a recipe
**So that** the app can extract the recipe details and let me verify them before saving to my collection

**Acceptance Criteria:**
- [ ] Given I paste a valid recipe URL into the app
- [ ] When the system processes the URL
- [ ] Then it extracts recipe details (e.g., title, ingredients, steps, cooking time)
- [ ] And it displays the extracted recipe client-side for my review
- [ ] And I must confirm or edit the recipe before saving it to my collection

**Extended Acceptance Criteria:**
- [ ] The system should validate that the URL is accessible before attempting extraction.
- [ ] If the recipe site is supported, the system should extract structured data (title, ingredients, instructions, nutrition info if available).
- [ ] If the recipe site is unsupported or parsing fails, the system should provide a clear error message.
- [ ] If the user cancels, the extracted recipe should be discarded without saving.

**API**
- `POST /api/recipe/extract`

**Request**
```json
{
  "source": "string"
}
```

**Response**
```json
{
  "title": "string",
  "ingredients": [
    {
      "quantity": "string",
      "unit": "string",
      "name": "string"
    }
  ],
  "instructions": "string"
}
```

---

### User Story: Import Recipe from Text

**As a** user  
**I want** to paste or type a recipe into the app as plain text  
**So that** the app can parse the text into structured recipe details and let me verify them before saving to my collection  

**Acceptance Criteria:**
- [ ] Given I paste or type a recipe into the app  
- [ ] When the system processes the text  
- [ ] Then it extracts recipe details (e.g., title, ingredients, steps, cooking time)  
- [ ] And it displays the structured recipe client-side for my review  
- [ ] And I must confirm or edit the recipe before saving it to my collection  

**Extended Acceptance Criteria:**
- [ ] The system should attempt to parse common recipe formats (e.g., ingredients list, numbered steps).  
- [ ] If parsing succeeds, structured fields (title, ingredients, instructions, etc.) are pre-filled.  
- [ ] If parsing fails or is incomplete, the system should allow me to manually adjust or add missing fields.  
- [ ] The user should be able to freely edit any extracted fields before saving.  
- [ ] The system should not save the recipe to the database until the user explicitly confirms.  
- [ ] If the user cancels, the extracted recipe should be discarded without saving.

---

## 🔧 Developer / Maintainer Stories

### User Story: Testing

**As a** developer
**I want** automated unit tests
**So that** I can quickly verify that changes don’t break existing functionality.

**Acceptance Criteria**
- [ ] Tests can be run with a single command (e.g. dotnet test).
- [ ] At least one unit test exists for each major feature.
- [ ] Tests run successfully in the CI/CD pipeline.

---

### User Story: Error Handling

**As a** maintainer
**I want** clear error messages and logs
**So that** I can diagnose problems efficiently when they occur.

**Acceptance Criteria**

- [ ] All exceptions are logged with a timestamp and stack trace.
- [ ] Logs are stored in a consistent location (e.g. /logs folder).
- [ ] User-facing errors do not expose sensitive information.

---

### User Story: Configuration

**As a** developer
**I want** environment-specific configuration files
**So that** I can run the application locally without affecting production data.

**Acceptance Criteria**

- [ ] Config files exist for at least two environments (e.g. Development, Production).
- [ ] Switching environments does not require code changes.
- [ ] Local development uses a separate database or data store from production.

---

### User Story: Code Quality

**As a** maintainer
**I want** consistent coding style and linting rules
**So that** the project stays readable over time.

**Acceptance Criteria**

- [ ] A linter or style checker is integrated into the project.
- [ ] Code fails CI if it violates defined style rules.
- [ ] Naming conventions and formatting are consistent across files.

---

### User Story: Monitoring / Stability

**As a** maintainer
**I want** to track performance metrics
**So that** I can ensure the system runs reliably under load.

**Acceptance Criteria**

- [ ] Key performance metrics (e.g. response time, error rate) are captured.
- [ ] Metrics can be viewed in a dashboard or report.
- [ ] System behavior under load has been tested and documented.

---

### User Story: Documentation

**As a** future developer
**I want** setup instructions documented
**so that** I can get the project running quickly without guesswork.

**Acceptance Criteria**

- [ ] README includes setup steps (dependencies, build, run instructions).
- [ ] Any required environment variables are documented.
- [ ] A new developer can clone the repo and run the project successfully by following the docs.