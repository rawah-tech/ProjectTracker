# ProjectTracker (ASP.NET Core + JWT + EF Core + SQL Server)

## Features
- JWT Authentication: register & login
- Project CRUD (user-specific)
- Task CRUD under projects (user-specific)
- SQL Server with EF Core (async/await)
- Unit tests: create project & add task (xUnit + EF InMemory)

## Prerequisites
- .NET SDK (project targets net10.0)
- SQL Server Express (localhost\SQLEXPRESS)

## Configuration
Update `ProjectTracker.Api/appsettings.json`:
- ConnectionStrings:DefaultConnection
- Jwt:Key / Issuer / Audience

Example connection string:
Server=localhost\SQLEXPRESS;Database=ProjectTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;

## Run
```bash
cd ProjectTracker.Api
dotnet ef database update
dotnet run
