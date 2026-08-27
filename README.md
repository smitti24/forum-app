# iiDENTIFii Forum

A self-hosted, moderated discussion forum: an ASP.NET REST API over SQLite, and an Angular web client that consumes that API exclusively over HTTP. The same documented, versioned API serves both the web client and third-party integrators.

Anyone may read. Members post, comment, and like other people's posts — once each, never their own. Moderators flag posts as containing misleading or false information; a flagged post stays fully readable and carries its flag, and the forum records which moderator flagged it and when.

## Running it

You need the **.NET 10 SDK** and **Node 22**. Nothing else — no database server, no container, no external services.

### Backend

```bash
cd backend
dotnet run --project src/Forum.Api
```

It listens on **http://localhost:5032**. On first run it creates `forum.db`, applies migrations, enables write-ahead logging, and seeds demo content. OpenAPI is at `/openapi/v1.json` and a health check at `/health`.

### Frontend

In a second terminal:

```bash
cd frontend
npm install
npm start
```

It serves **http://localhost:4200** and proxies `/api` to the backend, so both halves must be running.

### Tests

```bash
cd backend  && dotnet test     # 116 tests, against real SQLite
cd frontend && npm test        # 20 tests, Vitest
```

### Seeing it work

With both halves running, open **http://localhost:4200**:

1. **Browse anonymously** — the feed lists seven posts. Filter by author, by date range or by flag state, and sort by newest, oldest or most liked. Open a post to read its comments.
2. **Try to like or comment while signed out** — you are told to sign in rather than the action silently failing.
3. **Sign in as `bmokoena`** — like one of `asmith`'s posts. Liking it a second time conflicts and the count stays put; liking your own post is refused outright.
4. **Sign in as `moderator`** — flagging controls appear that no other account sees. Flag a post: it stays readable, keeps accepting comments and likes, and carries its flag everywhere it appears.
5. **The already-flagged post** — *"Liveness checks work fine on a printed photograph"* is flagged out of the box, so the flagged state is visible before you moderate anything yourself.

## Seeded accounts

Seeding runs only in Development, so these credentials cannot reach a real deployment. Every account uses the password **`forum-demo-password`**, and you can sign in with either the username or `<username>@example.com`.

| Username | Role | Why it exists |
|---|---|---|
| `asmith` | Member | Ordinary member with posts and comments |
| `bmokoena` | Member | A second identity, so the like rules can be exercised across two members |
| `moderator` | Moderator | Flags and unflags; also participates as an ordinary member |
| `dubious` | Member | Author of the post that is already flagged |

Seeded content includes seven posts, six comments, likes spread across several members, and **one already-flagged post**, so every moderation state is visible on first login without creating it.

## API

Versioned in the URL, so an integrator can see which contract they are coding against without inspecting headers.

| Method | Path | Auth |
|---|---|---|
| `POST` | `/api/v1/auth/register` | anonymous |
| `POST` | `/api/v1/auth/login` | anonymous |
| `GET` | `/api/v1/auth/me` | member |
| `GET` | `/api/v1/posts` | anonymous |
| `POST` | `/api/v1/posts` | member |
| `GET` | `/api/v1/posts/{id}` | anonymous |
| `GET` | `/api/v1/posts/{id}/comments` | anonymous |
| `POST` | `/api/v1/posts/{id}/comments` | member |
| `POST` | `/api/v1/posts/{id}/like` | member |
| `DELETE` | `/api/v1/posts/{id}/like` | member |
| `POST` | `/api/v1/posts/{id}/flag` | moderator |
| `DELETE` | `/api/v1/posts/{id}/flag` | moderator |

`GET /posts` accepts `from`, `to`, `author` (a username), `flagged`, `sort` (`newest`, `oldest`, `most-liked`) and `page`/`pageSize`, and returns `{ items, page, pageSize, total }`. `pageSize` is clamped to 100 server-side regardless of what is requested.

Business-rule violations carry distinct status codes from validation failures, so a caller can branch on the code rather than parse prose: **400** malformed request, **401** not authenticated, **403** liking your own post or flagging without the moderator role, **404** no such post, **409** already liked or identifier already taken, **429** credential endpoint rate limit.

