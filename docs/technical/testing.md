# Testing and quality checks

Run commands from the repository root unless a command changes directory. Use
.NET 10 and Node.js 24, matching the project manifests and CI.

## Backend

```bash
dotnet build
dotnet test
```

The test project contains xUnit service tests and SQL Server-backed integration
tests. `ApiIntegrationTestFactory` uses Testcontainers to start a temporary SQL
Server and `WebApplicationFactory` to host the API in Testing. Docker must be
available for the integration tests. The factory replaces email, storage, cache,
Google-token validation and background jobs with test doubles; it does not test
those external services or the Hangfire Dashboard.

## Frontend

```bash
cd uucars-web
npm ci
npm run lint
npm run build
npm test -- --run
npm run test:coverage
```

`build` runs TypeScript checking and a Vite production build. Vitest and React
Testing Library cover the component suites under `src/test/`; this is not a
claim of complete application coverage. Existing files under `coverage/` are
saved report output, not proof of a fresh passing test run.

For opt-in production bundle analysis:

```bash
ANALYZE_BUNDLE=true npm run build
```

The report is `dist/bundle-report.html`. Ordinary builds do not generate it.
Local bundle/Lighthouse measurements must not be presented as real-user Web
Vitals or as proof of a field-monitoring pipeline.

## End-to-end

The suite runs five Chromium journeys: registration, seller listing submission,
Admin approval, buyer ordering and seller order viewing. It creates its own
buyer/seller users and listings through UI/API helpers. Migrations provide the
test Admin account expected by `e2e/flows.spec.ts`.

Prepare these prerequisites:

- an isolated SQL Server database connection in `ConnectionStrings:E2EConnection`;
- Redis reachable on `localhost:6379`, using database 1 by default;
- a test JWT signing key of at least 32 characters;
- a public Google client ID for the frontend sign-in component;
- free ports 5065 and 5173; do not start the normal API or Vite server first.

The default Compose stack keeps SQL Server and Redis inside its private network,
so it does not supply these host ports. Use dedicated local test services or the
service-container configuration in `.github/workflows/ci.yml`. Never point E2E
at the production database.

The API reads E2E user secrets explicitly, or environment variables can supply
the same keys. For example, with an already running isolated local SQL Server:

```bash
export ConnectionStrings__E2EConnection='Server=localhost,1433;Database=UUcarsE2EDB;User Id=sa;Password=YOUR_LOCAL_TEST_PASSWORD;TrustServerCertificate=True'
export JwtSettings__Secret='replace-with-a-random-test-key-at-least-32-characters'
export VITE_GOOGLE_CLIENT_ID='your-public-google-client-id'
cd uucars-web
npm ci
npx playwright install chromium
npm run e2e
```

`playwright.config.ts` starts the API with the `e2e` launch profile, waits for its
car-list endpoint, then starts Vite on 5173. It sets the frontend API URL to
5065 and uses `reuseExistingServer: false`. It runs with one worker and no
retries. E2E substitutes no-op background jobs, so it does not deliver real
verification/reset emails or run Hangfire. Verification helpers call the
Development/E2E-only token endpoint; that endpoint returns 404 in Production.
The suite does not validate real Google sign-in or R2 image upload.

An existing test database retains data between local runs; repeat runs can also
reach the API's registration rate limit. Use a dedicated disposable test
environment and check failures before retrying. CI creates fresh service
containers for each run.

Playwright writes an HTML report and failure screenshots. The configuration's
`on-first-retry` trace mode produces no retry trace while `retries` is zero; do
not expect a trace from every failure.

## CI and release evidence

CI runs backend build/tests and frontend lint/tests/build in separate jobs.
The E2E job starts only after both succeed. It uses temporary SQL Server and
Redis services, starts the real API/frontend and executes `npm run e2e`; it does
not need a dedicated Azure staging deployment. Failure artefacts are uploaded
only when the E2E job fails.

The release-source check applies to pull requests targeting `main` and requires
`develop` from the same repository. It is normally skipped on push runs. CD
accepts successful main-push CI and performs the deployment and smoke checks
described in [Production delivery](production-delivery.md).

A configured command is not evidence of a successful execution. Record the
actual local result or GitHub run/step outcome when reporting validation.
