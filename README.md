# Bookify - Hotel Reservation System

A production-ready hotel reservation system built with ASP.NET Core MVC following N-Tier architecture, Repository Pattern, Unit of Work, and Dependency Injection.

## Architecture

- **Bookify.Web** - ASP.NET Core MVC presentation layer
- **Bookify.Services** - Business logic and service layer
- **Bookify.Data** - Data access layer with EF Core, Repositories, and UnitOfWork
- **Bookify.Core** - Shared DTOs, enums, view models, and extensions
- **Bookify.Tests** - Unit and integration tests

## Features

- ✅ User Authentication & Authorization (ASP.NET Identity with Customer/Admin roles)
- ✅ Room Catalog with filtering and search
- ✅ Reservation Cart (Session-based)
- ✅ Booking Management
- ✅ Stripe Payment Integration
- ✅ Admin Dashboard with CRUD operations
- ✅ Health Checks
- ✅ Serilog Structured Logging
- ✅ DataTables integration for admin lists

## Prerequisites

- .NET 8.0 SDK
- SQL Server 2019+ (or LocalDB)
- Stripe account (for payment processing)

## Setup Instructions

### 1. Database Configuration

Update the connection string in `Bookify.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Bookify;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 2. Stripe Configuration

1. Create a Stripe account at https://stripe.com
2. Get your API keys from the Stripe Dashboard
3. Update `Bookify.Web/appsettings.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_secret_key_here",
    "PublishableKey": "pk_test_your_publishable_key_here",
    "WebhookSecret": "whsec_your_webhook_secret_here"
  }
}
```

**Important:** For production, use User Secrets (development) or Azure Key Vault (production) to store sensitive keys.

### 3. Run Database Migrations

```bash
cd Bookify.Web
dotnet ef database update --project ../Bookify.Data
```

### 4. Run the Application

```bash
dotnet run --project Bookify.Web
```

The application will:
- Create the database if it doesn't exist
- Run migrations automatically
- Seed initial data (Admin user, Room Types, Rooms)

### 5. Default Admin Credentials

- **Email:** admin@bookify.com
- **Password:** Admin@123

**Important:** Change the default admin password after first login!

## Project Structure

```
Bookify/
├── Bookify.Core/          # Shared DTOs, Enums, ViewModels
├── Bookify.Data/          # Entities, DbContext, Repositories, UnitOfWork
├── Bookify.Services/      # Business logic services
├── Bookify.Web/           # MVC Controllers, Views, Configuration
│   ├── Areas/
│   │   └── Admin/         # Admin area controllers and views
│   ├── Controllers/       # Public controllers
│   └── Views/             # Razor views
└── Bookify.Tests/         # Unit and integration tests
```

## Key Routes

### Public Routes
- `/` - Home page
- `/rooms` - Room listing with filters
- `/rooms/details/{id}` - Room details
- `/cart` - Shopping cart
- `/booking/checkout` - Checkout page
- `/booking/history` - User booking history

### Admin Routes (Requires Admin role)
- `/admin` - Admin dashboard
- `/admin/roomtypes` - Manage room types
- `/admin/rooms` - Manage rooms
- `/admin/bookings` - Manage bookings

### Health & Diagnostics
- `/health` - Health check endpoint

## Technologies Used

- **ASP.NET Core 8.0** - Web framework
- **Entity Framework Core 8.0** - ORM
- **ASP.NET Identity** - Authentication & Authorization
- **Stripe.net** - Payment processing
- **Serilog** - Structured logging
- **Bootstrap 5** - UI framework
- **jQuery** - JavaScript library
- **DataTables** - Table enhancement
- **Toaster.js** - Notifications

## Development

### Adding a New Migration

```bash
cd Bookify.Web
dotnet ef migrations add MigrationName --project ../Bookify.Data
dotnet ef database update --project ../Bookify.Data
```

### Running Tests

```bash
dotnet test
```

## Security Notes

- All POST endpoints use anti-forgery tokens
- Stripe webhook endpoint validates signatures
- Sensitive keys stored in configuration (use secrets manager in production)
- Password complexity enforced via Identity options
- SQL injection prevented by using EF Core parameterized queries
- XSS protection via Razor HTML encoding

## Future Enhancements

- Multi-room inventory management
- Dynamic pricing
- Promotions and vouchers
- Multi-hotel/property support
- Channel manager integration
- Mobile PWA features

## License

This project is provided as-is for educational and development purposes.

## Support

For issues and questions, please refer to the project documentation or contact the development team.

