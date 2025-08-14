# Meal Planner App

A full-stack meal planning application built with **Angular** (frontend) and **ASP.NET Core** (backend). This project demonstrates a professional setup including CI/CD, automated documentation, testing, and deployment.

## Features
- Pantry management: add, edit, and remove ingredients  (planned)
- Generate meal plans based on pantry contents (planned)
- Recipe search and favorites (planned)
- Create shopping lists automatically (planned)
- Save favorite recipes and import/search recipes online (planned)

## Tech Stack
- **Frontend**: Angular, SCSS
- **Backend**: ASP.NET Core Web API
- **Database**: PostgreSQL
- **Recipe API**: TheMealDB
- **Testing**:
  - Frontend unit tests and e2e tests (Karma & Playwright)
  - Backend unit tests with XUnit (and Moq for mocking)
- **Documentation**:
  - Compodoc for the frontend
  - OpenAPI for the API docs
  - DocFX for the backend docs
- **Deployment & CI/CD**: GitHub Actions + Azure Web Apps + Static Web Apps

## Documentation
- [Frontend Docs](https://salmon-pond-0787b270f.1.azurestaticapps.net/frontend) (generated with Compodoc)
- [Backend Docs](https://salmon-pond-0787b270f.1.azurestaticapps.net/backend) (generated with DocFX)
- [API Docs](https://salmon-pond-0787b270f.1.azurestaticapps.net/api) (generated with OpenAPI)

## Project Structure
```console
/frontend # Angular app & unit and e2e tests
/backend # ASP.NET Core Web API & unit tests
/docs # temporary folder for merged docs (deployed automatically)
```

## CI/CD
- Automatic **build → test → deploy** workflows for both frontend and backend
- Automated deployment of generated documentation
- Ensures that changes are fully tested and documented before deployment

## Getting Started
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

3. Build and run the backend:
```bash
cd backend
dotnet restore
dotnet tool restore
dotnet build
dotnet run
```

4. Run tests:
```bash
# Frontend unit (Karma)
ng test
# Frontend e2e (Playwright)
npx playwright test
# Backend (xUnit)
dotnet test Backend.Tests
```

## Notes
- Documentation is automatically generated and deployed via CI/CD; no need to commit generated docs.
- Follows best practices for modern full-stack development, including clean repo structure, automated tests, and professional CI/CD pipelines.
