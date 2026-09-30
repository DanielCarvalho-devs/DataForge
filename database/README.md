# DataForge Database

This directory contains the SQL Server database schema used by DataForge.

## Database schema

The file DataForgeDb.sql contains the database structure required by the application.

It includes tables, relationships, constraints and other schema objects generated from SQL Server.

Application data, imported datasets, local connection strings, passwords and development secrets are intentionally excluded.

## Setup

1. Open SQL Server Management Studio.
2. Connect to your SQL Server instance.
3. Execute DataForgeDb.sql.
4. Copy DataForge.Api/appsettings.example.json to DataForge.Api/appsettings.json.
5. Configure your local SQL Server connection string.
6. Configure your own secure JWT signing key.
7. Start the DataForge API.

## Security

Never commit production data, local credentials, connection strings or JWT secrets.
