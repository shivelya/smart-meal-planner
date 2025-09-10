# 📅 Meal Plans

## Story: View Saved Meal Plans
***As a*** logged-in user
***I want*** to view my saved meal plans
***so that*** I can reuse or review them.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I open meal plans, I see a list of all my saved plans | ✅ | ⬜ |
| Each plan shows its creation date & number of meals included | ✅ | ⬜ |

**API**
- `GET /api/meal-plan?skip={skip}&take={take}`

**Response (200)**
```json
{
  "totalCount": 0,
  "mealPlans": [
    {
      "id": 0,
      "startDate": "2025-09-08T16:38:09.592Z",
      "meals": [
        {
          "id": 0,
          "notes": "string",
          "recipeId": 0,
        }
      ]
    }
  ]
}
```

---

## Story: Add Meal Plan

***As a*** user
***I want*** to add meal plans
***So that*** I can create my own based on what I feel like eating

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I go to the meal plan screen, I can create a new one with no recipes |  | ⬜ |
| I can search for recipes and add them to a chosen meal plan | ✅ | ⬜ |

**API**
- `POST /api/meal-plan`

**Request**
```json
{
  "startDate": "2025-09-08T16:42:44.586Z",
  "meals": [
    {
      "notes": "string",
      "recipeId": 0
    }
  ]
}
```

**Response 200**
```json
{
  "id": 0,
  "startDate": "2025-09-08T16:42:44.588Z",
  "meals": [
    {
      "id": 0,
      "notes": "string",
      "recipeId": 0,
    }
  ]
}
```

---

## Story: Update Meal Plan

***As a*** user
***I want*** to customize my meal plans
***So that*** I can best accommodate my needs

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I choose a meal plan, I can select recipes | ✅ | ⬜ |
| I can delete those selected recipes | ✅ | ⬜ |
| I can search for recipes and add them to a chosen meal plan | ✅ | ⬜ |
| I can swap out recipes and add notes to updates | ✅ | ⬜ |

**API**
- `PUT /api/meal-plan/{id}`

**Request**
```json
{
  "id": 0,
  "startDate": "2025-09-08T16:45:07.928Z",
  "meals": [
    {
      "id": 0,
      "notes": "string",
      "recipeId": 0
    }
  ]
}
```

**Response 200**
```json
{
  "id": 0,
  "startDate": "2025-09-08T16:45:07.930Z",
  "meals": [
    {
      "id": 0,
      "notes": "string",
      "recipeId": 0,
    }
  ]
}
```

---

## Story: Delete Meal Plan

***As a*** user
***I want*** to delete my meal plans
***So that*** I can keep my list of meal plans clean

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When looking at meal plans, I can select ones delete | ✅ | ⬜ |

**API**
- `DELETE /api/meal-plan/{id}`

**Response 204**
_No Content_

---

## Story: Generate Meal Plan from Pantry
***As a*** logged-in user
***I want*** to generate a meal plan based on my pantry items
***So that*** I can make meals with ingredients I already have.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I request a generated plan, meals are suggested that use my pantry items | ✅ | ⬜ |
| The plan is saved and can be viewed later | ✅ | ⬜ |
| If no suitable meals are found, I see a message |  | ⬜ |

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

## Story: Customize Meal Plan

***As a*** user,
***I want*** to be able to write blobs of text instead of add recipes
***so that*** I can remind myself what I'm making without needing a whole recipe.
- Maybe I'm just making spaghetti and I know I have the ingredients on hand, so I just need to write "spaghetti" as a remidner of what I'm doing Tuesday night.
- Maybe I just need to write in a reminder about a dinner out with friends, or that I'll be too busy to cook that night and need a reminder to pick something up.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| User can add recipes to certain days, or just add notes to certain days | ✅ | ⬜ |
| User can later edit that text, or delete it and add a recipe | ✅ | ⬜ |
| User can also have both text and a recipe | ✅ | ⬜ |

---

## Story: Cooking a Meal Plan Recipe

***A a*** user,
***I want*** to be able to mark which meals I've made in a given meal plan.
***so that*** I can keep track.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| A meal can be marked as being cooked | ⬜ | ⬜ |

---

## Story: Using Pantry Items

***As a*** user,
***I want*** my pantry to stay updated when I cook meals
***so that*** my pantry is correct the next time I make a meal plan.

**Accpetance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| The system will generate a list of pantry items it believes were used once a given recipe is cooked | ⬜ | ⬜ |
| The list of used pantry items is displayed to the user for verification | ⬜ | ⬜ |
| The user can select which pantry items should be removed | ⬜ | ⬜ |

**API**
- `PUT /meal-plan/cook/{id}`

**Request**
```json
{ "meal-plan-entry-id": 1}
```

**Response (200)**
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

## Story: Using Pantry Items

***As a*** user,
***I want*** as many meals as I requested in my meal plan, even if I don't have enough of my own recipes.
***so that*** I have a full meal plan without having to save my own recipes.

**Accpetance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| The system will generate as many meals as possible out of the users saved recipes first. | ⬜ | ⬜ |
| If there are no more recipes that use the users pantry items, the system will go to an outside source such as Spoonacular. | ⬜ | ⬜ |
| The system can also use the outside source by default if the user requests it. | ⬜ | ⬜ |

---