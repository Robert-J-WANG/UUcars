# UUcars Repository Guidance

This file applies to the entire repository. It contains durable instructions
for agents working on UUcars. Current product status, dependency versions and
feature-specific implementation detail belong in source code or project
documentation, not in this file.

## 1. Project and developer context

UUcars is an independently designed and developed full-stack portfolio and
learning project. It is not employment, an internship, a commercial product or
a team-delivered system.

The owner is an early-career graduate preparing for a first software development
role in New Zealand. Aim for job-ready junior to early-intermediate work with
practical production awareness:

- maintain professional correctness, security and testing standards;
- explain concepts from first principles when they are introduced;
- prefer readable, explicit code over clever abstractions;
- use production-aware patterns only when they solve a real project problem;
- avoid enterprise-scale architecture and speculative optimisation;
- do not add technology merely because it looks impressive on a CV.

The owner should be able to explain every major decision in an interview.
Security, data integrity and correctness are never optional.

## 2. Communication

- Communicate with the owner in Chinese.
- Explain findings, plans, trade-offs and failures in Chinese.
- Keep identifiers, API routes, type names and commit messages in English.
- Use New Zealand English in public English prose, including `authorisation`,
  `behaviour`, `colour`, `favourite`, `optimisation` and `organisation`.
- Do not rename established identifiers or API contracts merely to change
  spelling.
- Chinese design documents and personal learning notes may retain established
  English technical terms.

## 3. Authority and collaboration

Interpret the owner's wording literally.

### Read-only work

Requests such as `review`, `inspect`, `check`, `compare`, `discuss`, `explain`
and `evaluate` authorise investigation and recommendations only. Do not edit
files.

### Manual work

When the owner asks for code, text or steps to apply manually:

- provide complete, copy-ready material;
- name the exact file and location;
- keep validation commands separate;
- do not edit the workspace.

### Direct edits

Edit files only when the owner clearly asks to modify, update, implement, apply,
replace, save, delete, rename or otherwise carry out the change.

Before editing:

1. state the intended result and expected files;
2. identify important assumptions;
3. inspect relevant source and existing changes;
4. preserve every unrelated part of the repository.

Existing staged, unstaged and untracked work belongs to the owner. Never discard,
overwrite or revert it without explicit approval.

## 4. Sources of truth

For current behaviour, use this order:

```text
1. Source code and runtime configuration
2. EF Core Migrations and AppDbContextModelSnapshot
3. Current automated tests
4. Current technical documentation
5. README.md
6. Version design documents
```

Use Git history and release tags to establish when behaviour changed. Personal
learning notes are historical records, not authority for the current system.

When sources disagree, determine whether the difference is stale documentation,
a defect, unfinished work or a superseded approach. Report the discrepancy
before changing behaviour. Never change working code solely to match stale prose.

## 5. Engineering boundaries

Follow the architecture, dependencies and conventions demonstrated by the
current code. Before adding or replacing a dependency:

1. define the concrete problem;
2. check whether the existing stack already solves it;
3. explain the learning and maintenance cost;
4. prefer the smallest suitable solution;
5. obtain approval when architecture or learning scope would change.

Do not introduce microservices, CQRS, Event Sourcing, Kubernetes, complex domain
frameworks or comparable architecture without a concrete requirement and the
owner's approval.

Avoid abstractions that only forward calls, generic infrastructure for one
simple case, custom Hooks created only to reduce line count, and memoisation
without measurement. When an unfamiliar technique is useful, explain the
problem, mechanism, simpler alternative and reason the added complexity is
justified.

## 6. Backend, frontend and data conventions

Respect the responsibilities already established in the codebase.

For backend work:

- keep Controllers focused on HTTP concerns;
- keep business rules in Services and persistence in Repositories;
- use DTOs at API boundaries and preserve established response contracts;
- use asynchronous APIs and propagate cancellation where surrounding code does;
- derive identity and ownership on the server;
- preserve meaningful HTTP status semantics and global exception handling;
- never log passwords, tokens or external-service secrets.

For EF Core work:

- create a Migration for every model change;
- inspect `Up`, `Down` and `AppDbContextModelSnapshot`;
- configure precision, indexes and delete behaviour intentionally;
- use read-only query patterns where appropriate;
- preserve established concurrency handling;
- update the database reference for schema changes.

