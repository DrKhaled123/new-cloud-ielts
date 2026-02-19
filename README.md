# StarterKit — Project Implementation Notes

This `README.md` documents the **project-specific implementation**.

Use this file as a navigational map: what exists in this project, what it does, and how the key flows hang together.

---

## Tech Stack
- Frontend: React + TypeScript + Material UI (MUI)
- Backend: .NET 8 Web API
- Auth / Blob: Supabase
- Document DB: MartenDB (PostgreSQL)

---

## What in the StarterKit is “real plumbing” vs “placeholder”

### Intended to stay (core plumbing)
- **ServiceInvoker routing + security attributes**: `Services/App.ServiceInvoker/*`
  - Core request routing mechanism. Feature work should be implemented by adding/updating **Managers** + **DTOs**, not by modifying ServiceInvoker.
- **Backend bootstrapping/DI**: `Services/App.Api/Program.cs`
  - Registers Managers/Engines/Accessors, config, CORS, database, and maps the ServiceInvoker endpoint.
- **Core infrastructure Accessors** (real implementations):
  - `Services/App.Api/Accessors/DatabaseAccessor.cs` (Marten/Postgres)
  - `Services/App.Api/Accessors/AuthAccessor.cs` (Supabase auth)
  - `Services/App.Api/Accessors/BlobStorageAccessor.cs` (Supabase storage)
- **Frontend app scaffolding**: routing, theme/layout, auth state, and API layer wiring.

### Placeholders / examples (meant to be replaced)
- **Sample domain + engine** (demonstration-only):
  - `Services/Contracts.Domain/Enums/SampleEnum.cs`
  - `Services/Contracts.Domain/Logic/SampleLogicContract.cs`
  - `Services/App.Api/Engines/SampleEngine.cs`
- **Example/placeholder view** (replace with your app’s actual home/dashboard):
  - `react-app/src/views/ExampleView.tsx`

---

## Implementation Index

### Frontend Views
- `react-app/src/views/ExampleView.tsx` — *Example/Placeholder*: minimal landing view. Replace/remove this when you start building real app pages.
- `react-app/src/views/NotFoundView.tsx` — 404 fallback view.

### Frontend Components
- `react-app/src/components/AppView.tsx` — common view wrapper.
- `react-app/src/components/CanvasBackground.tsx` — *Used on example placeholder view*: shared background styling.
- `react-app/src/components/ErrorBoundary.tsx` — global error boundary.

### Frontend Stores
- `react-app/src/auth/AuthStore.ts` — auth/session state (tokens, user, login/logout).

### Managers
- `Services/App.Api/Managers/AuthManager.cs` — auth endpoints.
  - Methods: `Login`, `SignUp`, `SendPasswordResetEmail`, `ChangePassword`, `UpdateUserEmail`, `UpdateUserPassword`, `UpdateUserName`, `RefreshToken`.

### Engines
- `Services/App.Api/Engines/SampleEngine.cs` — *Placeholder*: example stateless engine; replace with real engines.

### Accessors
- `Services/App.Api/Accessors/DatabaseAccessor.cs` — Marten document DB operations.
- `Services/App.Api/Accessors/AuthAccessor.cs` — Supabase auth operations.
- `Services/App.Api/Accessors/BlobStorageAccessor.cs` — Supabase blob/storage operations.
- `Services/App.Api/Accessors/LocalDatabaseAccessor.cs` — local alternative DB implementation, used when the app is running in Development (local) mode
- `Services/App.Api/Accessors/LocalAuthAccessor.cs` — local alternative auth implementation, used when the app is running in Development (local) mode
- `Services/App.Api/Accessors/LocalBlobStorageAccessor.cs` — local alternative blob implementation, used when the app is running in Development (local) mode

---

## Key Flows
- (Document major end-to-end flows at a high level)

## Auth + Roles
- Roles are stored in Supabase **app_metadata** (JWT claim `app_metadata.roles`).
- Backend role checks derive roles from the access token's `app_metadata.roles` claim.
- Role admin updates must use the Supabase **service role** key and write to `app_metadata`.

### ServiceInvoker auth flow (local + cloud)

- ServiceInvoker authenticates and authorizes manager-method calls.
- The StarterKit uses a separated AuthN/AuthZ design and supports Supabase JWKS validation in production.
- See `docs/service-invoker-auth-flow.md`.

## Integrations
- (External services/APIs used and what for)

## Configuration Keys
- (List important config keys and what they affect; do not include secrets)

