Billing Payment System API

A fully functional billing management backend developed using ASP.NET Core 8, Entity Framework Core, JWT Authentication, Azure SQL, and Azure App Service.
This API supports three roles:

Admin (manages bills)

Mobile user (queries bills)

Banking/Payment service (processes payments)

The system includes JWT-based authentication, CSV batch bill import, request/response logging, and complete Swagger documentation.

🚀 Features
🔐 Authentication (JWT)

Separate login flows for Admin, Mobile, and Banking clients

Secure token generation with configurable expiration

📄 Admin Capabilities

Add bill for a subscriber

Upload bills in bulk using CSV

View detailed error reports for invalid rows

Azure SQL database integration

📱 Mobile User Capabilities

Login to receive token

Query latest bill

Check bill payment status

🏦 Banking API Capabilities

Mocked payment endpoint

Marks bill as paid

Simulates external bank transaction

🛠 Infrastructure

Swagger documentation and UI

EF Core migrations

Azure SQL database

Azure App Service deployment

Custom request/response logging middleware

CORS enabled for all origins

🧱 Technologies Used
Component	Technology
Backend Framework	ASP.NET Core 8
Authentication	JWT Bearer Tokens
Database	Azure SQL + Entity Framework Core
Hosting	Azure App Service
Documentation	Swagger / OpenAPI
Logging	Custom middleware
Deployment	GitHub, CLI, Visual Studio
📦 Project Structure
Billing.Api/
 ├── Controllers/
 │    ├── AuthController.cs
 │    ├── AdminController.cs
 │    ├── MobileController.cs
 │    └── BankingController.cs
 ├── Data/
 │    ├── BillingDbContext.cs
 │    └── SeedData.cs
 ├── Models/
 ├── Dtos/
 ├── Middleware/
 │    └── RequestResponseLogging.cs
 ├── appsettings.json
 ├── Program.cs
 └── Billing.Api.csproj

🔥 Authentication
Login Request Body
{
  "clientType": "Admin",
  "username": "admin",
  "password": "123456"
}


ClientType can be:

"Admin"

"Mobile"

"Bank"

After login:

Copy your token → Click Authorize in Swagger → Paste as:

Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

🧑‍💼 Admin Endpoints
➕ Add a Bill

POST /api/v1/admin/add-bill

{
  "subscriberNo": "1001",
  "year": 2025,
  "month": 12,
  "totalAmount": 300
}

🗂 Upload CSV Batch

POST /api/v1/admin/add-bill-batch

CSV format:

SubscriberNo,Year,Month,TotalAmount
1001,2024,12,200
1002,2024,11,150


Returns success and error counts.

📱 Mobile Endpoints
📌 Mobile Login

POST /api/v1/auth/login

{
  "clientType": "Mobile",
  "username": "murat",
  "password": "123456"
}

🔍 Query Bill

GET /api/v1/mobile/query-bill?subscriberNo=1001

Response example:

{
  "subscriberNo": "1001",
  "month": 12,
  "year": 2025,
  "billTotal": 300,
  "isPaid": false
}

🏦 Banking Endpoints
💰 Pay Bill

POST /api/v1/banking/pay

{
  "iban": "TR00000000001",
  "amount": 300,
  "subscriberNo": "1001"
}


Marks bill as paid in Azure SQL.

🌐 Azure Deployment
Used Services:

Azure SQL Database

Azure App Service

Azure API Management (optional)

ConnectionStrings stored securely

Swagger enabled in production via:

"Swagger": {
  "EnableInProduction": true,
  "ServerUrl": ""
}


Deployed live at:

[https://billingpaymentsystem-<region>.azurewebsites.net/](https://billingpaymentsystem-fpagf3eda5bqfqh6.francecentral-01.azurewebsites.net)

🧪 Testing via Swagger

Go to root URL

Swagger UI automatically opens (RoutePrefix = "")

Login

Authorize

Test endpoints freely

📝 Logging Middleware

Every request and response is logged:

app.UseRequestResponseLogging();


Helps with debugging and API monitoring.

🗄 Database Migration Commands
dotnet ef migrations add InitialCreate
dotnet ef database update

