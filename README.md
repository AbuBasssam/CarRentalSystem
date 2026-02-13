# 🚗 Car Rental System

A comprehensive car rental management system built with .NET and Clean Architecture principles, providing a robust and scalable solution for car rental businesses.

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Technologies](#technologies)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Background Services](#background-services)
- [API Documentation](#api-documentation)
- [Security](#security)
- [Contributing](#contributing)
- [License](#license)

## 🎯 Overview

The Car Rental System is an enterprise-grade application designed to manage car rental operations efficiently. It implements Clean Architecture principles, ensuring maintainability, testability, and separation of concerns.

## 🏗️ Architecture

This project follows **Clean Architecture** with clear separation between layers:

- **Domain Layer**: Contains business entities, enums, and exceptions
- **Application Layer**: Business logic, DTOs, commands, queries, and validation
- **Infrastructure Layer**: Data access, external services, and implementations
- **Presentation Layer**: API controllers and authorization handlers
- **Worker Layer**: Background services for automated maintenance tasks

### Architecture Patterns Used

- **CQRS (Command Query Responsibility Segregation)**: Separate read and write operations
- **Repository Pattern**: Abstract data access logic
- **Unit of Work Pattern**: Manage database transactions
- **Mediator Pattern**: Handle commands and queries
- **Provider Pattern**: Flexible policy and permission management
- **Background Services**: Automated cleanup and maintenance tasks

## ✨ Features

### Authentication & Authorization
- JWT-based authentication with refresh token mechanism
- Email confirmation system with OTP verification
- Role-based access control (RBAC)
- Permission-based authorization
- Policy-based authorization for complex scenarios
- Rate limiting for security (global and sensitive endpoints)
- Password reset with OTP verification
- Session token management with automatic cleanup

### User Management
- User registration with email verification
- Role and permission management
- User profile management
- Unverified users automatic cleanup

### Core Functionality
- Car inventory management
- Rental operations (OTP-based)
- Pagination support with localized queries
- Multi-language support (Arabic & English)
- Email notifications with customizable templates

### Background Services
- **Auth Token Cleanup**: Automatically removes expired authentication tokens
- **OTP Cleanup**: Removes expired verification codes
- **Password Reset Token Cleanup**: Manages password reset token lifecycle
- **Unverified Users Cleanup**: Removes inactive unverified accounts

### Security Features
- Password policies enforcement
- Email verification requirements
- Permission-based authorization handlers
- Global rate limiting middleware
- Sensitive data rate limiting
- Token rotation on refresh
- Automatic session cleanup
  
## 🛠️ Technologies

### Backend
- **.NET 8** - Core framework
- **Entity Framework Core** - ORM with SQL Server
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping

### Security
- **JWT (JSON Web Tokens)** - Authentication
- **ASP.NET Core Identity** - User management
- **Custom Authorization Handlers** - Permission management
- **BCrypt.Net** - Password hashing

### Background Processing
- **Hosted Services** - Background task execution
- **Serilog** - Structured logging

### Database
- **SQL Server** - Primary database
- **Entity Framework Migrations** - Database versioning

### Additional Tools
- **Worker Services** - Automated maintenance tasks
- **Localization** - Multi-language support
- **Resource Files** - Shared resources management

## 📁 Project Structure
```text
CarRentalSystem/
├── CarRentalSystem.sln
├── Directory.Build.props
├── Directory.Packages.props
├── .env.example
├── API/                              # Host project (Web API startup)
│   ├── Program.cs
│   ├── API.http
│   ├── appsettings.json
│   ├── appsettings.json.example
│   └── Properties/launchSettings.json
├── Application/                      # Use-cases, CQRS handlers, services, validations
│   ├── Abstracts/
│   ├── Behaviors/
│   ├── Features/
│   │   ├── Auth/
│   │   └── Home/
│   ├── Interfaces/
│   ├── Models/
│   ├── Resources/
│   ├── Services/
│   ├── Validations/
│   ├── DependencyInjection.cs
│   └── Application.csproj
├── Domain/                           # Core domain models and business constants
│   ├── AppMetaData/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── HelperClasses/
│   ├── Interfaces/
│   ├── Security/
│   └── Domain.csproj
├── Infrastructure/                   # EF Core context, repositories, migrations, seeders
│   ├── Context/
│   ├── EntitiesConfigurations/
│   ├── Implementations/
│   ├── Migrations/
│   ├── Seeder/
│   ├── DependencyInjection.cs
│   └── Infrastructure.csproj
├── Presentation/                     # API surface: controllers, middleware, authorization
│   ├── Authorization/
│   ├── Controller/
│   ├── Filters/
│   ├── Helpers/
│   ├── Middleware/
│   ├── Services/
│   ├── DependencyInjection.cs
│   └── Presentation.csproj
└── Worker/                           # Background worker host for cleanup jobs
    ├── BackgroundServices/
    ├── Configuration/
    ├── Program.cs
    ├── DependencyInjection.cs
    ├── appsettings.json
    └── Worker.csproj
```

## 🚀 Getting Started

### Prerequisites

- .NET SDK 8.0 or higher
- SQL Server 2019 or higher
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repository
```bash
git clone https://github.com/AbuBasssam/CarRentalSystem.git
cd CarRentalSystem
```

2. Update connection string in `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

3. Configure JWT settings in `appsettings.json`
```json
{
  "JwtSettings": {
    "SecretKey": "YOUR_SECRET_KEY_HERE",
    "Issuer": "CarRentalSystem",
    "Audience": "CarRentalUsers",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 5
  }
}
```

4. Configure email settings
```json
{
  "EmailSettings": {
    "FromEmail": "your-email@example.com",
    "Password": "your-app-password",
    "Host": "smtp.gmail.com",
    "Port": 587
  }
}
```

5. Apply database migrations
```bash
dotnet ef database update --project Infrastructure --startup-project API
```

6. Run the application
```bash
# Run API
dotnet run --project API

# Run Worker Services (in separate terminal)
dotnet run --project Worker
```

The API will be available at `https://localhost:5001` (or the port specified in launchSettings.json)

## 🔄 Background Services

The system includes automated background services for maintenance and cleanup tasks:

### 1. Auth Token Cleanup Service
**Purpose**: Automatically removes expired authentication tokens to maintain database performance.

**Configuration**:
```json
{
  "BackgroundServices": {
    "AuthTokenCleanup": {
      "Enabled": true,
      "IntervalHours": 24,
      "RetentionDaysAfterExpiry": 7,
      "BatchSize": 1000,
      "RunAt": "03:00"
    }
  }
}
```

**Features**:
- Runs daily at 3:00 AM (configurable)
- Keeps expired tokens for 7 days for audit purposes
- Batch processing for optimal performance
- Only deletes used or revoked tokens (respects unique index constraints)

### 2. OTP Cleanup Service
**Purpose**: Removes expired one-time passwords (OTP) used for email verification and password reset.

**Configuration**:
```json
{
  "BackgroundServices": {
    "OtpCleanup": {
      "Enabled": true,
      "IntervalMinutes": 15,
      "RetentionHoursAfterExpiry": 1,
      "MaxAgeHours": 24,
      "BatchSize": 500,
      "RunAt": null
    }
  }
}
```

**Features**:
- Runs every 15 minutes (OTP is short-lived)
- Keeps used/expired OTPs for 1 hour for audit
- Force deletes OTPs older than 24 hours
- Handles both ConfirmEmail (5 min) and ResetPassword (3 min) OTPs

### 3. Password Reset Token Cleanup Service
**Purpose**: Manages the lifecycle of password reset tokens.

**Configuration**:
```json
{
  "BackgroundServices": {
    "PasswordResetTokenCleanup": {
      "Enabled": true,
      "IntervalHours": 1,
      "RetentionHoursAfterExpiry": 2,
      "BatchSize": 500
    }
  }
}
```

**Features**:
- Runs hourly
- Keeps expired tokens for 2 hours
- Handles token rotation on usage

### 4. Unverified Users Cleanup Service
**Purpose**: Removes user accounts that haven't completed email verification within the specified timeframe.

**Configuration**:
```json
{
  "BackgroundServices": {
    "UnverifiedUserCleanup": {
      "Enabled": true,
      "IntervalHours": 6,
      "UnverifiedAccountMaxAgeHours": 24,
      "BatchSize": 100
    }
  }
}
```

**Features**:
- Runs every 6 hours
- Removes unverified accounts older than 24 hours
- Frees up reserved usernames and email addresses
- Cascading delete removes associated OTPs

### Monitoring Background Services

All background services use Serilog for structured logging:

```
[2026-01-21 03:00:00 INF] Auth Token Cleanup Service started
[2026-01-21 03:00:01 INF] Starting Auth Token cleanup operation
[2026-01-21 03:00:01 DBG] Batch 1: Found 150 expired tokens to delete
[2026-01-21 03:00:02 DBG] Batch 1: Successfully deleted 150 tokens
[2026-01-21 03:00:02 INF] Cleanup completed. Total deleted: 150 tokens in 1 batch(es). Duration: 00:01
```

Logs are stored in:
- Console output
- SQL Server `SystemLogs` table (via Serilog.Sinks.MSSqlServer)

## 🔒 Security

### Authentication
The system uses JWT-based authentication with the following features:
- Access tokens with 30-minute expiration
- Refresh tokens with 7-day validity
- Token rotation on refresh (one-time use refresh tokens)
- Email confirmation required for account activation
- Automatic token cleanup to prevent token proliferation

### Authorization
Multi-level authorization system:
- **Role-Based Access Control**: Predefined roles with specific permissions
- **Permission-Based Authorization**: Granular control over resources using custom policy provider
- **Policy-Based Authorization**: Custom policies for complex scenarios
  - `ValidTokenPolicy`: Validates token status
  - `ResetPasswordOnlyPolicy`: Restricts access during password reset flow
  - `VerificationOnlyPolicy`: Restricts access during email verification
  - `LogoutPolicy`: Handles logout scenarios

### Rate Limiting
Two-tier rate limiting system:
- **Global Rate Limiting**: Applied to all endpoints (configurable requests per minute)
- **Sensitive Rate Limiting**: Extra protection for authentication endpoints
  - SignIn/SignUp: Prevents brute force attacks
  - Password Reset: Prevents password reset abuse
  - Email Verification: Prevents OTP spam

### Password Policy
Configurable password requirements enforced by ASP.NET Core Identity:
- Minimum length: 8 characters
- Required character types:
  - Uppercase letters
  - Lowercase letters
  - Digits
  - Special characters

### OTP Security
- Time-limited codes (3-5 minutes)
- Maximum attempt tracking (5 attempts)
- Cooldown periods 
- Automatic expiration and cleanup
- BCrypt hashing for stored codes

### Token Security
- Unique index constraints prevent duplicate active tokens
- Token rotation prevents replay attacks
- Immediate revocation on logout
- Cascading cleanup of expired tokens
- Audit trail retention (7 days for auth tokens, 1 hour for OTPs)

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test Tests/Application.Tests
```

## 📊 Database Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName --project Infrastructure --startup-project API

# Update database
dotnet ef database update --project Infrastructure --startup-project API

# Remove last migration
dotnet ef migrations remove --project Infrastructure --startup-project API

# Generate SQL script
dotnet ef migrations script --project Infrastructure --startup-project API --output migration.sql
```

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes using conventional commits:
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation changes
   - `refactor:` Code refactoring
   - `test:` Adding tests
   - `chore:` Maintenance tasks
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Coding Standards

- Follow Clean Architecture principles
- Follow C# coding conventions and best practices
- Use meaningful variable and method names
- Document public APIs with XML comments
- Write unit tests for business logic
- Use async/await for I/O operations
- Implement proper error handling and logging
- Follow SOLID principles

### Code Review Checklist

- [ ] Code follows project architecture
- [ ] All tests pass
- [ ] No sensitive data in commits
- [ ] Documentation updated (if applicable)
- [ ] Proper error handling implemented
- [ ] Logging added for important operations
- [ ] Performance considerations addressed

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Abu Bassam**
- GitHub: [@AbuBasssam](https://github.com/AbuBasssam)

## 🙏 Acknowledgments

- Clean Architecture by Robert C. Martin
- ASP.NET Core Team for excellent documentation
- Community contributors and supporters

## 📞 Support

For support, email abubasssam@example.com or open an issue in the GitHub repository.

## 🗺️ Roadmap

- [ ] Implement car inventory management
- [ ] Add rental booking system
- [ ] Create admin dashboard
- [ ] Add payment integration
- [ ] Implement real-time notifications
- [ ] Add audit logging system
- [ ] Create comprehensive API documentation with Swagger
- [ ] Add performance monitoring
- [ ] Implement caching strategies

---

**Note**: This project is under active development. Features and documentation are subject to change.
