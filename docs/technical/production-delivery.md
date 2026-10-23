# Production delivery

## Implemented architecture

- Azure Static Web Apps hosts the React frontend.
- Azure Container Apps runs the ASP.NET Core API.
- Azure SQL Database stores application data.
- Upstash Redis provides the production cache.
- GitHub Container Registry stores the API image.
- Cloudflare R2 stores vehicle images.
- Resend delivers transactional email.
- Hangfire runs inside the API container and stores job data in Azure SQL.
- Azure Application Insights receives ASP.NET Core telemetry and Serilog logs.
- GitHub Actions builds, validates and deploys releases.
- Azure OIDC federated identity authorises backend deployment.

## CI and release path

CI runs the backend build and tests, frontend lint, tests and build, and the real
Playwright end-to-end suite for the configured branch pushes and pull requests.

Production releases use a `develop`-to-`main` pull request after the release
source, backend, frontend and end-to-end checks pass. After a successful `main`
CI run, CD builds both release artefacts from the same commit, publishes the API
image to GHCR by immutable digest, deploys it to Azure Container Apps, uploads
the pre-built frontend to Azure Static Web Apps and performs public read-only
smoke checks.

See [Development and release workflow](development-workflow.md) for branch and
release responsibilities.

## Runtime security and configuration

The production API image runs as the non-root `app` user. Published application
files remain read-only at runtime, while `/app/logs` is writable for Serilog.

Azure Container Apps supplies database, JWT, Redis, email and object-storage
credentials through environment variables backed by Container Apps Secret
references. Public identifiers and service URLs remain ordinary environment
variables. Runtime credentials are not stored in the image or repository.

The Admin-only Hangfire Dashboard is served by the API at
`/admin/hangfire`. Browser navigation uses the existing short-lived access token
through an HttpOnly cookie restricted to that path; normal API requests continue
to use the JWT Bearer header.

## Google sign-in configuration

Production Google sign-in uses the same public OAuth client ID in two places:

- Vite reads `VITE_GOOGLE_CLIENT_ID` during the frontend build.
- Azure Container Apps supplies `GoogleAuth__ClientId` for API ID-token audience
  validation.

The Static Web Apps origin must be registered as an authorised JavaScript origin
in the Google OAuth client. This sign-in flow does not use a Google client secret.

Production secrets belong in GitHub or Azure configuration and must not be
committed to the repository.