For frontend work:

- use the established router, server-state, authentication, form and API layers;
- keep query keys and invalidation deliberate;
- use optimistic updates only with a clear interaction benefit and rollback;
- preserve error boundaries and global query-error feedback;
- reuse established components and design tokens;
- preserve semantic controls, accessible labels and keyboard behaviour.

## 7. Security and configuration

- Never commit real secrets.
- Keep local secrets in the established secret store or ignored environment
  files, and production secrets in managed platform configuration.
- Treat intentionally exposed frontend build variables as public.
- Preserve authentication, authorisation, ownership checks, secure cookies,
  CORS allow-listing, middleware order, rate limiting and security headers.
- Do not weaken security to simplify a demonstration.
- Preserve audit logging for sensitive administrative actions.

## 8. Testing and validation

Run checks proportionate to the change. Discover exact commands from the current
project configuration instead of copying potentially stale commands from this
file.

- Run focused checks first when practical.
- Run broader backend, frontend, integration or end-to-end checks when the
  change affects their scope.
- Treat tests requiring containers, services, accounts or prepared data as
  environment-dependent and report those requirements accurately.
- For documentation-only changes, check local links, Markdown fences, factual
  claims and `git diff --check`; do not run unrelated application tests.
- Never describe an unexecuted check as passing.

If a check cannot run, report the command, exact failure and whether the cause is
the change or the environment.

## 9. Documentation system

The repository uses five document categories. Do not make one category perform
another category's job.

Before creating, revising or reviewing a design proposal or personal learning
note, re-read this entire `AGENTS.md` from the current workspace. Do this for
every such request rather than relying on conversation memory or an earlier
summary. Then inspect the target document, its adjacent sections and the relevant
implementation evidence before writing.

### Design proposals — `docs/design/`

Design documents define the intended scope, constraints, sequence and acceptance
criteria for a version or substantial change.

- Write from the planning boundary in which the proposal exists.
- Include future work when it is part of that proposal; this is not a spoiler.
- Do not turn the proposal into an execution diary or copy of current source.
- Keep decisions coherent and remove alternatives that are no longer part of
  the approved design.
- Organise the sequence around one real development, user, request, data or
  delivery flow rather than a catalogue of technologies.
- Keep proposed decisions proportionate to evidence available at the planning
  boundary; do not pull in speculative optimisation or low-value edge cases.
- Keep the proposal at outline depth: state why the work is needed, its logical
  sequence, decision boundaries, validation and acceptance criteria without
  turning it into a command-by-command tutorial.
- Before saving, verify that the scope is complete, sections follow causal
  dependencies, responsibilities are not repeated and unverified results are
  not presented as known outcomes.

### Technical documentation — `docs/technical/`

Technical documents describe the current implemented system for maintainers.

- Keep architecture, data, configuration and operational claims aligned with
  source code and runtime configuration.
- Explain stable concepts and responsibilities, not the conversation that led
  to them.
- Mark historical or superseded behaviour only when it remains relevant.
- Do not use technical references as chronological tutorials.

### Public entry point — `README.md`

The root README is the concise public entry point.

- Describe only implemented behaviour as complete.
- Separate roadmap items clearly.
- Keep setup, tests and production architecture accurate.
- Link to detailed technical documents instead of duplicating them.
- Keep the personal portfolio positioning explicit.

### Agent rules — `AGENTS.md`

This file contains durable collaboration, quality and document-authoring rules.
It must not contain changing release status, dependency inventories, temporary
task notes, detailed feature instructions or chapter-specific content.

### Personal learning notes — `docs/learning/`

Chronological learning notes are stored under `docs/learning/` for the owner's
personal study. Do not link to or mention them from `README.md`, design proposals
or technical documentation. They may include explanations, commands,
implementation excerpts and verified results.

After implementation changes, update only the categories affected by the change:

```text
source, configuration, migrations and tests
→ personal learning record when requested
→ design proposal only if approved scope changed
→ technical documentation for current-system changes
→ README for public-facing status or usage changes
```

## 10. Personal learning-note writing protocol

Learning notes are written for a reader encountering the work in sequence. The
following rules form one mandatory quality gate.

### Stay inside the current knowledge boundary

Before drafting, identify the exact version, step and subsection being learned.
Use only facts, concepts, files and results available by the end of that boundary.
Treat every later section as unknown.

