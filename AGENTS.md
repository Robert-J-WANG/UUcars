# UUcars Repository Guidance

This file applies to the entire repository.

## 1. Project identity

UUcars is a personal full-stack portfolio and learning project. It is not an
internship, employment placement, commercial product or team-delivered system.

The project exists to:

- demonstrate practical ASP.NET Core and React full-stack development;
- build a portfolio suitable for software development roles in New Zealand;
- practise realistic development, testing, documentation, Git and deployment
  workflows;
- preserve an honest learning history while presenting the final implemented
  system accurately;
- help the project owner explain every major technical decision in an interview.

Never describe UUcars as professional employment experience. It may be described
as an independently designed and developed portfolio project.

## 2. Developer context

The project owner:

- is an early-career graduate preparing for a first software development role
  in the New Zealand market;
- is learning at junior to early-intermediate full-stack level;
- values clear explanations and code that can be understood and discussed;
- prefers mastering relevant fundamentals over collecting advanced technology
  names.

Use this context when deciding technical depth.

The target level is:

```text
Job-ready junior to early-intermediate full-stack development
with practical production awareness
```

This means:

- maintain professional correctness, security and testing standards;
- teach the concepts commonly expected in junior and early-intermediate roles;
- use production-aware patterns where they solve a real project problem;
- prefer readable, explicit implementations over clever abstractions;
- avoid enterprise-scale architecture that the project does not need;
- avoid premature or speculative performance optimisation;
- do not add a technology solely because it appears impressive on a CV.

Do not introduce microservices, CQRS, Event Sourcing, Kubernetes, complex domain
frameworks or similar advanced architecture unless a future requirement creates
a concrete need and the owner explicitly approves it.

Security, data integrity and correctness are never optional merely because the
project targets an early-career level.

## 3. Current project status

The implemented system currently covers:

- V1: Step 01–35;
- V2.0: Step 36–56;
- V2.1: Step 56b;
- V3: Step 57–70.

V3 Step 70 is complete.

V3 Step 71–75 remain roadmap items:

- Admin data dashboard;
- Google OAuth;
- CI/CD improvement;
- bundle analysis and Web Vitals;
- final V3 release and portfolio close-out.

Do not describe roadmap items as implemented. Update this section when those
steps are genuinely completed.

## 4. Language and communication

Use the following defaults:

- communicate with the project owner in Chinese;
- explain plans, findings, trade-offs and errors in Chinese;
- keep code identifiers, API routes, type names and commit messages in English;
- write public-facing repository documentation in English unless the owner asks
  otherwise;
- use New Zealand English in English prose.

Preferred NZ English includes:

```text
authorisation
behaviour
colour
favourite
optimisation
organisation
```

Do not rename established code identifiers or API contracts merely to change
spelling. For example, the existing `/favorites` routes and `Favorite` types must
remain unchanged unless an intentional breaking change is approved.

Chinese learning notes may retain established English technical terminology.

## 5. Source-of-truth order

When claims conflict, use this order:

```text
1. Current source code and runtime configuration
2. EF Core Migrations and AppDbContextModelSnapshot
3. Current automated tests
4. Current version development notes
5. docs/database-schema.md
6. Version development outlines
7. README.md
```

Git history and release tags may be used to confirm when a feature was
introduced.

Never change working code merely to match stale documentation. First determine
whether the discrepancy is:

- a documentation delay;
- a code defect;
- an unfinished implementation;
- a historical approach that was later replaced.

Report the discrepancy before changing documentation. If code behaviour must
change, explain the plan and wait for the required authority.

## 6. Technology boundaries

The established project stack is:

### Backend

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core 9
- SQL Server and Azure SQL Database
- JWT Bearer authentication
- Refresh-token rotation
- Redis
- Hangfire
- SignalR
- Serilog
- Azure Application Insights
- Resend
- Cloudflare R2 through the AWS S3-compatible SDK

### Frontend

- React 19
- TypeScript 6
- Vite 8
- React Router 7
- Tailwind CSS 4
- shadcn/ui and Radix UI
- TanStack Query 5
- Zustand
- Axios
- React Hook Form
- Zod
- Sonner
- dnd-kit
- Embla Carousel

### Testing

- xUnit
- WebApplicationFactory
- Testcontainers for SQL Server
- Vitest
- React Testing Library
- Playwright

### Delivery

- Docker
- Docker Compose
- GitHub Actions
- GitHub Container Registry
- Azure Static Web Apps
- Azure Container Apps
- Azure SQL Database

