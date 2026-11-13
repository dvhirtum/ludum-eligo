# Authentication

## Google console setup

1. Open the [Google Cloud console](https://console.cloud.google.com/apis/credentials) and select or create a project.
2. Configure the OAuth consent screen (external, with the scopes `openid`, `email`, `profile`).
3. Create an **OAuth client ID** of type **Web application**.
4. Add this **authorized redirect URI**:

   ```
   https://localhost:4200/api/auth/signin-google
   ```

   Port `4200` is the Angular dev server's pinned port in the AppHost. The callback goes through the dev proxy to the API.
5. Store the client id and secret as AppHost parameters (see the [README](../../README.md#secrets)).

## Configuration

| AppHost parameter | API setting | Secret |
|---|---|---|
| `google-client-id` | `Authentication:Google:ClientId` | No |
| `google-client-secret` | `Authentication:Google:ClientSecret` | Yes |

## Endpoints

| Method | Route | Auth | Behaviour |
|---|---|---|---|
| GET | `/api/auth/login?returnUrl=` | Anonymous | Starts the Google challenge. `returnUrl` must be a local path (default `/`), otherwise `400`. |
| GET | `/api/auth/signin-google` | Anonymous | Google callback, handled by the Google handler. |
| GET | `/api/auth/me` | Signed in | `200 { displayName }`, or `401` (also when the user no longer exists). |
| POST | `/api/auth/logout` | Anonymous | Removes the session cookie. `204`. |

Every other `/api` route requires a signed-in user and returns `401` otherwise, including unknown routes. The health endpoints `/api/health` and `/api/alive` are anonymous and are not mapped in production.

## Sign-in flow

1. The SPA navigates the browser to `/api/auth/login?returnUrl=/collection`.
2. The API redirects to Google (challenge scheme `Google`).
3. Google redirects back to `/api/auth/signin-google`. In development this goes through the Angular dev proxy. The API trusts the proxy's `X-Forwarded-*` headers, so it builds the redirect URI with the SPA's origin.
4. `OnTicketReceived` runs the `SignInWithGoogle` command. It finds the user by the Google `sub` claim, creating the user on first sign-in, and updates the display name and email.
5. The Google principal is replaced by one holding the internal user id (`NameIdentifier`) and the display name. The cookie handler stores it in the session cookie and redirects to `returnUrl`.
6. If Google returns an error or the user cancels, the API redirects to `/?signInFailed=true`.

## Session cookie

| Setting | Value |
|---|---|
| Name | `__Host-ludumeligo-session` |
| `HttpOnly` | Yes |
| `Secure` | Always |
| `SameSite` | `Lax` |
| Lifetime | 14 days, sliding |

For `/api` requests, the cookie handler answers `401` (not signed in) or `403` instead of redirecting.

## Antiforgery

ASP.NET Core antiforgery in Angular's XSRF mode (`Features/Auth/XsrfProtection.cs`):

- Every GET, HEAD, OPTIONS or TRACE request under `/api` refreshes the `XSRF-TOKEN` cookie. Scripts can read it (`Secure`, `SameSite=Strict`). It runs after authentication, because tokens are bound to the user, and before authorization, so a `401` response still carries a token.
- Every other `/api` request must send the token in the `X-XSRF-TOKEN` header. Otherwise the API returns `400` with `code: xsrf_invalid`. This check runs after authorization.
- Angular's `HttpClient` reads the cookie and sends the header automatically (`withXsrfConfiguration`).

## Current user

`ICurrentUser` (`Common/CurrentUser.cs`) gives slices the internal user id from the `NameIdentifier` claim. It throws when the request has no signed-in user. Every handler scopes its data by this id.

## SPA

| Piece | File | Does |
|---|---|---|
| `AuthService` | `core/auth/auth.service.ts` | Loads `/api/auth/me` at startup; `signIn(returnUrl)`, `signOut()` |
| `signedInGuard` | `core/auth/auth.guard.ts` | Sends anonymous visitors to `/` |
| `signedOutGuard` | `core/auth/auth.guard.ts` | Sends signed-in users from `/` to `/collection` |
| `unauthorizedInterceptor` | `core/auth/unauthorized.interceptor.ts` | On a `401` (except from `/api/auth/me`), marks the user signed out and goes to `/` |

## Tests

The integration test host replaces Google:

- `TestAuthHandler` authenticates requests carrying an `X-Test-User: <user id>` header (`ApiFactory.CreateClientFor`).
- `GET /test/sign-in?userId=` issues a real session cookie (`ApiFactory.CreateSignedInClientAsync`).
- `WithXsrfTokenAsync()` reads the `XSRF-TOKEN` cookie and sends it in the header, as Angular does.
