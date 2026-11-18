# UUcars frontend

This directory contains the React single-page application for UUcars. It is one
part of the repository-wide system described in the [root README](../README.md).

Use the root README for prerequisites, local configuration, application startup,
quality checks and production delivery. The scripts in `package.json` remain the
authoritative command reference for this package.

## Source layout

```text
src/
├── api/          HTTP client and feature API modules
├── assets/       Frontend-owned static assets
├── components/   Shared application and UI components
├── hooks/        Reusable React Hooks with a concrete shared purpose
├── lib/          Shared infrastructure and utility code
├── pages/        Route-level page components
├── routes/       Lazy route-component imports
├── stores/       Client-side application state
├── test/         Frontend test setup and suites
└── types/        Shared TypeScript contracts
```

Keep route definitions, server-state handling, authentication state, forms and
API response handling consistent with the patterns already used in `src/`.

`src/router.tsx` defines routes and their Error Boundary/Suspense wrappers.
Create/edit, Admin and sign-in route components load on demand; public listing
and detail pages remain eager. `vite.config.ts` enables a bundle report only
when `ANALYZE_BUNDLE=true`. See [testing and quality checks](../docs/technical/testing.md).
