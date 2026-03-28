# Phase 2: Auth Integration

## Context Links

- [Plan Overview](plan.md)
- [Keycloak Auth Research](../../plans/reports/researcher-260318-1717-nextjs-keycloak-auth.md)
- [Phase 1: Project Setup](phase-01-project-setup.md)

## Overview

- **Priority:** P1 (blocks all protected pages)
- **Status:** pending
- **Effort:** 4h
- **Description:** Integrate NextAuth.js v5 with Keycloak OIDC provider. Implement JWT management, token refresh, role extraction, route protection middleware, and login/logout pages.

## Requirements

### Functional
- Keycloak OIDC login flow (redirect to Keycloak, callback handling)
- JWT token stored in session with access_token for API calls
- Automatic token refresh when expired
- Role extraction from Keycloak ID token (patient role)
- Route protection: unauthenticated users redirect to login
- Login page with "Sign In with Keycloak" button
- Logout clears session and redirects to login
- Error page for auth failures

### Non-functional
- Token refresh transparent to user
- Session persists across page refreshes (JWT strategy)
- No secrets exposed to browser (server-side token handling)
- Type-safe session with custom fields (accessToken, roles)

## Architecture

```
Auth Flow:
1. User visits /dashboard (protected)
2. middleware.ts checks JWT → no token → redirect to /login
3. User clicks "Sign In" → NextAuth redirects to Keycloak
4. Keycloak authenticates → callback to /api/auth/callback/keycloak
5. NextAuth JWT callback: store access_token, refresh_token, roles
6. NextAuth Session callback: expose accessToken & roles to session
7. middleware.ts allows request → page renders with session

Token Refresh:
1. JWT callback checks expiresAt on every request
2. If expired → call Keycloak token endpoint with refresh_token
3. Update JWT with new access_token + expiresAt
4. If refresh fails → set error flag → redirect to login
```

## Related Code Files

### Create
- `client/patient-app/app/api/auth/[...nextauth]/route.ts` -- NextAuth handler
- `client/patient-app/lib/auth-config.ts` -- NextAuth options (provider, callbacks)
- `client/patient-app/lib/auth-refresh.ts` -- token refresh logic
- `client/patient-app/lib/auth-session.ts` -- getServerSession helper
- `client/patient-app/middleware.ts` -- route protection
- `client/patient-app/types/next-auth.d.ts` -- type augmentation
- `client/patient-app/app/(auth)/login/page.tsx` -- login page
- `client/patient-app/app/(auth)/error/page.tsx` -- auth error page
- `client/patient-app/app/(auth)/layout.tsx` -- auth layout (centered, no sidebar)
- `client/patient-app/components/auth/login-form.tsx` -- login button component
- `client/patient-app/components/auth/logout-button.tsx` -- logout button
- `client/patient-app/components/providers/session-provider.tsx` -- NextAuth SessionProvider wrapper

### Modify
- `client/patient-app/app/layout.tsx` -- wrap with SessionProvider

## Implementation Steps

1. **Create NextAuth type augmentation** (`types/next-auth.d.ts`)
   - Extend `Session` with `accessToken: string`, `user.roles: string[]`
   - Extend `JWT` with `accessToken`, `refreshToken`, `expiresAt`, `idToken`, `roles`

2. **Create token refresh utility** (`lib/auth-refresh.ts`)
   - Function `refreshAccessToken(token: JWT)` calls Keycloak token endpoint
   - Uses `grant_type=refresh_token` with client credentials
   - Returns updated token or error state
   > **[AUDIT FIX F4]** Add in-process mutex to prevent concurrent refresh races.
   > Keycloak refresh tokens are single-use; concurrent JWT callbacks (from parallel Server Component fetches) will all fire `refreshAccessToken` simultaneously, invalidating the token after the first call.
   ```typescript
   // Simple per-token refresh lock (server process scope)
   const refreshLocks = new Map<string, Promise<JWT>>()
   export async function refreshAccessTokenSafe(token: JWT): Promise<JWT> {
     const key = token.sub ?? 'default'
     if (!refreshLocks.has(key)) {
       const p = refreshAccessToken(token).finally(() => refreshLocks.delete(key))
       refreshLocks.set(key, p)
     }
     return refreshLocks.get(key)!
   }
   ```

3. **Create NextAuth config** (`lib/auth-config.ts`)
   - KeycloakProvider with env vars (KEYCLOAK_CLIENT_ID, CLIENT_SECRET, ISSUER)
   - Scope: `openid email profile`
   - JWT callback: extract roles via `token.roles as string[]` (top-level claim, NOT `resource_access`) — confirmed in validation <!-- Updated: Validation Session 1 - roles at token.roles[] -->
   > **[AUDIT FIX F7]** Session callback must NOT expose `accessToken` to the client-side session object. Server components and API routes get the token directly from `getServerSession()` → JWT. Only expose non-sensitive fields (user name, email, roles) in session.
   ```typescript
   callbacks: {
     session({ session, token }) {
       session.user.roles = token.roles as string[]
       // accessToken NOT added here — kept server-side in JWT only
       return session
     }
   }
   ```
   - JWT strategy, 24h maxAge
   - Custom pages: signIn → `/login`, error → `/error`