Production documentation must use the implemented Azure architecture. Do not
claim an unimplemented hosting or reverse-proxy deployment.

Before adding or replacing a dependency:

1. explain the problem it solves;
2. check whether the existing stack already solves the problem;
3. explain the learning and maintenance cost;
4. prefer the smallest suitable solution;
5. obtain approval if it changes the project architecture or learning scope.

## 7. Appropriate engineering depth

Prefer implementations the owner can explain from first principles.

Good depth for this project includes:

- Controller–Service–Repository separation;
- dependency injection and configuration binding;
- DTO validation and explicit mapping;
- authentication and role-based authorisation;
- EF Core relationships, indexes and Migrations;
- database transactions and optimistic concurrency;
- cache-aside and intentional cache invalidation;
- background jobs with clear operational purpose;
- structured logging and basic observability;
- unit, integration, component and end-to-end testing;
- CI/CD and environment-based configuration;
- clear handling of failures and security boundaries.

Avoid unnecessary complexity such as:

- extracting a Hook or component without improving reuse or readability;
- adding `useMemo`, `useCallback` or memoisation without a demonstrated need;
- creating generic abstractions for a single simple use case;
- adding layers that only forward calls;
- performance optimisation without measurement or a real bottleneck;
- replacing a working, understandable solution with a more fashionable one.

If an unfamiliar technique is genuinely useful, explain:

- what problem exists;
- how the technique solves it;
- a simpler alternative;
- why the added complexity is justified.

## 8. Collaboration and edit authority

Interpret requests according to their wording.

### Read-only requests

Requests such as:

```text
review
inspect
check
compare
discuss
explain
evaluate
give suggestions
```

are read-only. Do not edit files.

### Manual-edit requests

If the owner asks for code, replacement text or operation steps to apply
manually:

- provide complete, copy-ready content;
- name the exact target file and location;
- do not edit the workspace;
- explain validation commands separately.

### Direct-edit requests

Only edit files when the owner clearly asks to:

```text
modify
update
implement
apply
replace
save
delete
rename
```

Before code changes:

- give a short plan;
- name the files expected to change;
- state any important assumption;
- do not modify unrelated files.

When the scope is restricted to one feature or section, preserve every other
part of the file.

Existing uncommitted changes belong to the owner. Never discard, overwrite or
revert them without explicit approval.

## 9. Backend conventions

- Keep Controllers thin.
- Put business rules in Services.
- Put EF Core query and persistence behaviour in Repositories.
- Use DTOs at the API boundary; do not expose Entities directly.
- Use the existing `ApiResponse<T>` and `PagedResponse<T>` contracts.
- Use async APIs and propagate `CancellationToken` where the surrounding code
  supports it.
- Use `DateTime.UtcNow` for application timestamps.
- Keep authentication identity server-derived; do not trust client-supplied
  user IDs for ownership.
- Preserve appropriate 400, 401, 403, 404, 409 and 500 semantics.
- Route unexpected failures through the global exception middleware.
- Do not log passwords, access tokens, refresh tokens or external-service
  secrets.
- Keep Admin audit logging for sensitive moderation operations.

For EF Core:

- create a Migration for every model change;
- inspect both `Up` and `Down`;
- inspect `AppDbContextModelSnapshot`;
- use SQL Server-compatible types and behaviour;
- configure decimal precision explicitly;
- consider index usefulness against real query shapes;
- specify delete behaviour intentionally;
- use `AsNoTracking()` for appropriate read-only queries;
- preserve `Cars.RowVersion` concurrency handling;
- update `docs/database-schema.md` with the version that introduces the change.

Do not add database fields early merely because a later version will need them,
unless the corresponding outline explicitly plans a reserved field.

## 10. Frontend conventions

- Use React function components and TypeScript.
- Keep route definitions centralised in the existing router.
- Use TanStack Query for server state.
- Use Zustand for the established authentication client state.
- Use React Hook Form and Zod for forms.
- Keep the current Axios responsibility split:
  - the request interceptor reads the access token;
  - the response interceptor handles transport and business failures;
  - individual API modules unwrap `response.data.data`.
- Use Query keys and invalidation deliberately.
- Use optimistic updates only when they materially improve the interaction and
  rollback behaviour is clear.
- Preserve the current Error Boundary and global query-error feedback.
- Keep accessible labels, semantic controls and keyboard behaviour in UI work.
- Reuse established UI components and design tokens.

Do not extract logic into a custom Hook simply to reduce the line count of one
small component. Prefer local, readable code until reuse or complexity justifies
an abstraction.

## 11. Security and configuration

