# Testing and quality checks

## Backend

```bash
dotnet build
dotnet test
```

Backend integration tests use Testcontainers and require Docker.

## Frontend

```bash
cd uucars-web
npm run lint
npm run build
npm test -- --run
npm run test:coverage
```

## End-to-end

With the API on port `5065` and the frontend on port `5173`:

```bash
cd uucars-web
npm run e2e
```

The Playwright suite covers registration, seller listing submission, Admin
approval, buyer ordering and seller order viewing. It requires prepared test
data and the expected test Admin account.

The GitHub Actions CI workflow runs the real Playwright suite with temporary SQL
Server, Redis, API and frontend services on the runner. It runs alongside the
backend and frontend checks without a dedicated Azure staging deployment.

Never report an unexecuted local or CI check as passing.
