# Car Rental Platform

Short description
- Car Rental backend implemented with clean separation: Domain, Application, Infrastructure, Presentation and API layers plus a Worker service.
- Purpose: manage vehicles, bookings, billing, employees and admins with role-based authorization and permission claims.

Tech stack
- .NET 8, C# 13
- ASP.NET Core 8 (Web API)
- EF Core (migrations & design-time factory)
- Microsoft Identity (roles & claims)
- JWT bearer authentication
- MediatR, AutoMapper, FluentValidation
- Serilog, MailKit
- Project folders: `API`, `Application`, `Domain`, `Infrastructure`, `Presentation`, `Worker`

Architecture overview
- `Domain` — entities, value objects, metadata (roles, permissions).
- `Application` — business logic, services, MediatR handlers, validators.
- `Presentation` — common controllers/helpers for presentation concerns.
- `Infrastructure` — DbContext (`AppDbContext`), EF configurations, seeders, identity store.
- `API` — ASP.NET Core Web API project (startup/Program).
- `Worker` — background worker project (uses `BackgroundService` patterns when needed).

Key implementation notes
- Identity: `User` inherits `IdentityUser<int>` and the project treats `Email` as the canonical username. Normalized username values used by Identity are based on the email.
- RBAC: Roles defined in `Domain.AppMetaData.Roles` (`Admin`, `Employee`, `Customer`). Permissions are implemented as role claims of type `"permission"` (see `Domain.AppMetaData.Permissions`).
- Seeders: `Infrastructure.Seeder.Role` and `Infrastructure.Seeder.User` exist to seed roles and example users (Admin, Employee, Customer).

Prerequisites
- Install .NET 8 SDK
- SQL Server (or update provider in `DesignTimeDbContextFactory` / DI to your DB)
- Set connection string named `DefaultConnection` in `appsettings.json` or environment variable `DEFAULT_CONNECTION`

Configuration
- Connection string: `ConnectionStrings:DefaultConnection` in `appsettings.json` (or env var `DEFAULT_CONNECTION`).
- Email settings are configured via `EmailSettings` and the `emailSettings` section in configuration.
- JWT settings are expected to be configured in API startup (check `Application.DependencyInjection` wiring for auth).

Database: migrations and design-time
- A design-time factory (`Infrastructure.DesignTimeDbContextFactory`) is available so EF tools can create `AppDbContext` when DI is not active.
- Typical migration workflow:
  - Create a migration:
    - __dotnet ef migrations add <Name> --project Infrastructure --startup-project API__
  - Apply migrations:
    - __dotnet ef database update --project Infrastructure --startup-project API__
  - (You can also run the CLI from the solution root and pass `--project`/`--startup-project` as needed.)

Seeding roles & users
- Role seeding: `Infrastructure.Seeder.Role.SeedAsync(RoleManager<Role>)` — seeds `Admin`, `Employee`, `Customer` and attaches permission claims.
- User seeding: `Infrastructure.Seeder.User.SeedAsync(UserManager<User>)` — seeds example admin, employee and customer accounts with default passwords.
- To run seeders at startup, call them from `Program.cs` after building the service provider, e.g.:
  - Use a scoped block to resolve `RoleManager<Role>` and `UserManager<User>` and call the async `SeedAsync` methods (ensure you `Wait()` or `GetAwaiter().GetResult()` in Program if using top-level sync calls).

Running the solution
- From the solution root:
  - Run the API: __dotnet run --project API__
  - Run the Worker: __dotnet run --project Worker__
- For development use Swagger (configured in `Application.DependencyInjection`) to explore endpoints and test auth flows.

Authorization & Permissions
- Use ASP.NET Core authorization policies that validate the claim type `permission` (see `Domain.AppMetaData.Permissions`) to gate actions.
- Role hierarchy (as implemented via claims):
  - Admin: all Employee permissions + manage employees, vehicles (create/update/delete), prices/offers, system settings, analytics, audit logs.
  - Employee: rental/return operations, vehicle inspection logging, invoicing and payment processing.
  - Customer: browse cars, create/manage own bookings, view own invoices/payments, update profile.

Useful files to inspect
- `Infrastructure\AppDbContext.cs` — EF/Identity configuration
- `Infrastructure\DesignTimeDbContextFactory.cs` — EF Core design-time factory
- `Domain\AppMetaData\Roles.cs` and `Domain\AppMetaData\Permissions.cs` — role and permission constants
- `Infrastructure\Seeder\Role.cs` and `Infrastructure\Seeder\User.cs` — seeding logic
- `Application\DependencyInjection.cs` — DI wiring, swagger and auth setup
- `API` project — startup / Program entry

Tips & troubleshooting
- If you see: "Unable to create a 'DbContext' of type ... Unable to resolve service for type 'Microsoft.EntityFrameworkCore.DbContextOptions'..." ensure:
  - `AppDbContext` constructor uses `DbContextOptions<AppDbContext>` and you're using a design-time factory or registering the `DbContext` in `Program.cs` with `services.AddDbContext<AppDbContext>(...)`.
- For migrations targeting the correct startup project, always pass `--project` and `--startup-project` when running __dotnet ef__.

Contributing
- Follow the layered architecture (Domain -> Application -> Infrastructure -> API).
- Register new services in `Application.DependencyInjection`.
- When adding new permissions, add constants to `Domain.AppMetaData.Permissions` and include them in role claims via the seeder or an admin UI.

License
- (Add your license information here)

If you want, I can:
- Add an example `Program.cs` snippet that runs the seeders during startup.
- Add authorization policy registration examples that check the `"permission"` claim.