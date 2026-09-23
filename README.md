# EduTrack: Student and Course Management System

A full-stack capstone project: **ASP.NET Core 8 Web API** + **SQL Server** + **React (TypeScript, Vite)**.
Learners browse a course catalogue, enroll, and track status and grades. Admins manage courses, enrollments and people.

## What is included

| Area | Delivered |
|---|---|
| Auth | Register, login, logout (the token is really revoked server-side), `me`. JWT bearer, BCrypt password hashing, password rules enforced on the server. |
| Courses | Public catalogue with search by title/description, category and instructor. Admin create, edit, delete. Delete is blocked (409) while enrollment records exist. |
| Enrollment workflow | Enroll (confirm, then success screen), see all your enrollments, drop an active course. Re-enrolling after a drop reactivates the same row. Statuses: `Active`, `Completed`, `Dropped`. |
| Admin | Dashboard analytics (totals, status split, top courses, 6-month trend), enrollment management with grades, people management (students and instructors), CSV export of enrollments. |
| API quality | Layered architecture (controllers, services, repositories), DTOs + AutoMapper, FluentValidation, one error format for every failure, Swagger with a bearer button. |
| Tests | xUnit + Moq: services, validators, mapping, middleware, validation filter, controllers, repositories (EF InMemory) and the seeder. |
| Frontend | React Router, auth context, protected and admin-only routes, accessible forms and dialogs, responsive layout. |

## Project layout

```
EduTrack/
├── EduTrack.sln
├── docker-compose.yml            optional SQL Server container
├── backend/
│   ├── EduTrack.API/
│   │   ├── Controllers/          thin HTTP layer
│   │   ├── Services/             business rules (auth, courses, enrollments, users, admin, JWT)
│   │   ├── Repositories/         EF Core data access behind interfaces
│   │   ├── Models/  Data/        entities, DbContext, seeder
│   │   ├── DTOs/  Validators/  Mappings/
│   │   ├── Middleware/  Filters/  Exceptions/  Common/
│   │   ├── Program.cs  appsettings.json
│   └── EduTrack.Tests/           xUnit + Moq test project
└── frontend/                     React + TypeScript (Vite)
    └── src/ api/ components/ context/ pages/ utils/
```

## Getting started

**Prerequisites:** .NET 8 SDK, Node 18+ (Node 20+ recommended), and SQL Server (local instance, LocalDB or Docker).

### 1. Database

The default connection string in `backend/EduTrack.API/appsettings.json` targets a local SQL Server with Windows authentication:

```
Server=localhost;Database=EduTrackDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Pick whichever option fits your machine (environment variables override `appsettings.json`):

- **LocalDB (Windows):** `ConnectionStrings__DefaultConnection="Server=(localdb)\MSSQLLocalDB;Database=EduTrackDb;Trusted_Connection=True;"`
- **Docker (any OS):** `docker compose up -d`, then `ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=EduTrackDb;User Id=sa;Password=EduTrack_Passw0rd!;TrustServerCertificate=True"`

### 2. Run the API

```bash
cd backend/EduTrack.API
dotnet run
```

- Swagger UI: http://localhost:5080/swagger
- On first start the API creates the schema and seeds an admin plus demo data (see below).
- Set your own signing key outside of development: `Jwt__Key` (32+ characters), for example via `dotnet user-secrets`.

### 3. Run the UI

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173. The Vite dev server proxies `/api` to `http://localhost:5080`, so no CORS setup is needed.
If your API uses another port, start Vite with `VITE_API_PROXY_TARGET=http://localhost:<port> npm run dev`.

### Demo accounts (seeded on first run)

| Role | Email | Password |
|---|---|---|
| Admin | `admin@example.com` | `Admin@123` |
| Student | `student@example.com` | `Student@123` |
| Instructors | `instructor1@example.com`, `instructor2@example.com` | `Instructor@123` |

Set `Seed:IncludeDemoData` to `false` to seed only the admin. Change `Seed:AdminEmail` / `Seed:AdminPassword` in configuration to use your own admin credentials. **Change these before any real deployment.**

### EF Core migrations (recommended for a real repo)

