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

## 3. Project status handling

Do not store current version or development-step completion status in this
file. This file contains durable repository rules, not a progress snapshot.

Before starting, reviewing or documenting a development step:

- determine the current state using the source-of-truth order in section 5;
- inspect the relevant development notes, source code, configuration, tests and
  Git history;
- verify relevant CI, deployment and production checks before relying on a
  completion or release claim;
- do not rely on remembered conversation context or a stale roadmap summary;
- never describe planned or partially completed work as implemented.

Keep changing implementation progress in the corresponding `docs/steps_v*.md`
file. Keep public implementation and roadmap status in `README.md` where
appropriate.

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

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
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

The current CI workflow runs the real Playwright suite with temporary SQL
Server, Redis, API and frontend services on a GitHub Runner. Validate changes
on the working feature branch before merging, and confirm develop CI after
the merge. Dedicated Azure Staging is not required. Do not report CI E2E as
passing without a real successful run.

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
- Keep version outline formats consistent with the neighbouring versions.
- Keep completed work and roadmap boundaries accurate.

### `docs/database-schema.md`

This is the current complete database reference.

- Keep final fields, indexes, relationships and delete behaviour aligned with
  the Model Snapshot.
- Mark which version introduced each table, field, index and relationship.
- Preserve the version-to-version Migration timeline.
- Do not make instructions for an earlier version depend on fields introduced
  by a later version.

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

## 14. Development step outline rules

Use these rules whenever creating, revising or reviewing the outline for any
development step.

### Establish the boundary first

Before drafting the outline:

- verify the previous step's real completion state against the source-of-truth
  order in section 5;
- identify the current implementation, configuration, test and delivery state;
- state the concrete problem this step needs to solve;
- define what is included, what is excluded and what proves the step complete;
- keep later-step work out of the current step;
- never describe planned work, checks or releases as completed.

If the documents disagree with the implementation, report the discrepancy
before using either version as the outline's starting point.

### Organise around one real development carrier

Choose one primary carrier that represents how the work actually proceeds. It
may be a user journey, request flow, data flow, application lifecycle, delivery
workflow or concrete artefact. Use it to connect the whole step.

Do not organise an outline as a collection of unrelated technology topics. A
technology belongs in the outline only where it supports the carrier and solves
the problem at that stage.

Order sections by real dependencies and implementation sequence. A typical
sequence is:

```text
confirm the starting state
→ establish the method or baseline
→ gather evidence
→ identify the actual problem
→ make the smallest justified change
→ compare and validate
→ document and deliver
```

Adapt the sequence to the work instead of forcing every step into this exact
shape. Each section should create the reason or evidence needed by the next
section.

### Keep decisions proportionate and evidence-led

For every proposed item, check:

- whether it solves a current and observable project problem;
- whether it serves the expected users and normal project scenarios;
- whether its benefit justifies its implementation and maintenance cost;
- whether it belongs in this step rather than a later step;
- whether the existing stack already provides a sufficient solution;
- whether the owner can understand and explain the resulting design.

Do not preselect an optimisation, abstraction, dependency or architecture before
the preceding evidence supports it. The outline must permit a proposed change to
be reduced or rejected when measurement shows that it has little value.

Use techniques that are current, supported, production-relevant and compatible
with the repository's actual versions. When compatibility or the correct
approach is not yet established, plan an investigation and decision point rather
than presenting an assumption as the chosen implementation.

Keep related technical concepts distinct. Do not treat similar metrics, loading
states, error boundaries, test levels, deployment stages or architectural
patterns as interchangeable merely to simplify the outline.

### Keep the outline at outline depth

An outline should establish:

- why the work is needed;
- the carrier and logical sequence;
- the evidence or decisions required at each stage;
- the permitted change boundary;
- the validation and completion criteria.

Do not overload it with commands, long concept tutorials, speculative
implementation detail or results that do not yet exist. The number of sections
must follow the content: merging must not remove a required responsibility, and
splitting must improve sequence or clarity rather than satisfy a preferred
section count.

Use wording that preserves the difference between intention and evidence. For
example, write that a step will be released through the existing delivery flow
until the required checks, deployment and production validation have actually
succeeded.

### Audit an outline before saving it

Confirm all of the following:

1. it matches the current source, configuration, tests and delivery state;
2. it has a clear scope and does not pull later work forward;
3. it avoids low-value edge cases, speculative optimisation and unnecessary
   architecture;
4. its techniques are current and compatible with the implemented stack;
5. one real carrier connects the sections;
6. the sections follow causal dependencies and actual development order;
7. it does not repeat responsibilities or reveal unverified conclusions;
8. its completion and release wording reflects evidence rather than intention.

## 15. Development step detail-expansion rules

Use these rules when expanding an approved development step outline into
detailed development and learning notes.

### Re-read the repository guidance first

Before generating new development-step content or expanding existing content,
re-read this entire `AGENTS.md` from the current workspace. Do this for every
such request, even if the file was read earlier in the same conversation. Do not
rely on remembered rules, an earlier summary or previous conversation context.

After re-reading it:

- identify the applicable edit authority, source-of-truth, scope, technical,
  documentation and validation rules;
- inspect the approved outline, adjacent sections and relevant implementation
  evidence;
- resolve or report any conflict before writing;
- apply the rules to both the generated content and the final review.

### Preserve the approved scope

- Expand only the scope explicitly requested by the owner.
- Treat the approved outline as the content contract for purpose, order and
  boundaries.
- Add the explanation, operations and validation needed to fulfil that contract,
  but do not add a new objective or architecture direction.
- Preserve every unrelated section.
- If detailed investigation shows that the outline is inaccurate, conflicting
  or incomplete, report the issue before changing its structure or scope.

### Preserve chronological knowledge and the formal record