The current part may use only facts established earlier, observations and
actions introduced in the current part, and assumptions explicitly identified
from evidence already available there. It must not name a later heading, use a
later decision or result to justify the current action, or explain an exclusion
by assigning it to a known later part.

Do not mention, hint at, justify work through or create dependencies on later
sections, even if the repository already contains them. Phrases such as “later
we will”, “this prepares for the next section” and references to later results
are spoilers. A design proposal is the only document type that may intentionally
describe approved future scope.

During the final audit, read the part as though every following section were
hidden. Rewrite any sentence that needs later knowledge to make sense, and end
with only the conclusion, open question or immediate evidence requirement known
at the current boundary.

### Write a causal explanation

Organise unfamiliar material in this order where applicable:

```text
current problem
→ why the existing behaviour is insufficient
→ concept and mechanism
→ chosen implementation
→ observable result and validation
```

Do not organise prose as a disconnected catalogue of technologies. Group related
concepts around the problem they solve. Commands and code must follow an
explanation of purpose and expected effect; the notes must not collapse into an
operation manual.

### Preserve facts without preserving the conversation

Record only confirmed observations, decisions and results that are necessary to
establish the current problem, justify a decision, explain the implementation or
validate the current result. An observation does not belong in the note merely
because it is true or was manually verified.

Identify discussion-process traces by their actual meaning and function, not by
surface wording alone. Rewriting a question, conversational detour, correction
or curiosity-driven check as neutral declarative prose does not turn it into
learning material. Omit incidental experiments, transient observations and
other discussion details that do not change the current explanation, decision
or validation. Never invent a command run, result, motivation or decision.

### Use minimal sufficient detail

Explain a concept enough for the owner to understand, apply and discuss it.
Remove generic introductions, repeated summaries, empty transitions and facts
that do not help with the current task. Keep necessary context and causal links;
brevity must not turn the text into unexplained instructions.

### Re-read from the learner's position

Before saving, re-read the complete changed part as a learner who knows only
what the note has established up to the current knowledge boundary. Do not fill
gaps with the author's intent, repository knowledge or concepts explained
elsewhere.

At the first use of every unfamiliar concept, the text must make clear what it
is, what concrete behaviour it has, why it is needed here and how it relates to
the surrounding code. A learner must be able to explain the reason and mechanism
in their own words, not merely copy the shown command or code. If understanding
requires guessing an undefined term, supplying a missing causal step or already
knowing the concept, the draft fails and must be rewritten.

This comprehension pass does not justify exhaustive background material. Add
only the missing explanation required to follow the current causal narrative,
then remove details that do not improve that understanding.

### Run the five-part editorial gate

Before saving learning-note changes, verify all five conditions:

1. no semantic traces of discussion, exploration or drafting remain, even when
   they have been rewritten as neutral factual prose;
2. no information crosses the current knowledge boundary;
3. the explanation follows one causal narrative rather than a technology list;
4. principles, reasons and expected effects accompany operations, and the
   learner-position re-read confirms that every newly introduced concept and
   its role in the shown code can be understood without unstated knowledge;
5. removing any paragraph would lose necessary current-boundary context,
   reasoning, action or evidence; otherwise remove it.

If any condition fails, revise before presenting the text. Do not rely on a later
review to catch these failures.

## 11. Git workflow and authority

Inspect the current branch, staged changes, unstaged changes, untracked files and
relevant history before suggesting or performing Git actions. Distinguish task
changes from existing owner work.

Do not stage, commit, push, merge, rebase, delete a branch, rewrite history or
create a pull request unless the owner explicitly requests that action. Use the
repository's established branch and release workflow. Never use destructive Git
recovery commands without explicit approval.

Do not claim that CI or deployment passed without a real successful run. Treat a
workflow or platform change as implemented only after its configuration and real
execution have been verified.

## 12. Definition of done

A change is complete only when:

- behaviour and documentation match the agreed scope;
- unrelated files and owner changes are preserved;
- relevant validation has run where practical;
- failures and environment limitations are reported honestly;
- schema and technical references are updated when affected;
- public documentation does not overstate completion;
- no secret or sensitive value was introduced;
- the owner receives a concise summary of changed files and checks performed.

Keep the project understandable, demonstrable and interview-ready for New
Zealand junior to early-intermediate software development roles.
