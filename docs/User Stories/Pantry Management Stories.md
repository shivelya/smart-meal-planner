# 🥫 Pantry Management

## Story: View Pantry
**As a** logged-in user
**I want** to view all my pantry items
**so that** I can see what ingredients I currently have.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I open the pantry screen, I see a list of all my pantry items | ✅ | ⬜ |
| Each item shows name, quantity, unit, and category | ✅ | ⬜ |

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

## Story: Add Pantry Item
**As a** logged-in user
**I want** to add items to my pantry
**so that** I can track ingredients I have on hand.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I submit a new item, it is added to my pantry list | ✅ | ⬜ |
| The item must have at least a name, category, and quantity | ✅ | ⬜ |

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

## Story: Reuse list of foods

**As a** user,
**I want** to select or add ingredients when creating a pantry item,
**so that** I can easily track the foods I have without worrying about duplicates or database structure.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|

|  When creating a new pantry item, the user can start typing the name of an ingredient, and system should search the database for matching ingredients and suggest them in real-time (autocomplete) | ✅ | ⬜ |
|  If the ingredient exists, selecting it will populate the pantry item with the corresponding `IngredientId` | ✅ | ⬜ |
|  If the ingredient does not exist:
   - The user can enter the new ingredient name and category | ✅ | ⬜ |
   - The system will create the new ingredient in the database before creating the pantry item | ✅ | ⬜ |
|  From the user’s perspective, they do not need to distinguish between ingredients and pantry items; they only interact with ingredient names | ✅ | ⬜ |
|  On the backend, all ingredients are stored in a single table for searchability and consistency | ✅ | ⬜ |

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

## Story: Edit Pantry Item
**As a** logged-in user
**I want** to edit a pantry item
**So that** I can adjust quantities or fix mistakes.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I edit an item and save, the pantry list updates with new values | ✅ | ⬜ |
| If I try to save invalid data (e.g., negative quantity), I get an error | ✅ | ⬜ |

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

## Story: Remove Pantry Item
**As a** logged-in user
**I want** to remove items from my pantry
**so that** I can keep my pantry up to date.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I delete an item, it no longer appears in my pantry list | ✅ | ⬜ |
| If the item doesn’t exist, I get an error | ✅ | ⬜ |

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

## Story: Pantry Item from Packaged Food (e.g., Box of Crackers)

**As a** user
**I want** to take a picture of a packaged food item, such as a box of crackers
**So that** the system can read the packaging, identify it as "crackers," and automatically create a pantry item entry for me

**Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| Given I take a photo of a packaged food item | ⬜ | ⬜ |
| When the system processes the image | ⬜ | ⬜ |
| Then it recognizes the product type (e.g., crackers) | ⬜ | ⬜ |
| And it generates a pantry item with the appropriate name and details | ⬜ | ⬜ |

**Extended Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| The system should attempt to extract additional details from the packaging (e.g., brand, weight, flavor) | ⬜ | ⬜ |
| The system should auto-populate the pantry item | ⬜ | ⬜ |
| The system should prompt the user to confirm or correct the item | ⬜ | ⬜ |
| The pantry item should include default quantity (e.g., 1 box) unless more detail is detected | ⬜ | ⬜ |

---

## Story: Pantry Item from Fresh Food (e.g., Banana)

**As a** user
**I want** to take a picture of a fresh food item, such as a banana
**So that** the system can recognize it as "banana" and automatically create a pantry item entry for me

**Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| The user can upload a photo for detection | ⬜ | ⬜ |
| The system should attempt to detect the type of food in the image | ⬜ | ⬜ |
| The system should auto-populate the pantry item | ⬜ | ⬜ |
| The system should prompt the user to confirm or correct the item | ⬜ | ⬜ |
| The pantry item should include default quantity (e.g., 1 box) unless more detail is detected | ⬜ | ⬜ |

---

## Story: Search Pantry Items by Name

**As a** user
**I want** to search for pantry items by entering their name
**So that** I can quickly find a specific item in my pantry list without scrolling

**Acceptance Criteria:**
| Task | Backend | Frontend |
|------|---------|----------|
| Given I am on the pantry screen, I can type a name (full of partial) into a search bar. | ✅ | ⬜ |
| The the pantry list updates to show only matching items. | ✅ | ⬜ |
| And the search is case-insensitive (e.g., "Crackers" matches "crackers"). | ✅ | ⬜ |
| The system should support partial matches (e.g., typing "crack" returns "crackers") | ✅ | ⬜ |
| The system should update search results in real-time as the user types |  | ⬜ |
| If no items match the search, the system should display a "No results found" message |  | ⬜ |
| The user should be able to clear the search input to return to the full list |  | ⬜ |
| The system should handle special characters and spacing gracefully (e.g., "mac & cheese" vs "mac and cheese") | ⬜ | ⬜ |

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