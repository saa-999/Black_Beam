# BlackBeam

A .NET 8 Microservices-based backend system tailored for Point of Sale (POS) and Cashier Hub operations, featuring API Gateway routing, identity management, inventory, loyalty, and order processing with real-time tracking.

## Table of Contents

- [Background](#background)
- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Installation](#installation)
- [Configuration](#configuration)
- [Running Locally](#running-locally)
- [API Overview](#api-overview)
- [Security Notes](#security-notes)
- [Contributing](#contributing)

## Background

BlackBeam is designed to be the foundational backend for retail and point-of-sale systems. It separates concerns into distinct microservices—handling everything from user authentication to inventory tracking, loyalty points, and secure order processing via Stripe. It uses a centralized API gateway for routing and real-time SignalR communication for live cashier updates.

## Architecture Overview

The system is composed of an API Gateway and four distinct microservices:

*   **ApiGateway:** Powered by YARP (Yet Another Reverse Proxy), it routes traffic to the appropriate downstream microservices and enforces global rate-limiting.
*   **Identity Service:** Handles user authentication, registration, and JWT generation.
*   **Inventory Service:** Manages product stock and catalog.
*   **Loyalty Service:** Manages customer points and rewards.
*   **Orders Service:** Processes transactions, integrates with Stripe for payments, and provides real-time order tracking using SignalR (`/CashierHub`).

## Tech Stack

*   **Framework:** .NET 8.0 (C#)
*   **Gateway:** YARP
*   **Database:** MySQL (via Entity Framework Core `Pomelo.EntityFrameworkCore.MySql`)
*   **Authentication:** JWT (JSON Web Tokens)
*   **Real-time Communication:** SignalR
*   **Payments Integration:** Stripe
*   **Environment Configuration:** DotNetEnv

## Installation

### Requirements

*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   MySQL Server (Version 8.0+ recommended)
*   Git

### Clone the repository

```bash
git clone https://github.com/YOUR_ORGANIZATION/BlackBeam.git
cd BlackBeam
```

### Restore dependencies

```bash
dotnet restore Black_Beam.slnx
```

## Configuration

The project uses `.env` files to manage sensitive credentials like database connection strings and JWT secrets. 

**IMPORTANT:** Do NOT commit `.env` files to version control.

1.  Copy the `.env.example` file located in the root directory to create a new file named `.env`.
    ```bash
    cp .env.example .env
    ```
2.  Open the newly created `.env` file and populate it with your local development variables, including:
    *   MySQL connection credentials for each service.
    *   A strong, secure secret key for JWT (`JwtSettings__Secret`).
    *   Your Stripe API keys for the Orders service.

Each individual microservice loads environment variables automatically using `DotNetEnv`.

## Running Locally

To run the full suite locally, you'll need to start each project. You can do this via Visual Studio (Multiple Startup Projects), Rider, or the CLI.

Using the .NET CLI, open multiple terminal windows and run:

```bash
# Terminal 1 - API Gateway (Port 5000 / 5001 - check launchSettings.json)
cd BlackBeam.ApiGateway
dotnet run

# Terminal 2 - Identity Service (Port 5264)
cd BlackBeam.Services.Identity
dotnet run

# Terminal 3 - Inventory Service (Port 5275)
cd BlackBeam.Services.Inventory
dotnet run

# Terminal 4 - Loyalty Service (Port 5236)
cd BlackBeam.Services.Loyalty
dotnet run

# Terminal 5 - Orders Service (Port 5123)
cd BlackBeam.Services.Orders
dotnet run
```

*Note: You must apply EF Core database migrations for each service (Identity, Inventory, Loyalty, Orders) before they can operate correctly. (e.g., `dotnet ef database update`).*

## API Overview

All external requests should go through the API Gateway. The Gateway routes traffic based on the following path prefixes:

*   **`/api/identity/*`** -> Identity Service
*   **`/api/auth/*`** -> Identity Service
*   **`/api/orders/*`** -> Orders Service
*   **`/CashierHub/*`** -> Orders Service (SignalR)
*   **`/api/loyalty/*`** -> Loyalty Service
*   **`/CashierHub-Points/*`** -> Loyalty Service
*   **`/api/inventory/*`** -> Inventory Service

For specific endpoint details, you can inspect the `Endpoints/` folder within each microservice's directory.

## Security Notes

*   This project handles sensitive operations such as payments and user credentials. Ensure that JWT secrets and Stripe keys are properly secured and never hardcoded in source control.
*   A known limitation is that the project requires setting `RequireHttpsMetadata = true` in the JWT bearer configuration when deployed to production. Currently, it is set to `false` for development ease.
*   For instructions on reporting vulnerabilities safely, please review our [SECURITY.md](SECURITY.md) policy.

## Contributing

We welcome community contributions! Please review our [Contributing Guidelines](CONTRIBUTING.md) to understand how to propose changes, follow coding standards, and set up your local development environment.

## License

[MIT License](LICENSE)
