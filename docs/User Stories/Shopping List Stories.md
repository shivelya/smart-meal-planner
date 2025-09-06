# 🛒 Shopping List

## Story: View Shopping List
***As a** logged-in user
**I want** to view the shopping list for a meal plan
**so that** I know what additional items to buy.*

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I select a meal plan, I can see a generated shopping list | ⬜ | ⬜ |
| Items already in my pantry are marked as available (and not added to the list) | ⬜ | ⬜ |
| The shopping list shows item names, quantities, and units | ⬜ | ⬜ |

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