### Postman

**Published collection:** <!-- TODO: paste the public Postman link here before submitting -->

`postman/iidentifii-forum.postman_collection.json` covers every endpoint and can be imported directly. Run **Login** and the access token is captured into a collection variable that every later request uses. **Create post** captures the new id, so the comment, like and flag requests work without copying anything by hand. The list requests carry disabled query parameters for each filter — enable them to see filtering and sorting, plus a request that demonstrates a rejected sort value.

To exercise moderation, set the `username` collection variable to `moderator` and run **Login** again.

`backend/openapi.json` is the generated OpenAPI document, committed so the contract can be read without running anything. It is regenerated from `/openapi/v1.json` whenever a response shape changes.

## How it is built, and why

### SQLite through EF Core

A forum is relational, overwhelmingly read-heavy and, in this deployment, single-writer. SQLite fits that shape, ships with a first-party .NET provider, and puts no dependency between `git clone` and a running application.

*Rejected:* PostgreSQL in Docker is the right production answer, but it inserts a container between an assessor and a running app. SQL Server LocalDB is Windows-only.

The discipline that makes this defensible rather than lazy is refusing to paint into a corner: no provider-specific SQL and no raw queries anywhere, so moving to PostgreSQL is a provider swap. Write-ahead logging is enabled, because without it SQLite blocks readers for the duration of a write and every browsing request would queue behind whoever is posting.

Timestamps are `DateTime` in UTC, not `DateTimeOffset`. EF persists a `DateTimeOffset` into SQLite as text with its offset appended and then orders by that text, so ordering breaks across offsets — which would break the date-range filter and the date sort this API is required to support. A two-line value converter marks values UTC when they are read back, so they serialise with a `Z` rather than leaving every consumer to guess the zone.

### Minimal APIs over `DbContext` — no mediator, no repository

`DbContext` is already a unit of work, `DbSet<T>` is already a repository, and `SaveChangesAsync` is already an atomic commit. A repository layer on top adds indirection without adding capability, and leaks the moment a projection is needed.

It would also cost correctness here. Efficient paging with no N+1 queries is achieved by projecting straight to response records inside the handler, so a list endpoint materialises no entities and issues one statement. Behind a generic `GetAllAsync()` that is not possible.

*Rejected:* MediatR is now commercially licensed. A hand-rolled equivalent would be roughly 120 lines of reflection-based dispatch to serve a dozen endpoints, and ASP.NET already provides both halves of what it offers — handlers inject through the endpoint delegate, and endpoint filters are the pipeline.

Every endpoint therefore reads top to bottom in one file.

### The business rules live where they can be guaranteed

**One like per member per post is a composite primary key** on `(PostId, MemberId)`. The handler does not check first and then insert — that has a race window under concurrency where two simultaneous requests both pass the check. It attempts the insert and translates the constraint violation into a 409. The database cannot be raced.

The denormalised `LikeCount` is incremented in the *same* `SaveChangesAsync` as the insert, so a rejected duplicate rolls the increment back with it and the count cannot drift from the rows even on the failure path.

**No self-likes is a handler check** returning 403. It is about intent rather than integrity, no constraint can express it, and it deserves a different status code. Database for integrity, handler for intent.

**Uniqueness of emails and usernames is enforced on normalised lowercase columns.** SQLite compares with `=` case-sensitively, so without this "ASmith" and "asmith" would be two different members and neither could reliably log in.

### Authentication

Login returns a signed JWT carrying the member id and role, with a short expiry. The web client holds it in memory only — never `localStorage`, so an XSS cannot lift it out of storage — and third-party integrators send the identical token as a bearer header against the identical endpoints.

Members log in with **either** their email or their username. That is only safe because the two namespaces provably cannot collide: a username may never contain `@`. Usernames are also restricted to letters, digits and `. _ -`, because they appear in URLs and in the author filter, where an unconstrained value containing `/`, `?` or `#` would change the path.

Registration never accepts a role — moderators exist only through seeding — and both credential endpoints are rate limited. Login failures are indistinguishable between an unknown identifier and a wrong password, in message and in timing: an unknown identifier still performs a dummy hash verification, so response time does not reveal whether an account exists.

