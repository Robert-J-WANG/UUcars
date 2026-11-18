# Local development

## Prerequisites

- .NET 10 SDK
- Node.js 24
- Docker Desktop or another Docker-compatible runtime
- A Google OAuth 2.0 Web client ID for real Google sign-in
- Cloudflare R2 credentials for real image operations
- A Resend API key for real email delivery

Before running npm commands, check `node --version`. The package requires Node
24; an older system Node on PATH can cause Vitest worker startup failures even
when dependencies are installed. If using nvm, select `nvm use 24` in the current
shell. The CI Node version does not change your local shell automatically.

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
pending EF Core Migrations during startup. The migrations include a development
Admin seed; replace the seeded account credentials before exposing a deployment
and never treat the test account as a production secret.

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

The checked-in `.env.example` uses the Compose API on port 8080. Do not copy the
production API URL into local test settings. Vite builds also read the tracked
`.env.production`; ignored `.env.local` values can override those defaults, so
review the environment before preparing a production artefact.

The default R2 bucket name in `appsettings.json` is `uucars-images`; the local
credentials and public URL must refer to that bucket. The Google Web client
must allow `http://localhost:5173` as a JavaScript origin. Placeholder external
credentials allow configuration to load but do not make Google, R2 or Resend
features work.

## Native API development

The `http` launch profile runs the API in Development on port 5065. Unlike
Compose, it needs SQL Server and Redis reachable from the host. Supply
`ConnectionStrings:DefaultConnection`, `ConnectionStrings:Redis` and
`JwtSettings:Secret` through the API project's user-secrets store or environment
variables. Add the Google, email and R2 settings when using those features.
`appsettings.json` contains placeholders rather than working credentials.

```bash
dotnet run --project UUcars.API/UUcars.API.csproj --launch-profile http
```

For this mode, change `VITE_API_BASE_URL` in the frontend's ignored `.env.local`
to `http://localhost:5065` and restart Vite. Scalar is available at
`http://localhost:5065/scalar/v1` in Development. It is not mounted by the default
Production-mode Compose configuration.

The default Compose runtime uses Production cookie settings, including Secure
cookies. Use the native Development profile for local HTTP session debugging;
do not relax production cookie settings to accommodate a local browser.

## Isolated browser tests

Playwright manages its own E2E API and Vite processes. It needs a separate test
database and host-accessible Redis; the ordinary Compose startup is not the
E2E setup. Follow [Testing and quality checks](testing.md) before running it.