4. **Create NextAuth route handler** (`app/api/auth/[...nextauth]/route.ts`)
   - Import authOptions from lib/auth-config
   - Export GET and POST handlers

5. **Create SessionProvider wrapper** (`components/providers/session-provider.tsx`)
   - Client component wrapping NextAuth's SessionProvider
   - Import in root layout.tsx

6. **Update root layout.tsx**
   - Wrap children with SessionProvider

7. **Create middleware.ts** (root of patient-app)
   > **[AUDIT FIX F6]** NextAuth v5 removed `withAuth`. Use v5 pattern only.
   ```typescript
   // middleware.ts — v5 pattern: export auth directly
   export { auth as middleware } from '@/lib/auth-config'
   export const config = {
     matcher: ['/((?!api/auth|_next/static|_next/image|favicon.ico|login|error).*)'],
   }
   ```
   - No role check in middleware — Keycloak enforces patient role at login; middleware only checks for valid session <!-- Updated: Validation Session 1 - confirmed Keycloak-only enforcement -->

8. **Create login page** (`app/(auth)/login/page.tsx`)
   - Centered card layout
   - Hospital branding (title, logo placeholder)
   - "Sign In with Keycloak" button using `signIn("keycloak")` — no custom `callbackUrl` param
   > **[AUDIT FIX F6-openredirect]** Do NOT pass `callbackUrl` from URL query params directly to `signIn`. NextAuth v5 handles the callback URL internally from the session, preventing open redirect attacks.
   - Error display from searchParams

9. **Create auth error page** (`app/(auth)/error/page.tsx`)
   - Display error message from searchParams
   - Link back to login

10. **Create auth layout** (`app/(auth)/layout.tsx`)
    - Centered, no sidebar, minimal chrome

11. **Create logout button component** (`components/auth/logout-button.tsx`)
    > **[AUDIT FIX F11]** Must revoke the Keycloak session, not just clear the NextAuth cookie.
    ```typescript
    // Call Keycloak end_session_endpoint before NextAuth signOut
    const handleLogout = async () => {
      const session = await getSession()
      const idToken = session?.idToken // store idToken in JWT for this purpose
      const keycloakLogoutUrl = `${process.env.NEXT_PUBLIC_KEYCLOAK_ISSUER}/protocol/openid-connect/logout`
        + `?id_token_hint=${idToken}&post_logout_redirect_uri=${encodeURIComponent(window.location.origin + '/login')}`
      await signOut({ redirect: false })
      window.location.href = keycloakLogoutUrl
    }
    ```
    - Store `idToken` in JWT (not session) during JWT callback for logout use

12. **Create session helper** (`lib/auth-session.ts`)
    - Export `getAuthSession()` wrapping `getServerSession(authOptions)`
    - Used by server components and API layer

13. **Keycloak client setup** (manual step, document in .env.example)
    - Create client "hospital-patient-portal" in Keycloak admin
    - Valid redirect URI: `http://localhost:3100/api/auth/callback/keycloak`
    - Web origins: `http://localhost:3100`
    - Copy client secret to `.env.local`

14. **Test auth flow**
    - Start dev server, navigate to /dashboard
    - Should redirect to /login
    - Click sign in → Keycloak login page
    - Login → redirect back to /dashboard
    - Check session has accessToken

## Todo List

- [x] Create `types/next-auth.d.ts` type augmentation
- [x] Create `lib/auth-refresh.ts` token refresh
- [x] Create `lib/auth-config.ts` NextAuth options
- [x] Create `app/api/auth/[...nextauth]/route.ts`
- [x] Create `components/providers/session-provider.tsx`
- [x] Update `app/layout.tsx` with SessionProvider
- [x] Create `middleware.ts` route protection
- [x] Create `app/(auth)/layout.tsx`
- [x] Create `app/(auth)/login/page.tsx`
- [x] Create `app/(auth)/error/page.tsx`
- [x] Create `components/auth/login-form.tsx`
- [x] Create `components/auth/logout-button.tsx`
- [x] Create `lib/auth-session.ts` helper
- [x] Configure Keycloak client in admin console
- [x] Test full login/logout flow
- [x] Test token refresh (wait for expiry or shorten lifespan)
- [x] Verify middleware blocks unauthenticated access

## Success Criteria

- Login redirects to Keycloak, callback stores JWT
- Session contains accessToken and user roles
- Protected routes redirect to /login without valid session
- Token refresh works transparently
- Logout clears session and redirects
- TypeScript compiles with no errors

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Keycloak issuer URL differs in Docker vs host | Medium | High | Use env vars; different values for dev vs docker |
| NextAuth v5 breaking changes | Medium | High | Pin exact version, follow migration guide |
| Role claim in unexpected location | Medium | Medium | Log decoded token during dev, adapt extraction |
| CORS blocks callback | Low | High | Configure Keycloak redirect URIs correctly |

## Security Considerations

- NEXTAUTH_SECRET must be strong (32+ bytes, generated via `openssl rand -base64 32`)
- Client secret never exposed to browser (server-only env var)
- HttpOnly cookies for session (NextAuth default)
- Token refresh uses server-side only (no browser exposure)
- Rate limit login attempts (Keycloak handles this)

## Next Steps

- Phase 3: API Layer (uses auth session for Bearer token injection)
