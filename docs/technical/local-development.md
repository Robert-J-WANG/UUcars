# Local development

## Prerequisites

- .NET 10 SDK
- Node.js 24
- Docker Desktop or another Docker-compatible runtime
- A Google OAuth 2.0 Web client ID for real Google sign-in
- Cloudflare R2 credentials for real image operations
- A Resend API key for real email delivery

## Local infrastructure

Create a root `.env` file with local-only values:

```dotenv
DB_PASSWORD=choose-a-strong-local-password
JWT_SECRET=choose-a-long-random-secret
```

Start SQL Server and Redis:

```bash
docker compose up -d sqlserver redis
```

## API configuration

Use .NET User Secrets for sensitive local API configuration:

```bash
dotnet user-secrets --project UUcars.API set \
  "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=UUcarsDB;User Id=sa;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True"

dotnet user-secrets --project UUcars.API set \
  "ConnectionStrings:Redis" "localhost:6379"

dotnet user-secrets --project UUcars.API set \
  "JwtSettings:Secret" "YOUR_LONG_RANDOM_JWT_SECRET"

dotnet user-secrets --project UUcars.API set \
  "GoogleAuth:ClientId" "YOUR_GOOGLE_CLIENT_ID"

dotnet user-secrets --project UUcars.API set \
  "EmailSettings:ApiKey" "YOUR_RESEND_API_KEY"

dotnet user-secrets --project UUcars.API set \
  "StorageSettings:AccessKeyId" "YOUR_R2_ACCESS_KEY_ID"

dotnet user-secrets --project UUcars.API set \
  "StorageSettings:SecretAccessKey" "YOUR_R2_SECRET_ACCESS_KEY"

dotnet user-secrets --project UUcars.API set \
  "StorageSettings:AccountId" "YOUR_R2_ACCOUNT_ID"

dotnet user-secrets --project UUcars.API set \
  "StorageSettings:PublicUrl" "YOUR_R2_PUBLIC_URL"
```

Non-secret defaults are defined in `UUcars.API/appsettings.json`.

## Run the applications

Start the API:

```bash
dotnet restore
dotnet run --project UUcars.API
```

The Development API listens on `http://localhost:5065` and applies pending EF
Core Migrations during startup.

Create `uucars-web/.env.local`:

```dotenv
VITE_API_BASE_URL=http://localhost:5065
VITE_GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID
```

Start the frontend:

```bash
cd uucars-web
npm ci
npm run dev
```

The frontend listens on `http://localhost:5173`.

## Dockerised API option

To start SQL Server, Redis and the API together:

```bash
docker compose up --build
```

The containerised API is exposed on `http://localhost:8080`. Real email and R2
operations still require their corresponding configuration.
