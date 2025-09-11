# 🛒 Shopping List

## Story: View Shopping List

***As a*** logged-in user
***I want*** to view the shopping list for a meal plan
***so that*** I know what additional items to buy.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When the user selects a meal plan, they can tell the system to generate a shopping list | ✅ | ⬜ |
| The items can be added to the existing shopping or it can be restarted. | ✅ | ⬜ |
| Items already in the pantry are marked as available (and not added to the list) | ✅ | ⬜ |
| The shopping list will be sorted by category, and then alphabetically. | ⬜ | ⬜ |

**Extended Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| Have a list of staples, and keep those listed separately on the shopping list because the user may well already have them. | ⬜ | ⬜ |
| The shopping list shows item names, quantities, and units | ⬜ | ⬜ |

**API**
- `GET /meal-plans/{id}/generate-shopping-list?new=true`

**Request**
- new - boolean value defining whether or not to clear out the shopping list before adding these items to it

**Response (200)**
```json
{
  "neededIngredients": [
    {
      "id": 0,
      "food": {
        "id": 0,
        "name": "banana",
        "categoryId": 1,
        "category": {
          "id": 1,
          "name": "produce"
        }
      },
      "quantity": 0,
      "unit": "string"
    }
  ],
  "availableIngredients": [
    {
      "id": 0,
      "food": {
        "id": 0,
        "name": "strawberry",
        "categoryId": 1,
        "category": {
          "id": 1,
          "name": "produce"
        }
      },
      "quantity": 0,
      "unit": "string"
    }
  ]
}
```

---

## Story: Customize Shopping List

***As a*** user
***I want*** customize the shopping list
***so that*** can get snacks or non-food items, etc.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| The user can view the shopping list | ✅ | ⬜ |
| The user can fully edit items on the shopping list | ✅ | ⬜ |
| The user can add new items to the shopping list | ✅ | ⬜ |
| The user can remove items from the shopping list. | ✅ | ⬜ |
| The user can mark items as completed. | ✅ | ⬜ |

**API**
- `GET /shopping-list/`

**Response (200)**
```json
{
  {
    "totalCount": 0,
    "foods": [
      {
        "id": 0,
        "foodId": 0,
        "food": {
          "id": 0,
          "name": "string",
          "category": {
            "id": 0,
            "name": "string"
          }
        },
        "purchased": true,
        "notes": "string"
      }
    ]
  }
}
```

---

**API**
- `DELETE /shopping-list/{id}`

**Response (204)**

---

**API**
- `PUT /shopping-list/{id}`

**Request**
```json
{
  "id": 0,
  "foodId": 0,
  "food": {
    "id": 0,
    "name": "string",
    "category": {
      "id": 0,
      "name": "string"
    }
  },
  "purchased": true,
  "notes": "string"
}
```

**Response (200)**
```json
{
  "id": 0,
  "foodId": 0,
  "food": {
    "id": 0,
    "name": "string",
    "category": {
      "id": 0,
      "name": "string"
    }
  },
  "purchased": true,
  "notes": "string"
}
```

---

**API**
- `POST /shopping-list`

**Request**
```json
{
  "foodId": 0,
  "food": {
    "id": 0,
    "name": "string",
    "category": {
      "id": 0,
      "name": "string"
    }
  },
  "purchased": true,
  "notes": "string"
}
```

**Response (200)**
```json
{
  "id": 0,
  "foodId": 0,
  "food": {
    "id": 0,
    "name": "string",
    "category": {
      "id": 0,
      "name": "string"
    }
  },
  "purchased": true,
  "notes": "string"
}
```