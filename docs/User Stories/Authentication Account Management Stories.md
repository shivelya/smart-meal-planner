# 🔐 Authentication & Account Management User Stories

## Story: User Registration
***As a*** Non-registered user
***I want*** to create an account
***so that*** I can access the website.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| Given valid credentials, I create an account and am authenticated and receive a token/session | ✅ | ⬜ |
| Given invalid credentials, when I submit the login form, then I see an error message and an account is not created | ✅ | ⬜ |

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

## Story: User Login
***As a*** registered user
***I want*** to log in with my email and password
***so that*** I can access my information securely and others cannot.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| Given valid credentials, when I submit the login form, then I am authenticated and receive a token/session | ✅ | ⬜ |
| Given invalid credentials, when I submit the login form, then I see an error message and I am not logged in | ✅ | ⬜ |

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

## Story: User Logout
***As a*** logged-in user
***I want*** to log out
***so that*** my session is closed and my account is secure.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I log out, my session token is invalidated | ✅ | ⬜ |
| After logging out, I cannot access protected routes without logging in again | ✅ | ⬜ |

**API**
- `POST /api/auth/logout`

**Request**
```json
{ "refreshToken": "refresh-token" }
```

**Response (200)**

---

## Story: Change Password
***As a*** logged-in user
***I want*** to change my password
***so that*** I can keep my account secure.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I provide my old password and a new password, my password is updated | ✅ | ⬜ |
| If my old password is incorrect, I get an error | ✅ | ⬜ |

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

## Story: Reset Forgotten Password
***As a*** Registered user who forgot my password
***I want*** to reset it
****so that*** I can regain access to my account.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| When I request a reset, I receive a reset link via email | ✅ | ⬜ |
| When I submit a new password with a valid token, my password is updated | ✅ | ⬜ |
| If the token is expired or invalid, I get an error | ✅ | ⬜ |

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

## Story: Token Management

***As a*** registered user
***I want*** to not have to log in every time I visit the site
***so that*** my time isn't wasted on unndeeded tasks.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| While using the system, I am not suddenly logged out | ✅ | ⬜ |
| If I come back to the system the next day, I am not logged out | ✅ | ⬜ |
| If I come back to the system a week later, I am logged out | ✅ | ⬜ |

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

## Story: Synced User

***As a*** user with a live-in partner whom I meal plan with
***I want*** to keep our pantry and meal plans in sync
***so that*** both of us can know the status of food and meals at any time.

**Acceptance Criteria**
| Task | Backend | Frontend |
|------|---------|----------|
| I can send an invite via email to someone I'd like to be my partner. | ⬜ | ⬜ |
| That user can click the link on said email and accept the invitation. | ⬜ | ⬜ |
| That user will be prompted to make an acount if they don't already have one. | ⬜ | ⬜ |
| When adding new pantry items, recipes, meal plans, etc., they'll belong to the currently logged in user. | ⬜ | ⬜ |
| All pantry items, recipes, and meal plans will be visible to both users. | ⬜ | ⬜ |
| Either user can decide to no longer be partnered up, at which point they no longer have access to each others data. | ⬜ | ⬜ |

**API**
 - `POST /api/auth/request-sync`

 **Response (200)**

 **API**
 - `POST /api/auth/sync-account`

 **Response (200)**

 **API**
  - `GET /api/auth/unsync`

  **Response (200)**