The role claim decides what the client renders. It is never the authorisation gate: every protected endpoint re-checks server-side.

### Moderation

A post carries its flagged state, the moderator who set it, the time, and an optional note. The brief asks for one moderator capability, and the state plus the actor plus the timestamp is what "for regulatory reasons" actually requires — the forum can answer who flagged this and when.

*Rejected:* an append-only log of moderation actions with a projected state cache. It answers a question nobody asked and creates a second source of truth to keep consistent.

### Paging

Conventional `page`/`pageSize` with a total. Third-party consumption is first-class in this brief, integrators reconcile extracts against totals, and it is one query shape anyone can construct by hand in Postman.

*Rejected:* keyset cursor paging is genuinely better for an infinite feed — stable when a row is inserted mid-scroll, and constant cost at depth where `OFFSET` makes the engine walk every skipped row. It costs an opaque cursor, a seek predicate and a second envelope shape to explain, against a dataset of this size. It is the first thing I would add.

### Testing

116 backend tests run against **real SQLite**, driving real HTTP endpoints, each named as the requirement it defends. The front-end adds 20, covering the auth interceptor, the route guard, contract parsing and the feed's states.

The EF Core in-memory provider is not used anywhere. It enforces neither unique indexes nor referential integrity, so the central like rule — a composite primary key — would pass against it while failing for real. A test that cannot fail for the right reason is worse than no test.

Where a claim is about generated SQL rather than results, the test asserts the SQL: that ordering and range filtering translate rather than falling back to the client, and that list endpoints contain no `COUNT(` because counts are read from denormalised columns.

The **20 front-end tests** are deliberately narrow, covering the three things most likely to break silently: the interceptor attaches the token and does not leak it to a non-API host; the guards redirect by authentication and by role; and the feed renders each of loading, empty, error and populated. One asserts that a response breaking the contract reaches the designed error state rather than a blank screen, which is the behaviour the Zod boundary exists to produce.

Browser end-to-end automation is out of scope; end-to-end verification is a manual pass through the UI and Postman.

### The web client

Angular 21, standalone and **zoneless**, with signals throughout and no `NgModule` anywhere. Code is organised by feature rather than by kind — `features/posts`, `features/auth` — so a capability is one folder rather than a trail through four. `core/` holds infrastructure and never imports from `features/`; `shared/` holds the components the design system names and never reaches into a feature's data layer.

*Rejected:* NgRx, or any store. Almost everything here is **server state**, not client state, and a store would add a second copy of it plus a cache-invalidation problem nobody asked for. The genuine client state is a token, a filter object and a few form drafts — each a signal, owned by the thing that uses it.

*On the version:* Angular 21 rather than 22. `@ng-brutalism/ui` declares a `^21` peer, and Angular 22's CLI requires a newer Node than this machine runs. The library is in fact v22-clean — its partial-Ivy `minVersion` tops out at 17.2.0 and it imports only v22-stable API — so the upgrade is a CLI migration, not a rewrite.

### Server state through the resource API

Reads are `httpResource`, which exposes a request as signals for value, loading and error, re-fetches when its parameters signal changes, and aborts the outstanding request when it does. The feed's filters are one signal; changing a filter *is* the refetch. Writes — like, comment, flag, publish — are plain `HttpClient` calls followed by a reload.

That split is deliberate. A like is a command, not a reactive read, and modelling every mutation as a resource is where signal-based codebases usually go wrong.

### The API contract is checked at runtime

Every response is parsed by a **Zod** schema at the boundary, at the resource's own parse point, before it reaches a signal. TypeScript types are erased at runtime and describe only what the client *hopes* it will receive; the schemas describe what actually arrived.

This was not theoretical. While the two halves were built in parallel the wire contract moved twice — the role's casing, whether registration returned a token, whether an author was a string or an object — and none of it was catchable at build time. With validation at the boundary each change failed loudly on the first call, naming the field, instead of rendering `undefined` three components later. The same schemas validate the forms, so client and server rules cannot drift apart.

### The token lives in memory

The access token is held in a signal in an injectable store, never in `localStorage` and never in a cookie. An XSS anywhere in the client cannot lift it out of storage, and a refresh ends the session — the honest consequence of having no refresh token rather than an oversight.

