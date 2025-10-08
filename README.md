# Meal Planner App

A full-stack meal planning application built with **Angular** (frontend), **EF Core**, and **ASP.NET Core** (backend). This project demonstrates a professional setup including CI/CD, automated documentation, testing, and deployment.

## Current Progress
[User Stories/Acceptance Criteria](https://github.com/users/shivelya/projects/1)

## Features
- Pantry management: add, edit, and remove ingredients
- Generate meal plans based on pantry contents
- Recipe search and favorites
- Create shopping lists automatically
- Save favorite recipes and import recipes online
- Integrate with Spoonacular for recipe searches and polished ingredient data (planned)
- Sync with another user (like a live-in partner) to keep pantry and meal plans in sync (planned)
- Add items to the pantry via pictures of food item (planned)
- Have an understanding of various types of units for proper quantity comparisons (planned)

## Tech Stack
- **Frontend**: Angular, SCSS
- **Backend**: ASP.NET Core Web API
- **Database**: PostgreSQL
- **Recipe API**: Spoonacular
- **Testing**:
  - Frontend unit tests and e2e tests (Karma & Playwright)
  - Backend unit tests with (XUnit and Moq)
- **Documentation**:
  - Compodoc for the frontend
  - OpenAPI for the API docs
- **Deployment & CI/CD**: GitHub Actions + Azure Web Apps + Static Web Apps

## Live Artifacts

- [User Stories/Acceptance Criteria](https://github.com/users/shivelya/projects/1)
- [API Docs](https://smart-meal-planner-backend-ceazgjcaehfghdf7.canadacentral-01.azurewebsites.net/swagger/index.html) (generated with OpenAPI)
- [Backend Unit Test Code Coverage](https://shivelya.github.io/smart-meal-planner/coveragereport/)
- [(planned) Frontend Docs](https://shivelya.github.io/smart-meal-planner/frontend/) (generated with Compodoc) (planned)
- [(planned) Frontend Live Site](https://lemon-tree-0078d2d0f.1.azurestaticapps.net/)

## Project Structure
```console
/frontend # Angular app & unit and e2e tests
/backend # ASP.NET Core Web API & unit tests
/docs # Diagrams and User Stories
```

## CI/CD
- Automatic **build → test → deploy** workflows for both frontend and backend
- Automated deployment of generated documentation
- Ensures that changes are fully tested and documented before deployment

## Getting Started

### Prerequisites
 - .NET SDK
 - Node.js (and npm)
 - PostgreSQL

### Installation
1. Clone the repository:
```bash
git clone https://github.com/shivelya/smart-meal-planner.git
```

2. Install dependencies and build the frontend:
```bash
cd frontend
npm install
ng build
```

3. Install dependencies and build the backend:
```bash
cd backend
dotnet restore
dotnet tool restore
dotnet build
```

4. Configure required secrets
These must be set before running the project:
- `ConnectionStrings:DefaultConnection` → Your database connection string
- `Auth:JwtKey` → Key for JWT token signing. Must be at least 256 bits
- `Spoonacular:ApiKey` → API Key from Spoonacular. Available for free with sign up on their site
- `Email:SMTPPassword` → Password to your SMTP server of choice. The other configuration values are in appsettings.json.

```bash
# for VS code
cd backend\Backend
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "connect"
dotnet user-secrets set "Auth:JwtKey" "key"
dotnet user-secrets set "Spoonacular:ApiKey" "spoonacular api key"
dotnet user-secrets set "Email:SMTPPassword" "password"
```

6. Run migrations
```bash
cd backend
dotnet ef database update
```

6. Run the backend
```bash
cd backend
dotnet run
# I like dotnet run | ForEach-Object { $_ | jq . } to help keep my terminal messages readable
# requires jq install (winget install jqlang.jq on windows)
```

7. Run the frontend
```bash
cd frontend
ng start
```

### Run tests
```bash
# Backend unit (xUnit)
dotnet test Backend.Tests

# Backend integration (xUnit)
dotnet test Backend.IntegrationTests

# Frontend unit (Karma)
ng test

# Frontend e2e (Playwright)
npx playwright test

```

### API Endpoints
Fully defined here: [API Docs](https://smart-meal-planner-backend-ceazgjcaehfghdf7.canadacentral-01.azurewebsites.net/swagger/index.html) (generated with OpenAPI)