If no migrations exist, the API calls `EnsureCreated()` so it runs out of the box. To use proper migrations instead:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database drop --project backend/EduTrack.API      # only if the database was already created by EnsureCreated
dotnet ef migrations add InitialCreate --project backend/EduTrack.API
dotnet ef database update --project backend/EduTrack.API
```

Once a `Migrations` folder exists the API applies pending migrations on startup (`Database:AutoMigrate`).

## Tests

```bash
dotnet test backend/EduTrack.Tests
dotnet test backend/EduTrack.Tests --collect:"XPlat Code Coverage"     # writes coverage.cobertura.xml under TestResults/
```

For an HTML report: `dotnet tool install -g dotnet-reportgenerator-globaltool`, then
`reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:Html`.

## API reference

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | public | Create a student account and sign in |
| POST | `/api/auth/login` | public | Sign in, returns `{ token, expiresAt, user }` |
| POST | `/api/auth/logout` | signed in | Revoke the current token |
| GET | `/api/auth/me` | signed in | Current user |
| GET | `/api/courses?search=&category=&instructor=` | public | Search the catalogue |
| GET | `/api/courses/categories` | public | Distinct categories |
| GET | `/api/courses/{id}` | public | Course details |
| POST / PUT / DELETE | `/api/courses[/{id}]` | admin | Create, update, delete |
| POST | `/api/enrollments` | signed in | Enroll `{ courseId }` |
| GET | `/api/enrollments/mine` | signed in | My enrollments |
| GET | `/api/enrollments?courseId=&studentId=&status=` | admin | All enrollments |
| PUT | `/api/enrollments/{id}/status` | signed in | Students: drop own active enrollment. Admins: any status, plus `grade` with `Completed` |
| GET | `/api/users?search=&role=student\|instructor\|admin` | admin | List people |
| PUT / DELETE | `/api/users/{id}` | admin | Edit name and instructor flag, delete |
| GET | `/api/admin/dashboard` | admin | Analytics |
| GET | `/api/admin/reports/enrollments.csv` | admin | CSV export |

### Error format

Every failure (validation, auth, not found, conflict, unexpected) has the same shape:

```json
{
  "timestamp": "2026-09-21T10:15:30.123+00:00",
  "path": "/api/enrollments",
  "error": "Conflict",
  "message": "You are already enrolled in this course."
}
```

Validation failures add an `errors` object (`{ "email": ["Enter a valid email address."] }`), which the UI shows next to the matching field. Unexpected exceptions return a generic 500 message; details are logged server-side only.

## Design decisions and assumptions

- **Roles.** The brief specifies an `IsAdmin` flag. I added `IsInstructor` on `User` so instructors are real records that can be assigned to courses (`Course.InstructorId`) without a separate table. Everyone who is neither admin nor instructor is a student. Self-registration can never create an admin or instructor.
- **One enrollment per student and course.** A unique index enforces it. Enrolling again after a drop reactivates the same row, clears the grade and resets the enrollment date.
- **Grades.** Only admins can set a grade, only together with `Completed`, from 0 to 100. Moving an enrollment to any other status clears the grade. Re-saving `Completed` without a grade keeps the existing one.
- **Students can only drop.** They can drop their own *active* enrollments. Every other status change needs an admin.
- **Deletes are safe by default.** Foreign keys use `Restrict`. Courses with enrollments, instructors with courses, students with enrollments and admin accounts return a clear 409/400 instead of cascading.
- **Logout.** JWTs are stateless, so revoked token ids (`jti`) are kept in an in-memory denylist until they expire. That is correct for a single API instance; use a shared cache such as Redis if you scale out.
- **Passwords.** BCrypt (work factor 11). Login returns the same message for an unknown email and a wrong password.
- **CSV export** escapes fields and neutralises spreadsheet formula injection (`=`, `+`, `-`, `@` prefixes).
- **Dashboard aggregation** runs in memory, which is fine at this scale; move it to SQL `GROUP BY` if data grows.

## Verification status

Please read this before you demo or submit:

- **Frontend:** type-checks and builds (`npm run build`), and I exercised it in a headless browser against a mock API that mirrors the API contract: catalogue filtering, empty states, login errors, enrollment confirm and success, drop, re-enroll view, admin guard, registration validation, all admin pages, mobile layout. It has **not** been run against the real .NET API.
- **Backend:** the environment I built this in has no .NET SDK and cannot reach NuGet, so the C# code (API and tests) was **never compiled or executed**. All 51 files pass a C# syntax parse and were reviewed by hand, but you should expect to run `dotnet build` and `dotnet test` yourself and fix any small compile or assertion issues that turn up. Treat the test suite and the coverage figure as unmeasured until you have run them.

## Troubleshooting

- **`Cannot open database` / login failed:** check the connection string and that SQL Server is running. Docker users must set the `ConnectionStrings__DefaultConnection` environment variable.
- **UI shows "Cannot reach the server":** the API is not running, or is on a different port than the Vite proxy target.
- **`Jwt:Key must be at least 32 characters long`:** set `Jwt__Key` to a longer value.
- **Migrations complain that tables already exist:** the database was created by `EnsureCreated()`. Drop it (`dotnet ef database drop`) before adding migrations.
