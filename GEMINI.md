# GEMINI.md - UIS Backend (Server)

This document provides specialized context for the UIS (University Interface System) backend, an ASP.NET Core 10.0 application.

## 🚀 Project Overview
The UIS Backend is a hybrid application providing a **RESTful Web API** for the mobile frontend and an **MVC Admin Panel** for platform management. It handles user authentication, service cataloging, order processing, secure payments (escrow), and real-time communication.

- **Framework:** ASP.NET Core 10.0
- **Database:** PostgreSQL (via Entity Framework Core 10)
- **Real-time:** SignalR (for Chat and Notifications)
- **Auth:** JWT with direct Login (OTP verification removed).
- **Real email sending:** via MailKit using fps60y@gmail.com for Welcome and Service emails.
- **Infrastructure:** Docker & Docker Compose

## 📂 Architecture & Directory Structure
The project follows a standard ASP.NET Core MVC/Web API pattern:

- `/Controllers`:
    - `/Api`: Contains Web API controllers for mobile client interaction.
    - `AdminController.cs`: MVC controller managing the dashboard and administrative views.
- `/Services`: Contains business logic (Auth, Order, Payment, Chat, KYC, etc.).
- `/Models`: Entity Framework Core data models.
- `/DTOs`: Data Transfer Objects for API requests and responses.
- `/Data`:
    - `ApplicationDbContext.cs`: EF Core database context.
    - `DbSeeder.cs`: Seeds the database with roles, admin, students, executors, and sample orders.
- `/Hubs`: SignalR `ChatHub` for real-time messaging.
- `/Views`: Razor Views for the Admin Panel.
- `/Migrations`: EF Core database migration history.

## 🏃 Building and Running

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for PostgreSQL and pgAdmin)

### Setup Environment
1.  **Run Application locally:**
    ```powershell
    dotnet run
    ```
    *The server starts at `http://localhost:5035` by default. SQLite database will be created automatically.*

4.  **API Documentation (Swagger):**
    Access Swagger UI at `http://localhost:5035/swagger` to explore and test API endpoints.

## 🛠️ Key Commands
- **Add Migration:** `dotnet ef migrations add <MigrationName>`
- **Remove Migration:** `dotnet ef migrations remove`
- **Watch Mode:** `dotnet watch run`
- **Seed Data:** Handled automatically on startup via `DbSeeder.SeedAsync`.

## 🔑 Default Credentials
- **Admin Email:** `admin@uis.com`
- **Password:** `admin123`
- **Sample Students:** `student1@uis.com` ... `student20@uis.com` (Pass: `pass123`)
- **Sample Executors:** `executor1@uis.com` ... `executor15@uis.com` (Pass: `pass123`)

## 📐 Development Conventions
- **Dependency Injection:** All business logic must be encapsulated in services and registered in `Program.cs`.
- **API Responses:** Use DTOs to avoid leaking internal models.
- **Authentication:** Protect API endpoints with `[Authorize]`. Use `ClaimTypes.NameIdentifier` to retrieve the current user's ID.
- **Localization:** The Admin Panel and seed data are primarily in Arabic.
- **Real-time:** Use `IChatService` for persistence and `ChatHub` for delivery.

## 📋 Core Entities
- **User / Role:** Many-to-Many RBAC. Users have multiple roles (e.g., Student + Executor).
- **Boolean Flags:** `IsAdmin`, `IsExecutor`, `IsStaff` in the User model for efficient access control.
- **KycRequest:** Identity verification. Approving a request automatically grants the "Executor" role and sets `IsExecutor = true`.
- **Service / Category:** The marketplace catalog.
- **Order / Escrow:** Transactional lifecycle and fund protection.
- **Chat / Message:** Direct and order-linked communication.
- **Ticket:** Support and dispute resolution.

## 📈 Current State
- The ASP.NET Core backend is fully implemented and operational.
- The **MVC Admin Panel** includes comprehensive views for User/Role Management, Orders, Services, Categories, KYC, Payments, and Tickets.
- The **RESTful Web API** provides endpoints for Authentication, Orders, Services, Chats, and KYC submission.
- Real-time chatting and notifications are functional via SignalR (`ChatHub`).
- The database is successfully seeded with test entities, sample orders, payments, and KYC records via `DbSeeder`.

## 📊 System State Snapshot

### 🌐 Network & Access
- **Backend Port:** 5035
- **Public URL (Cloudflare):** `https://transmit-cambridge-plus-colon.trycloudflare.com`
- **Tunnel Process PID:** 22480

### 📧 SMTP Configuration
- **Server:** `smtp.gmail.com:587`
- **User:** `fps60y@gmail.com`
- **Status:** Dynamic management enabled via Admin Panel.

https://transmit-cambridge-plus-colon.trycloudflare.com`
- **Tunnel Process PID:** 22480

### 📧 SMTP Configuration
- **Server:** `smtp.gmail.com:587`
- **User:** `fps60y@gmail.com`
- **Status:** Dynamic management enabled via Admin Panel.

