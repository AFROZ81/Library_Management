# Libris - Enterprise Library Management System

A modern, production-ready ASP.NET Core MVC library management system with role-based access control, comprehensive book management, and member-focused features.

## 🌟 Key Features

### **User Roles & Access Control**
- **Admin**: Full system access, user management, settings, audit logs
- **Librarian**: Book/loan management, member registration, reports
- **Member**: Personal dashboard, catalog browsing, reservations, profile management

### **Core Functionality**
- 📚 Complete book catalog management with images, genres, and ISBN
- 👥 Member registration and profile management
- 📖 Book loans with due dates, renewals, and fine calculation
- 🔄 Book reservation system with queue management
- 📊 Comprehensive reporting (circulation, financial, inventory, popular books)
- 🔍 Advanced search and book recommendations
- 📧 Email notifications for due dates, overdue notices, and reservation alerts
- 🛡️ Audit logging for all system activities
- 🔐 JWT authentication for API endpoints

### **Member-Specific Features**
- Personal dashboard with current loans and overdue alerts
- Borrowing history with status tracking
- Profile management with notification preferences
- Book reservations and queue position tracking
- Fine payment tracking

## 🚀 Quick Start

### **Prerequisites**
- .NET 10.0 SDK
- SQL Server (local or remote)
- Visual Studio 2022 or VS Code

### **Setup & Run**

1. **Clone and Navigate**
   ```bash
   cd "Libris.Web/Libris.Web"
   ```

2. **Configure Database**
   - Update `appsettings.json` with your SQL Server connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=LibrisDb;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

3. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

5. **Access the Application**
   - Navigate to `http://localhost:5002`
   - Default admin credentials will be created automatically

### **First-Time Setup**

The system automatically seeds initial data:
- **Admin User**: Admin account with full system access
- **Roles**: Admin, Librarian, Member roles created
- **Library Settings**: Default loan periods, fine rates configured

## 📁 Project Structure

```
Libris.Web/
├── Controllers/          # MVC controllers (Books, Members, Loans, etc.)
├── Models/              # Data models and ViewModels
├── Views/               # Razor views for each controller
├── Repositories/        # Data access layer
├── Services/            # Business logic (Email, Notifications, etc.)
├── Middleware/          # Custom middleware (Audit logging, API auth)
├── Data/               # DbContext and database configuration
└── wwwroot/           # Static files (CSS, JS, images)
```

## 🔧 Configuration

### **Environment-Specific Settings**
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings

### **Key Configuration Sections**
- **ConnectionStrings**: Database connection
- **EmailSettings**: SendGrid API for email notifications
- **ImageSettings**: Book image upload constraints
- **Jwt**: JWT token configuration for API

## 🏗️ Architecture

- **Pattern**: MVC with Repository Pattern
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity + JWT
- **Database**: SQL Server
- **Frontend**: Bootstrap 5 + jQuery
- **Email**: MailKit with SendGrid

## 📊 Database Schema

- **AspNetUsers**: User accounts and authentication
- **Members**: Library member profiles
- **Books**: Book catalog with metadata
- **BookLoans**: Loan transactions and history
- **BookReservations**: Book reservation queue
- **AuditLogs**: System activity tracking
- **LibrarySettings**: Configurable library parameters

## 🔐 Security Features

- Role-based authorization with policies
- Password complexity requirements
- Account lockout after failed attempts
- Audit logging for sensitive operations
- API key authentication for external access
- Secure cookie configuration

## 📧 Email Notifications

- **Due Date Reminders**: Automatic reminders before books are due
- **Overdue Notices**: Alerts when books become overdue
- **Reservation Alerts**: Notifications when reserved books become available
- Configurable per-member notification preferences

## 🐛 Troubleshooting

**Database Connection Issues**
- Ensure SQL Server is running
- Verify connection string format
- Check TrustServerCertificate setting

**Migration Errors**
- Run `dotnet ef migrations add` if schema changes were made
- Ensure database exists before running migrations

**Email Not Sending**
- Verify SendGrid API key in settings
- Check email configuration in appsettings

## 📝 License

This project is proprietary software. All rights reserved.

## 👥 Support

For issues or questions, please contact the development team.

---

**Built with ASP.NET Core 10.0 • Entity Framework Core • Bootstrap 5**
