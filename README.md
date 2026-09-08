**TMApi**

TMApi is a RESTful API for a task and ticket management system, built with ASP.NET Core and designed with authentication, authorization, logging, and role-based access in mind.

The project is currently being developed incrementally, with authentication and automated testing established as the foundation for the rest of the system.

**Tech Stack**

•	.NET 8

•	ASP.NET Core Web API

•	C#

•	Entity Framework Core

•	SQL Server

•	ASP.NET Core Identity

•	JWT Authentication

•	xUnit

•	Moq

•	GitHub Actions

**Current Features**

**Authentication**

•	User registration

•	User login

•	JWT token generation

•	Password validation

•	Role-based authorization

•	User role management

•	User administration

•	Authentication logging

**Roles**

The system currently uses:

•	**Admin** — Full system administration

•	**Agent** — Works with assigned tasks/tickets

•	**Requester** — Creates and views their own requests



New registrations are assigned the default user role configured by the application.



**Testing**

The project contains an automated unit test suite for the authentication service.



Current test status:

8 tests passing

Passed: 8

Failed: 0

Skipped: 0

Tests are automatically executed through GitHub Actions.



**CI/CD**

GitHub Actions currently performs the following whenever changes are pushed to **master**:

Push to master

&#x20;     ↓

Restore dependencies

&#x20;     ↓

Build solution

&#x20;     ↓

Run tests

&#x20;     ↓

Publish API

&#x20;     ↓

Create deployment artifact

The deployment artifact is currently stored by GitHub Actions.

Actual server deployment will be added in a later phase.



**Project Structure**

TMApi/

├── .github/

│   └── workflows/

│       └── ci.yml

│

├── TMApi/

│   ├── Controllers/

│   ├── Data/

│   ├── Migrations/

│   ├── Models/

│   ├── Services/

│   ├── Program.cs

│   └── TMApi.csproj

│

├── TMApi.Tests/

│   ├── AuthServiceTest.cs

│   └── TMApi.Tests.csproj

│

└── TMApi.sln

**Getting Started**

**Prerequisites**

•	.NET 8 SDK

•	SQL Server

•	Visual Studio 2022 or another compatible .NET IDE

**Clone the repository**

git clone https://github.com/JayJ-101/TMApi.git

cd TMApi

**Restore dependencies**

dotnet restore TMApi.sln

**Build the solution**

dotnet build TMApi.sln

**Run the tests**

dotnet test TMApi.sln

**Run the API**

dotnet run --project TMApi/TMApi.csproj

Swagger can then be used to explore and test the API when enabled by the application configuration.



**Development Status**

The project is under active development.

Current focus:

•	Authentication and Identity

•	Unit testing

•	Logging

•	CI automation

•	Deployment preparation

Future development will expand the task/ticket management functionality and its supporting services.



**Author**

Sinaye Tontsi

GitHub: [JayJ-101](https://github.com/JayJ-101)



