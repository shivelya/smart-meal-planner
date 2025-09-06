# 🍴 Recipes

## Story: View Recipes

***As a*** user
***I want*** to view my recipes
***so that*** I can look through what I have saved

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| User can navigate to the recipes page |  | ⬜ |
| User can view all their recipes | ✅ | ⬜ |

**API
- `GET /api/recipe?skip={skip}&take={take}`

**Response 200**
```json
[
  { "id": 101, "title": "Chicken Curry", "image": "url.jpg" }
]
```

---

## Story: Update Recipe

***As a*** user
***I want*** to be able to update my recipes
***so that*** I can make changes as I see fit.

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| I can make changes to titles, ingredients, and instructions | ✅ | ⬜ |
| Changes are saved to the DB and able to be retrieved | ✅ | ⬜ |

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

## Story: Search Recipes by Keyword

***As a*** user
***I want*** to search for recipes by title and ingredient
***So that*** I can find new meal ideas.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I search by a term (e.g., “chicken”), I see a list of recipes | ✅ | ⬜ |
| Each recipe shows title and brief description | ✅ | ⬜ |

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

## Story: Search Recipes by Pantry Items

***As a*** logged-in user
***I want**** to search for recipes using my pantry items
***So that*** I can cook meals with what I have.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I search by pantry ingredients, I see recipes that use those items | ✅ | ⬜ |
| Recipes that use more of my pantry items are ranked higher | ⬜ | ⬜ |

---

## Story: Save Recipes
***As a*** logged-in user
***I want*** to save recipes I’ve found
***so that*** I can access them later.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I save a recipe, it appears in my saved recipes list | ✅ | ⬜ |
| I can view saved recipes at any time | ✅ | ⬜ |

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

## Story: Delete Recipes

***As a*** user
***I want*** to delete recipes I no longer want
***So that*** I can keep my list of recipes clean

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| A user selects one or more recipes and deletes them | ✅ | ⬜ |
| Deleted recipes no longer exist within their account for meal plans or searches | ✅ | ⬜ |

**API**
- `DELETE /api/recipe/{id}`

**Response (204)**
_No content_

---

## Story: Import Recipe from URL

***As a*** user
***I want*** to provide the app with a URL to a recipe
***So that*** the app can extract the recipe details and let me verify them before saving to my collection

**Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| I can paste a recipe URL into the app |  | ⬜ |
| The system extracts recipe details (e.g., title, ingredients, steps, cooking time) | ✅ | ⬜ |
| And it displays the extracted recipe client-side for my review | ✅ | ⬜ |
| And I must confirm or edit the recipe before saving it to my collection |  | ⬜ |

**Extended Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| The system should validate that the URL is accessible before attempting extraction | ✅ | ⬜ |
| The system should extract structured data (title, ingredients, instructions) | ✅ | ⬜ |
| If the recipe site is unsupported or parsing fails, the system should provide a clear error message | ✅ | ⬜ |
| If the user cancels, the extracted recipe should be discarded without saving | ✅ | ⬜ |

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

## Story: Import Recipe from Text

***As a*** user  
***I want*** to paste or type a recipe into the app as plain text  
***So that*** the app can parse the text into structured recipe details and let me verify them before saving to my collection  

**Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| Given I paste or type a recipe into the app | ⬜ | ⬜ |
| When the system processes the text | ⬜ | ⬜ |
| Then it extracts recipe details (e.g., title, ingredients, steps, cooking time) | ⬜ | ⬜ |
| And it displays the structured recipe client-side for my review | ⬜ | ⬜ |
| And I must confirm or edit the recipe before saving it to my collection | ⬜ | ⬜ |

**Extended Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| The system should attempt to parse common recipe formats (e.g., ingredients list, numbered steps) | ⬜ | ⬜ |
| If parsing succeeds, structured fields (title, ingredients, instructions, etc.) are pre-filled | ⬜ | ⬜ |
| If parsing fails or is incomplete, the system should allow me to manually adjust or add missing fields | ⬜ | ⬜ |
| The user should be able to freely edit any extracted fields before saving | ⬜ | ⬜ |
| The system should not save the recipe to the database until the user explicitly confirms | ⬜ | ⬜ |
| If the user cancels, the extracted recipe should be discarded without saving | ⬜ | ⬜ |