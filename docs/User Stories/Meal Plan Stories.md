# 📅 Meal Plans

## Story: View Saved Meal Plans
***As a*** logged-in user
***I want*** to view my saved meal plans
***so that*** I can reuse or review them.

**Acceptance Criteria**
| When I open meal plans, I see a list of all my saved plans | ⬜ | ⬜ |
| Each plan shows its creation date meals included | ⬜ | ⬜ |

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

## Story: Add Meal Plan

***As a*** user
***I want*** to add meal plans
***So that*** I can create my own based on what I feel like eating

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I go to the meal plan screen, I can create a new one with no recipes
| I can search for recipes and add them to a chosen meal plan

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

## Story: Update Meal Plan

***As a*** user
***I want*** to customize my meal plans
***So that*** I can best accommodate my needs

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I choose a meal plan, I can select recipes
| I can delete those selected recipes
| I can search for recipes and add them to a chosen meal plan

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

## Story: Delete Meal Plan

***As a*** user
***I want*** to delete my meal plans
***So that*** I can keep my list of meal plans clean

**Acceptance criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I choose a meal plan, I can select recipes
| I can delete those selected recipes
| I can search for recipes and add them to a chosen meal plan

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
| When I request a generated plan, meals are suggested that use my pantry items | ⬜ | ⬜ |
| The plan is saved and can be viewed later | ⬜ | ⬜ |
| If no suitable meals are found, I see a message | ⬜ | ⬜ |

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
| User can add recipes to certain days, or just add notes to certain days | ⬜ | ⬜ |
| User can later edit that text, or delete it and add a recipe | ⬜ | ⬜ |
| User can also have both text and a recipe | ⬜ | ⬜ |