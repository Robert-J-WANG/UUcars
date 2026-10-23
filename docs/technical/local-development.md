# Local development

## Prerequisites

- .NET 10 SDK
- Node.js 24
- Docker Desktop or another Docker-compatible runtime
- A Google OAuth 2.0 Web client ID for real Google sign-in
- Cloudflare R2 credentials for real image operations
- A Resend API key for real email delivery

## Local configuration

Create an ignored root `.env` file:

```dotenv
DB_PASSWORD=choose-a-strong-local-password
JWT_SECRET=choose-a-long-random-secret
GOOGLE_CLIENT_ID=your-google-client-id
RESEND_API_KEY=your-resend-api-key
RESEND_FROM_EMAIL=onboarding@resend.dev
R2_ACCESS_KEY_ID=your-r2-access-key-id
R2_SECRET_ACCESS_KEY=your-r2-secret-access-key
R2_ACCOUNT_ID=your-r2-account-id
R2_PUBLIC_URL=https://your-public-r2-domain
```

`DB_PASSWORD` and `JWT_SECRET` are required. The Google, Resend and R2 values
are required only when exercising their corresponding features.

## Run the API and infrastructure

Start the API, SQL Server and Redis together:

```bash
docker compose up --build
```

The API is exposed on `http://localhost:8080`. SQL Server and Redis remain
inside the Compose network and are not published on host ports. The API applies
pending EF Core Migrations during startup.

## Run the frontend

Create an ignored `uucars-web/.env.local` file:

```dotenv
VITE_API_BASE_URL=http://localhost:8080
VITE_GOOGLE_CLIENT_ID=your-google-client-id
```

The frontend Google client ID must match `GOOGLE_CLIENT_ID` in the root `.env`
when Google sign-in is being tested.

Start the frontend:

```bash
cd uucars-web
npm ci
npm run dev
```

The frontend listens on `http://localhost:5173`.