- Never commit real secrets.
- Use .NET User Secrets for local backend secrets.
- Use environment variables or managed platform configuration in production.
- Only variables prefixed with `VITE_` may be intentionally exposed to the
  frontend build.
- Keep JWT signing keys, database passwords, Resend keys and R2 credentials out
  of source files.
- Preserve HttpOnly refresh-token cookie behaviour.
- Preserve CORS allow-listing and middleware order.
- Preserve rate limiting and security headers.
- Do not weaken authentication, authorisation or ownership checks to simplify a
  demo.

## 12. Testing and validation

Run checks proportionate to the change.

### Backend

```bash
dotnet build
dotnet test
```

Run focused tests first when practical. Integration tests use Testcontainers and
require Docker.

### Frontend

```bash
cd uucars-web
npm run lint
npm run build
npm test -- --run
```

Use coverage or Playwright when the change affects their scope:

```bash
npm run test:coverage
npm run e2e
```

Playwright requires the local API and frontend, prepared data and the expected
test Admin account.

The current GitHub Actions E2E job is a staging placeholder and does not execute
the real Playwright suite. Do not report CI E2E as passing until that job is
actually enabled against a staging environment.

### Documentation

For documentation-only changes:

- check local links;
- check Markdown code fences;
- run `git diff --check`;
- verify version and completion claims against code and history;
- do not run unrelated application tests unless the documentation change
  depends on them.

If a relevant check cannot run, state:

- the command attempted;
- the exact failure;
- whether the failure is caused by the change or the environment.

Never describe an unexecuted test as passing.

## 13. Documentation responsibilities

Each document type has a distinct purpose.

### `docs/steps_v*.md`

These are detailed development and learning notes.

- Preserve the real implementation sequence.
- Preserve useful historical learning material.
- Clearly mark superseded or abandoned approaches.
- Do not silently rewrite history as though mistakes never occurred.
- Add future steps only when their implementation work begins.

### `docs/v*_完整纲要.md`

These are canonical version development outlines.

- Write them as coherent plans prepared before development.
- Keep them aligned with the confirmed implementation.
- Do not include “original plan versus actual result” narration.
- Remove technologies and features that are not part of that version.
- Keep the V1, V2 and V3 format consistent.
- Keep completed work and roadmap boundaries accurate.

### `docs/database-schema.md`

This is the current complete database reference.

- Keep final fields, indexes, relationships and delete behaviour aligned with
  the Model Snapshot.
- Mark which version introduced each table, field, index and relationship.
- Preserve the V1 → V2 → V3 Migration timeline.
- Do not make V1 instructions depend on V2 or V3 fields.

### `README.md`

This is the public entry point.

- Describe only currently implemented behaviour as complete.
- Separate roadmap work clearly.
- Keep setup, tests and production architecture accurate.
- Do not present historical learning notes as the final architecture.
- Keep the personal portfolio positioning explicit.

### Documentation update order

After an implementation change, use this order where applicable:

```text
1. Source code, configuration, Migration and tests
2. Corresponding docs/steps_v*.md development record
3. Corresponding version outline if the plan changed
4. docs/database-schema.md for database changes
5. README.md for public project status
```

Before changing documentation, inspect and report discrepancies. Do not update
README first and leave lower-level source documents inconsistent.

## 14. Git workflow

Branch roles:

```text
main       released milestones and tags
develop    normal development baseline
feature/*  feature work
fix/*      isolated fixes
docs/*     documentation-only work
```

Normal feature flow:

```text
latest develop
→ feature branch
→ implementation and tests
→ review
→ merge --no-ff into develop
→ delete completed feature branch
```

Only milestone releases should merge `develop` into `main` and create a tag.

Do not stage, commit, push, merge, rebase, delete a branch or create a pull
request unless the owner explicitly requests that Git action.

Before suggesting Git commands:

- inspect the current branch;
- inspect staged, unstaged and untracked changes;
- distinguish the current task from existing owner changes;
- avoid commands that could overwrite work.

Never use destructive Git recovery commands without explicit approval.

## 15. Definition of done

A change is complete only when:

- its behaviour matches the agreed scope;
- unrelated files are unchanged;
- relevant build, lint and tests have been run where practical;
- failures and environment limitations are reported honestly;
- Migrations and Schema documentation are updated when required;
- development notes reflect the work;
- the outline and README do not overstate completion;
- no secret or sensitive value has been introduced;
- the owner receives a concise summary of files changed and checks performed.

The project should remain understandable, demonstrable and interview-ready for
New Zealand junior to early-intermediate software development roles.
