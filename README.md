# DataForge

DataForge is a full-stack data analytics workspace built with ASP.NET Core, React, TypeScript and SQL Server.

The project combines full-stack development with practical data profiling, quality analysis, exploration and transformation workflows.

## Main workflow

Import -> Profiling -> Quality -> Exploration -> Cleaning -> Analysis -> Reporting -> History

## Features

- JWT authentication and protected API endpoints
- User profile management
- Project and dataset management
- CSV, Excel and SQL Server import workflows
- Automatic dataset profiling
- Column type and semantic role detection
- Missing value detection
- Duplicate detection
- Outlier detection
- Data quality scoring
- Column statistics and distributions
- Dataset preview and pagination
- Transformation planning and auditing
- Analysis snapshots
- Dataset reports
- Activity history
- CSV and Excel export

## Data cleaning

The current cleaning module records and audits transformation operations such as duplicate removal, missing-value treatment, text normalisation and type conversion.

Physical rewriting and versioning of source datasets is considered a future evolution of the project.

## Architecture

DataForge uses a layered solution structure:

- DataForge.Api - ASP.NET Core Web API
- DataForge.Application - application layer
- DataForge.Domain - domain layer
- DataForge.Infrastructure - EF Core, SQL Server and infrastructure services
- DataForge.Tests - automated tests
- DataForge.Web - React and TypeScript frontend

## Technology stack

### Backend

- C#
- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT Authentication
- REST API

### Frontend

- React
- TypeScript
- Vite
- Axios
- React Router
- Lucide React
- CSS

### Tools

- Visual Studio
- Visual Studio Code
- Git
- GitHub
- Postman
- npm

## Local configuration

Sensitive local configuration is not committed to this repository.

Copy:

DataForge.Api/appsettings.example.json

to:

DataForge.Api/appsettings.json

and configure your own SQL Server connection string and JWT signing key.

## Requirements

- .NET 10 SDK
- Node.js
- npm
- SQL Server

## Backend

Restore and build:

    dotnet restore
    dotnet build .\DataForge.slnx

Run the API:

    dotnet run --project .\DataForge.Api

## Frontend

Install dependencies:

    cd .\DataForge.Web
    npm.cmd ci

Start development server:

    npm.cmd run dev

Default Vite development address:

    http://localhost:5173

## Production build

Backend:

    dotnet build .\DataForge.slnx --configuration Release

Frontend:

    cd .\DataForge.Web
    npm.cmd ci
    npm.cmd run build

## Security

The repository does not include the local appsettings.json, local datasets, build output, node_modules, certificates or private keys.

Never commit production credentials or JWT signing keys.

## Future improvements

- Physical dataset versioning after transformations
- Additional visualisations
- Expanded automated tests
- Deployment pipeline
- Power BI integration

## Author

Daniel Carvalho

Full Stack Developer | Data Analyst / Business Intelligence

GitHub: DanielCarvalho-devs

## Project purpose

DataForge was created as a portfolio project to demonstrate practical experience with full-stack development, REST APIs, authentication, relational databases, data processing, data quality analysis, React, TypeScript and layered backend architecture.