An interceptor attaches it, and only to requests whose URL starts with the API base, so it cannot leak to a third-party host. A route guard protects authenticated routes and a second protects moderation by role. **The role decides what renders; the server decides what works** — every protected endpoint re-checks, and the client's guard is a courtesy to the user, not a security boundary.

### The interface

Built against `design-system/`, which carries the token spec, a component sheet and a wired demo of every screen. Art direction lives in CSS custom properties overridden once at `:root`; **no component owns a stylesheet**, so the system's one styling rule holds structurally rather than by discipline.

Every list has four designed states — loading, empty, error, populated. Loading is a bordered skeleton shaped like the content it replaces, never a spinner; empty and error each carry an icon tile, a sentence, and the one action that resolves them. The like button owns the one-like rule and renders all four of its states, including the two where it refuses: your own post, and not signed in.

Two things in the design were deliberately not built, because the domain does not have them: post categories and tags (the brief's "tags" is moderator flagging, which the glossary calls a flag, and there is no user-applied taxonomy), and likes on comments (only posts carry likes). Building either would have meant inventing an API to back it.

## Known limitations

Deliberate omissions, not oversights. Each is roughly one slice to add.

- **Content cannot be withdrawn.** A moderator can flag misleading content but not hide genuinely malicious content. This is the most significant gap. The brief asks only for flagging, and hiding would thread an author-visibility branch through every read path.
- **Flag history is not retained.** Unflagging overwrites rather than appending, so the forum evidences the current state and who set it, not the full sequence of moderation decisions.
- **No refresh tokens.** A token cannot be revoked before it expires; its lifetime is the bound and logout is a client-side discard. Rotation with replay detection is the correct production answer and is materially more machinery than "users log in with a password" requires.
- **Offset paging, not keyset.** A post created while someone is paging can shift a page boundary. `COUNT(*)` runs per request over the filtered set.
- **Comments cannot be moderated.** Only posts carry a flag. A malicious comment is a real hole and is named here rather than left to be found.
- **Registration can still be inferred as an enumeration oracle.** Every collision returns one message naming neither identifier, so the response itself reveals nothing, but a determined caller can infer an email exists by pairing it with a username known to be free. Closing that properly needs mail infrastructure.
- **Rate limiting partitions on the socket address.** Correct when the API is reached directly, as here. Behind a reverse proxy that address is the proxy's, and every client would share one bucket. The fix is forwarded-headers handling with a configured known-proxy list, deliberately not guessed at, because trusting `X-Forwarded-For` without that list is worse than not reading it.
- **The JWT signing key is in `appsettings.json`.** Fine for a proof of concept and self-evidently a development value; it belongs in user-secrets or a key vault before any real deployment.
- **Post bodies are plain text.** No markup is accepted and no raw HTML is ever bound. Supporting Markdown needs a sanitiser, which is a decision rather than a detail.
- **No password reset, 2FA, social login, full-text search, or nested comments.** Out of scope.
- **A refresh signs you out.** The token is deliberately held only in memory, so there is no session to restore. This is the visible cost of the no-refresh-token decision.
- **The front-end has no route-level data resolvers or optimistic updates.** A like round-trips before the count moves. Optimistic mutation with rollback on a 409 is designed in the design system and was not built.
- **No browser end-to-end tests.** Component and unit tests only.
- **No deployment.** No container definitions; the README is the setup contract.

## Layout

```
backend/
  src/Forum.Api/
    Domain/          Member, Post, Comment, Like
    Features/        Auth, Posts, Comments, Likes, Moderation — one folder per capability
    Persistence/     ForumDbContext, migrations, seeder
    Program.cs       composition root
  tests/Forum.Tests/ 116 integration and unit tests
  openapi.json       generated contract
frontend/
  src/app/core/      infrastructure: api client, auth store, guards, interceptor
  src/app/features/  auth, posts, moderation, profile — one folder per capability
  src/app/shared/    the components the design system names
postman/             importable collection
design-system/       design tokens, component spec and screen demos
```

The code carries no explanatory comments by design. The reasoning lives here, where the choices can be argued rather than annotated.