Write from the knowledge available at that point in the development sequence.
For example, a baseline section records the current state, an analysis section
derives a conclusion from that evidence, an implementation section applies the
supported change, and a comparison section decides whether it helped.

Do not reveal a later finding, chosen solution, measurement result, test result
or release outcome before the notes reach the stage where it became known. Do
not make expected output look like executed output.

Record durable implementation facts when they matter to the learning sequence,
such as confirmed commits, configuration, technical decisions, executed output
and validation results. Do not copy the writing session into the project record.
Exclude:

- drafting conversation and outline revision history;
- the Agent's current editing activity;
- temporary file modifications created only while drafting documentation;
- deliberation about how the current writing task should be organised;
- transient workspace details that do not affect the implemented system.

If a temporary observation exposes a real implementation problem, record the
problem and its resolution at the appropriate stage, not the surrounding
conversation. Preserve genuine implementation history when section 13 requires
it.

### Maintain one causal and non-repetitive narrative

Within each section, connect the content in this order where applicable:

```text
current state
→ resulting problem or question
→ reason this work is needed now
→ action or decision
→ validation and conclusion
```

Sequential explanatory paragraphs should have an explicit causal or
transitional relationship. Keep the same user journey, request flow, data flow,
lifecycle, workflow or artefact as the local narrative carrier. Do not switch
between unrelated technology explanations merely because they belong to the
same broad topic.

Check repetition by meaning, not only by matching words. Explain a concept fully
where it first becomes necessary, then refer only to the part needed later. State
the step goal, full workflow, scope exclusions and release process at the level
where each belongs; do not repeat their complete form in later operational
sections.

### Use the minimum sufficient explanation

Detailed notes must be understandable without becoming either a bare operation
manual or a general textbook. Keep explanation that is needed to establish:

- why the work is needed at this point;
- the concrete project problem addressed by the action;
- non-obvious tool behaviour, parameters or technical consequences;
- what an observed or expected result means;
- how the result creates the next development step.

Remove explanation that only:

- describes an obvious normal operation;
- repeats a reason already established nearby or in another section;
- defends against a low-probability case outside the agreed scope;
- repeats a scope boundary that is already clear;
- adds words without changing the reader's understanding or decision.

After showing a result, explain its immediate significance once. Do not extend
the same conclusion through several paraphrased benefits. When considering a
deletion, ask whether the reader can still understand why the action occurs,
what happened and what the result means. If all three remain clear, remove the
sentence.

Explain an unfamiliar technique only when it is needed for the current work.
Cover the concrete problem, how the technique addresses it, the simpler
alternative where relevant and why added complexity is justified. Keep the
depth suitable for section 2. Verify code, commands, paths, configuration keys
and tool behaviour against the current repository and dependency versions.

### Keep overviews concise

A step overview should normally be limited to:

1. the transition from the previous completed state;
2. the current step's central problem;
3. the primary development or user carrier;
4. the governing implementation principle;
5. the goal and scope boundary.

Do not turn the overview into a summary of every later section. Leave detailed
technology choices, tutorials, commands, measurements and outcomes to the stage
where they are introduced or verified.

### Preserve evidence boundaries

Clearly distinguish:

- observed state from an assumption;
- expected output from actual output;
- a local or controlled measurement from production user data;
- an automated check from manual verification;
- a correlation from a demonstrated cause;
- planned delivery from a completed release.

Never invent command output, measurements, passing tests or deployment results.
When a result is unchanged or inconclusive, record that honestly rather than
manufacturing a stronger learning outcome.

### Audit each expanded section

Before considering a section complete, re-read the resulting section and its
adjacent sections. Confirm that it:

- still fulfils the approved outline responsibility;
- follows naturally from the preceding section and prepares the next one;
- contains no semantic repetition, drafting traces, transient writing-session
  details or premature conclusions;
- preserves necessary reasoning without explaining routine actions at length;
- is neither a bare command list nor an unrelated concept tutorial;
- uses technically accurate and current guidance;
- keeps commands and examples executable and free of secrets;
- labels unexecuted work and evidence limitations honestly;
- changes no unrelated content;
- has valid Markdown structure and code fences.

For every paragraph, ask:

1. Does it record or explain the real implementation rather than the writing
   process?
2. Does it contribute information that is not already present elsewhere?
3. Would removing it damage understanding of the reason, action or result?
4. Is it project-specific and useful rather than an explanation of an obvious
   normal operation?
5. Does it remain in the section where that information first becomes necessary?

Delete, merge or move the paragraph when the answers show that it is redundant,
misplaced or part of the drafting context.

## 16. Git workflow

Branch roles:

```text
main       production releases; milestone commits may receive tags
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
→ verify develop CI
→ retain the feature branch until the step's release and close-out are complete
```

Production releases use a pull request from this repository's `develop` to
`main`. Require the release-source check and Backend, Frontend and E2E checks
before merging, using Create a merge commit. Daily feature merges into develop
retain the existing local merge workflow; they do not require pull requests.
Protect main without routine bypass permissions; another person's review is not
required for this solo project.

The implemented CD workflow deploys the exact commit from successful main push
CI. Treat future rule or deployment changes as implemented only after their
configuration and real execution have been verified.

After a production release, synchronise the resulting `main` merge commit back
into `develop`. Delete a feature branch only after its release and required
checks are complete; never delete `develop` as the release source branch.

Do not stage, commit, push, merge, rebase, delete a branch or create a pull
request unless the owner explicitly requests that Git action.

Before suggesting Git commands:

- inspect the current branch;
- inspect staged, unstaged and untracked changes;
- distinguish the current task from existing owner changes;
- avoid commands that could overwrite work.

Never use destructive Git recovery commands without explicit approval.

## 17. Definition of done

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
