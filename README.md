# 🚗 Car Rental System

A comprehensive car rental management system built with .NET and Clean Architecture principles, providing a robust and scalable solution for car rental businesses.

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Technologies](#technologies)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
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
- **Presentation Layer**: API controllers and worker services

### Architecture Patterns Used

- **CQRS (Command Query Responsibility Segregation)**: Separate read and write operations
- **Repository Pattern**: Abstract data access logic
- **Unit of Work Pattern**: Manage database transactions
- **Mediator Pattern**: Handle commands and queries
- **Provider Pattern**: Flexible policy and permission management

## ✨ Features

### Authentication & Authorization
- JWT-based authentication
- Email confirmation system
- Role-based access control (RBAC)
- Permission-based authorization
- Refresh token mechanism
- Rate limiting for security
- Reset password functionality

### User Management
- User registration and authentication
- Role and permission management
- User profile management
- Session token management

### Core Functionality
- Car inventory management
- Rental operations (OTP-based)
- Pagination support with localized queries
- Multi-language support (Arabic & English)
- Email notifications

### Security Features
- Password policies enforcement
- Email verification requirements
- Permission-based authorization handlers
- Global rate limiting middleware
- Sensitive data rate limiting

## 🛠️ Technologies

### Backend
- **.NET 8** - Core framework
- **Entity Framework Core** - ORM
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping

### Security
- **JWT (JSON Web Tokens)** - Authentication
- **ASP.NET Core Identity** - User management
- **Custom Authorization Handlers** - Permission management

### Database
- **SQL Server** - Primary database
- **Entity Framework Migrations** - Database versioning

### Additional Tools
- **Worker Services** - Background tasks
- **Localization** - Multi-language support
- **Resource Files** - Shared resources management

## 📁 Project Structure

```
CarRentalSystem/
├── API/                          # Web API Entry Point
│   ├── Connected Services/
│   ├── Dependencies/
│   ├── Properties/
│   ├── API.http
│   ├── appsettings.json
│   └── Program.cs
│
├── Application/                  # Application Layer (Business Logic)
│   ├── Dependencies/
│   ├── Abstracts/
│   │   └── LocalizePaginationQuery.cs
│   ├── Behaviors/
│   │   └── ValidationBehaviors.cs
│   ├── Features/
│   │   ├── AuthFeature/
│   │   │   ├── Commands/
│   │   │   │   ├── ConfirmEmail/
│   │   │   │   │   ├── ConfirmEmailCommand.cs
│   │   │   │   │   ├── ConfirmEmailDTO.cs
│   │   │   │   │   ├── ConfirmEmailHandler.cs
│   │   │   │   │   └── ConfirmEmailValidator.cs
│   │   │   │   ├── SignIn/
│   │   │   │   │   ├── SignInCommand.cs
│   │   │   │   │   ├── SignInCommandHandler.cs
│   │   │   │   │   └── SignInCommandValidator.cs
│   │   │   │   └── SignUp/
│   │   │   │       ├── SignUpCommand.cs
│   │   │   │       ├── SignUpCommandDTO.cs
│   │   │   │       ├── SignUpCommandHandler.cs
│   │   │   │       └── SignUpCommandValidator.cs
│   │   │   └── Queries/
│   │   ├── Home/
│   │   │   ├── Dtos/
│   │   │   └── Queries/
│   │   └── ...
│   ├── Validations/
│   │   ├── LocalizePaginationValidator.cs
│   │   └── ValidationRuleExtension.cs
│   └── Resources/
│       ├── SharedResources.cs
│       ├── SharedResources.AR.resx
│       └── SharedResources.EN.resx
│
├── Domain/                       # Domain Layer (Core Business Entities)
│   ├── AppMetaData/
│   │   ├── Permissions.cs
│   │   ├── Policies.cs
│   │   ├── Roles.cs
│   │   └── Router.cs
│   ├── Entities/
│   │   ├── Identity/
│   │   │   ├── Role.cs
│   │   │   ├── User.cs
│   │   │   ├── UserRole.cs
│   │   │   └── UserToken.cs
│   │   └── Otp.cs
│   ├── Enums/
│   │   ├── enOtpType.cs
│   │   └── enTokenType.cs
│   ├── Exceptions/
│   │   ├── BadRequestException.cs
│   │   └── DomainException.cs
│   └── HelperClasses/
│       ├── EmailSettings.cs
│       ├── JwtAuthResult.cs
│       ├── JwtSettings.cs
│       └── RateLimitEntry.cs
│
├── Infrastructure/               # Infrastructure Layer (Data Access & External Services)
│   ├── Dependencies/
│   ├── Context/
│   │   └── AppDbContext.cs
│   ├── EntitiesConfigurations/
│   │   └── Identity/
│   │       ├── RoleConfig.cs
│   │       ├── UserConfig.cs
│   │       ├── UserRoleConfig.cs
│   │       └── UserTokenConfig.cs
│   ├── Implementations/
│   │   └── Repositories/
│   │       ├── GenericRepository.cs
│   │       ├── OtpRepository.cs
│   │       ├── RefreshTokenRepository.cs
│   │       └── UnitOfWork.cs
│   ├── Migrations/
│   │   └── 20251230114856_Initial.cs
│   ├── Security/
│   │   ├── Claims/
│   │   │   └── SessionTokenClaims.cs
│   │   ├── Handlers/
│   │   │   ├── PermissionAuthorizationHandler.cs
│   │   │   ├── ResetPasswordOnlyHandler.cs
│   │   │   └── VerificationOnlyHandler.cs
│   │   ├── Models/
│   │   │   └── UserClaimModel.cs
│   │   ├── Providers/
│   │   │   └── PermissionPolicyProvider.cs
│   │   └── Requirements/
│   │       ├── PermissionRequirement.cs
│   │       ├── ResetPasswordOnlyRequirement.cs
│   │       └── VerificationOnlyRequirement.cs
│   └── Seeder/
│       ├── Role.cs
│       └── User.cs
│
├── Presentation/                 # Presentation Layer (API Controllers & Middleware)
│   ├── Dependencies/
│   ├── Controllers/
│   │   ├── ApiController.cs
│   │   └── AuthController.cs
│   ├── Extensions/
│   │   └── CommandExecutor.cs
│   ├── Helpers/
│   │   └── QueryExecutor.cs
│   ├── Middleware/
│   │   ├── ErrorHandlerMiddleware.cs
│   │   ├── GlobalRateLimitingMiddleware.cs
│   │   └── SensitiveRateLimitingMiddleware.cs
│   └── Services/
│       ├── HttpRequestContext.cs
│       └── ServiceLifetime.cs
│
└── Worker/                       # Background Services
    ├── Connected Services/
    ├── Dependencies/
    ├── Properties/
    ├── appsettings.json
    ├── CarRentalWorker.cs
    └── Program.cs
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
    "DefaultConnection": "Server=YOUR_SERVER;Database=CarRentalDB;Trusted_Connection=True;"
  }
}
```

3. Configure JWT settings in `appsettings.json`
```json
{
  "JWT": {
    "Key": "YOUR_SECRET_KEY",
    "Issuer": "CarRentalSystem",
    "Audience": "CarRentalUsers",
    "DurationInMinutes": 60
  }
}
```

4. Configure email settings
```json
{
  "EmailSettings": {
    "Email": "your-email@example.com",
    "Password": "your-password",
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
dotnet run --project API
```

The API will be available at `https://localhost:5001` (or the port specified in launchSettings.json)



## 🔒 Security

### Authentication
The system uses JWT-based authentication with the following features:
- Access tokens with configurable expiration
- Refresh tokens for token renewal
- Email confirmation required for account activation

### Authorization
Multi-level authorization system:
- **Role-Based Access Control**: Predefined roles with specific permissions
- **Permission-Based Authorization**: Granular control over resources
- **Policy-Based Authorization**: Custom policies for complex scenarios

### Rate Limiting
Two-tier rate limiting system:
- Global rate limiting for all endpoints
- Sensitive rate limiting for authentication endpoints

### Password Policy
Configurable password requirements:
- Minimum length
- Required character types (uppercase, lowercase, digits, special characters)
- Password history

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Coding Standards

- Follow Clean Architecture principles
- Follow C# coding conventions
- Document public APIs
- Use meaningful commit messages

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Abu Bassam**
- GitHub: [@AbuBasssam](https://github.com/AbuBasssam)

