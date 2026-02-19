# ServiceInvoker authentication + authorization flow (StarterKit)

This doc describes:

1) the **current/legacy** StarterKit design (combined `IAuthHandler`), and
2) the **redesigned** StarterKit design (separate AuthN + AuthZ, with Supabase JWKS support).

---

## Key terms

- **ServiceInvoker**: a single API endpoint that routes requests to backend manager methods via reflection.
- **AuthN (Authentication)**: establishing *who the caller is* (build `HttpContext.User`).
- **AuthZ (Authorization)**: deciding whether the caller may invoke a specific method.

---

## Legacy flow (combined AuthN + AuthZ)

### Request path

`POST /invoke` → `ServiceInvokerEndpoints` → `ServiceMethodInvoker.InvokeAsync`

### Auth responsibilities

- `ServiceMethodInvoker` calls `IAuthHandler.ParseToken(request)`
  - parses `Authorization: Bearer ...`
  - sets `HttpContext.User`
  - populates `UserContextService`

- `ServiceMethodInvoker` calls `IAuthHandler.ValidateAccess(methodMetadata)`
  - enforces `[RequireAuthenticated]`
  - enforces `[RequireRole(...)]`

### Problems

- AuthN and AuthZ are coupled.
- Cloud auth previously relied on `SupabaseJwtKey` (legacy symmetric secret) instead of JWKS.

---

## Redesigned flow (separate AuthN + AuthZ)

### Design goals

- Keep ServiceInvoker simple.
- Cleanly support **local/dev auth** and **cloud/Supabase JWKS auth**.
- Fetch signing keys from the Supabase **well-known JWKS endpoint** (no OIDC discovery dependency).

### Request path

`POST /invoke` → `ServiceInvokerEndpoints` → `ServiceMethodInvoker.InvokeAsync`

### Components

#### AuthN: `IRequestAuthenticator`

- `AuthenticateAsync(HttpRequest)`
- Establishes `HttpContext.User` if a token is present and valid.
- Populates the app’s user context service (used later for roles/user id).

Implementations:

- **Local**: `LocalJwtRequestAuthenticator`
  - validates tokens signed with the local symmetric key (`LocalJwtOptions.SigningKey`) emitted by `LocalAuthAccessor`.

- **Production**: `SupabaseJwksRequestAuthenticator`
  - validates Supabase access tokens via JWKS.

#### AuthZ: `IMethodAuthorizer`

- `Authorize(MethodMetadata)`
- Enforces method attributes:
  - `[RequireAuthenticated]`
  - `[RequireRole(...)]`

Implementation:

- `AttributeMethodAuthorizer`
  - uses `HttpContext.User` for authenticated check
  - parses roles from `HttpContext.User` claim `app_metadata.roles`

### JWKS retrieval

- JWKS URL is derived from `SupabaseId`:

`https://{SupabaseId}.supabase.co/auth/v1/.well-known/jwks.json`

- Keys are fetched/cached by `SupabaseJwksKeyProvider`.
  - Cache TTL: **15 minutes**.

### Token validation posture

- Issuer:
  - `https://{SupabaseId}.supabase.co/auth/v1`
- Audience:
  - `authenticated` only
- Lifetime validation enabled

---

## DI wiring (App.Api)

- Registers `IRequestAuthenticator` to the environment-appropriate implementation:
  - local → `LocalJwtRequestAuthenticator`
  - prod → `SupabaseJwksRequestAuthenticator`

- Registers shared authorizer:
  - `IMethodAuthorizer` → `AttributeMethodAuthorizer`

---

## Notes

- Invalid/missing tokens do not necessarily throw in `IRequestAuthenticator`; instead, the authorizer will fail on methods that require authentication.
- If JWKS retrieval fails, token validation fails (no hidden fallback). This is intentional for clarity and security.
