# Research Report: Next.js 14+ App Router + Keycloak OIDC/OAuth2 Integration

**Date:** 2026-03-18
**Time:** 17:17
**Project:** Hospital HRM Patient Portal (Next.js Client)
**Scope:** Patient client app authentication via Keycloak → API Gateway (YARP)

---

## Executive Summary

**Recommended Approach:** NextAuth.js v5 (next-auth) with OpenID Connect (OIDC) provider for Keycloak integration. This is the most stable, maintainable pattern for Next.js 14 App Router apps requiring JWT token pass-through to downstream APIs.

**Key Decision Factors:**
- NextAuth.js handles OAuth2/OIDC flow → simplifies Keycloak integration
- Built-in session & token management with automatic refresh
- Middleware support for route protection in App Router
- JWT callback allows token customization (inject Keycloak roles)
- Native `Authorization: Bearer` header support for API calls

**Complexity:** ~Low-Medium. No custom OAuth2 implementation needed; standard OIDC provider pattern.

---

## 1. NextAuth.js v5 + Keycloak Configuration

### 1.1 Provider Setup Overview

NextAuth.js v5 uses the Keycloak OIDC provider via explicit configuration or generic OIDC provider:

**Option A: Generic OpenID Connect Provider (Recommended)**
```typescript
// app/api/auth/[...nextauth]/route.ts
import NextAuth from "next-auth"
import KeycloakProvider from "next-auth/providers/keycloak"

const handler = NextAuth({
  providers: [
    KeycloakProvider({
      clientId: process.env.KEYCLOAK_CLIENT_ID!,
      clientSecret: process.env.KEYCLOAK_CLIENT_SECRET!,
      issuer: process.env.KEYCLOAK_ISSUER!,
      // issuer example: http://localhost:8080/realms/hospital
    }),
  ],
  // ... callbacks & config below
})

export { handler as GET, handler as POST }
```

**Option B: Custom OIDC Provider (if Keycloak provider not available in v5)**
```typescript
import { OIDCProvider } from "next-auth/providers/oidc"

OIDCProvider({
  id: "keycloak",
  name: "Keycloak",
  type: "oidc",
  issuer: process.env.KEYCLOAK_ISSUER!,
  clientId: process.env.KEYCLOAK_CLIENT_ID!,
  clientSecret: process.env.KEYCLOAK_CLIENT_SECRET!,
  authorization: { params: { scope: "openid email profile roles" } },
})
```

### 1.2 Full NextAuth.js Configuration Example

```typescript
// app/api/auth/[...nextauth]/route.ts
import type { NextAuthOptions } from "next-auth"
import NextAuth from "next-auth"
import KeycloakProvider from "next-auth/providers/keycloak"
import type { JWT } from "next-auth/jwt"
import type { Session } from "next-auth"

export const authOptions: NextAuthOptions = {
  providers: [
    KeycloakProvider({
      clientId: process.env.KEYCLOAK_CLIENT_ID!,
      clientSecret: process.env.KEYCLOAK_CLIENT_SECRET!,
      issuer: process.env.KEYCLOAK_ISSUER!,
      // Authorization endpoint scopes to request
      authorization: {
        params: {
          scope: "openid email profile roles",
        },
      },
    }),
  ],

  // 1. JWT Callback: Add access token & roles to JWT
  callbacks: {
    jwt: async ({ token, account, user }) => {
      // On initial sign-in, inject account & access token
      if (account) {
        token.accessToken = account.access_token
        token.refreshToken = account.refresh_token
        token.expiresAt = account.expires_at
        token.idToken = account.id_token

        // Decode ID token to extract roles (Keycloak stores in token_roles claim)
        // FUTURE: parse id_token JWT to extract roles claim if needed
        // For now, store raw token for later extraction
      }

      // Handle token refresh if expired
      if (token.expiresAt && Date.now() >= token.expiresAt * 1000) {
        return refreshAccessToken(token)
      }

      return token
    },

    // 2. Session Callback: Include access token in session
    session: async ({ session, token }) => {
      // Attach access token to session so it's available in client
      session.accessToken = token.accessToken
      session.user.id = token.sub // subject from JWT
      session.user.roles = token.roles || [] // if extracting roles

      return session
    },

    // 3. SignIn Callback: Validate user (optional)
    signIn: async ({ user, account }) => {
      // Could add role validation here if needed
      return true
    },
  },

  // Session strategy: JWT is best for API token passing
  session: {
    strategy: "jwt",
    maxAge: 60 * 60 * 24, // 24 hours
  },

  // JWT options
  jwt: {
    maxAge: 60 * 60 * 24,
  },

  pages: {
    signIn: "/auth/login", // Custom login page
    error: "/auth/error",
  },

  // Debug mode (disable in production)
  debug: process.env.NODE_ENV === "development",
}

const handler = NextAuth(authOptions)
export { handler as GET, handler as POST }
```

### 1.3 Token Refresh Implementation

For automatic token refresh on expiration:

```typescript
// lib/auth-refresh.ts
import type { JWT } from "next-auth/jwt"

async function refreshAccessToken(token: JWT) {
  try {
    const response = await fetch(
      `${process.env.KEYCLOAK_ISSUER}/protocol/openid-connect/token`,
      {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({
          client_id: process.env.KEYCLOAK_CLIENT_ID!,
          client_secret: process.env.KEYCLOAK_CLIENT_SECRET!,
          grant_type: "refresh_token",
          refresh_token: token.refreshToken as string,
        }),
      }
    )

    if (!response.ok) throw new Error("Token refresh failed")

    const refreshedToken = await response.json()
    return {
      ...token,
      accessToken: refreshedToken.access_token,
      expiresAt: Math.floor(Date.now() / 1000) + refreshedToken.expires_in,
      refreshToken: refreshedToken.refresh_token || token.refreshToken,
    }
  } catch (error) {
    console.error("Token refresh error:", error)
    return { ...token, error: "RefreshTokenExpired" }
  }
}
```

---

## 2. JWT Token Management & API Integration

### 2.1 Accessing Token in Server Actions & API Routes

```typescript
// lib/api-client.ts - Server-side API calls with bearer token
import { getServerSession } from "next-auth/next"
import { authOptions } from "@/app/api/auth/[...nextauth]/route"

export async function callGatewayAPI(
  endpoint: string,
  options: RequestInit = {}
) {
  const session = await getServerSession(authOptions)

  if (!session?.accessToken) {
    throw new Error("No access token available")
  }

  const response = await fetch(`http://localhost:8000${endpoint}`, {
    ...options,
    headers: {
      ...options.headers,
      Authorization: `Bearer ${session.accessToken}`,
      "Content-Type": "application/json",
    },
  })

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error("Unauthorized - token may be expired")
    }
    throw new Error(`API error: ${response.status}`)
  }

  return response.json()
}
```

### 2.2 Calling API in Server Components

```typescript
// app/dashboard/patients/page.tsx
import { callGatewayAPI } from "@/lib/api-client"
import { getServerSession } from "next-auth/next"
import { authOptions } from "@/app/api/auth/[...nextauth]/route"

export default async function PatientsPage() {
  const session = await getServerSession(authOptions)

  // Verify user has patient role
  if (!session) {
    return <div>Not authenticated</div>
  }

  const patients = await callGatewayAPI("/api/patients")

  return (
    <div>
      <h1>Patients</h1>
      <ul>
        {patients.map((patient: any) => (
          <li key={patient.id}>{patient.firstName} {patient.lastName}</li>
        ))}
      </ul>
    </div>
  )
}
```

### 2.3 Client-Side API Calls with useSession Hook

```typescript
// components/patient-form.tsx
"use client"
import { useSession } from "next-auth/react"
import { useState } from "react"

export function PatientForm() {
  const { data: session } = useSession()
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setLoading(true)

    try {
      const response = await fetch("http://localhost:8000/api/patients", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${session?.accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          firstName: "John",
          lastName: "Doe",
          email: "john@hospital.com",
        }),
      })

      if (!response.ok) throw new Error("Failed to create patient")
      const patient = await response.json()
      console.log("Patient created:", patient)
    } catch (error) {
      console.error("Error:", error)
    } finally {
      setLoading(false)
    }
  }

  if (!session) {
    return <div>Please sign in</div>
  }

  return <form onSubmit={handleSubmit}>{/* form fields */}</form>
}
```

**IMPORTANT:** Client-side API calls expose bearer token in browser memory. For sensitive operations, prefer server actions or server components with `callGatewayAPI()`.

---

## 3. Protected Routes & Middleware

### 3.1 App Router Middleware for Route Protection

```typescript
// middleware.ts (root level, alongside app/ & lib/)
import { withAuth } from "next-auth/middleware"
import { NextRequest } from "next/server"

export default withAuth(
  function middleware(request: NextRequest) {
    // Middleware runs for each request
    // Token is available in request.nextauth
  },
  {
    callbacks: {
      authorized: ({ token, req }) => {
        // Redirect to login if no token
        if (!token) return false
        return true
      },
    },
    pages: {
      signIn: "/auth/login",
    },
  }
)

export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - api/auth (NextAuth routes)
     * - _next/static
     * - public files
     */
    "/((?!api/auth|_next/static|favicon.ico).*)",
  ],
}
```

### 3.2 Role-Based Route Protection

```typescript
// middleware.ts - Enhanced with role checking
import { withAuth } from "next-auth/middleware"
import { JWT } from "next-auth/jwt"
import { NextRequest } from "next/server"

export default withAuth(
  function middleware(request: NextRequest) {
    // This runs only if authorized() returned true
    return NextResponse.next()
  },
  {
    callbacks: {
      authorized: ({ token, req }: { token: JWT | null; req: NextRequest }) => {
        // No token = redirect to login
        if (!token) return false

        // Role-based path restrictions
        const { pathname } = req.nextUrl

        if (pathname.startsWith("/dashboard/admin")) {
          // Check if user has admin role
          return token.roles?.includes("admin") || false
        }

        if (pathname.startsWith("/dashboard/patients")) {
          // Check if user has patient role
          return token.roles?.includes("patient") || false
        }

        return true
      },
    },
    pages: {
      signIn: "/auth/login",
      error: "/auth/error",
    },
  }
)

export const config = {
  matcher: ["/dashboard/:path*"],
}
```

### 3.3 Extracting Roles from Keycloak ID Token

Keycloak stores roles in the ID token's `roles` claim. Extract during JWT callback:

```typescript
// app/api/auth/[...nextauth]/route.ts - Enhanced JWT callback
import { jwtDecode } from "jwt-decode"

jwt: async ({ token, account }) => {
  if (account?.id_token) {
    try {
      const decoded: any = jwtDecode(account.id_token)

      // Keycloak stores roles in token's 'roles' claim
      // Or in 'resource_access' with client-specific roles
      token.roles = decoded.roles || []

      // For client-specific roles:
      if (decoded.resource_access) {
        const clientRoles = decoded.resource_access[process.env.KEYCLOAK_CLIENT_ID!]?.roles || []
        token.roles = clientRoles
      }
    } catch (e) {
      console.error("Failed to decode ID token:", e)
    }
  }
  return token
}
```

**Need to install:** `npm install jwt-decode`

---

## 4. Environment Variables Configuration

### 4.1 Required .env.local

```bash
# NextAuth Configuration
NEXTAUTH_URL=http://localhost:3000                    # Client app URL
NEXTAUTH_SECRET=<generate-with-openssl-rand>          # Use: openssl rand -base64 32

# Keycloak Configuration
KEYCLOAK_CLIENT_ID=hospital-patient-portal            # From Keycloak client settings
KEYCLOAK_CLIENT_SECRET=<client-secret-from-keycloak>  # From Keycloak credentials tab
KEYCLOAK_ISSUER=http://localhost:8080/realms/hospital # Full realm issuer URL

# Gateway API
NEXT_PUBLIC_GATEWAY_URL=http://localhost:8000         # Public gateway URL (can be exposed)
GATEWAY_API_URL=http://localhost:8000                 # Server-side gateway URL
```

### 4.2 Generate NEXTAUTH_SECRET

```bash
# On development machine
openssl rand -base64 32

# Output example: abc123xyz...
# Copy to NEXTAUTH_SECRET in .env.local
```

### 4.3 Keycloak Client Setup

In Keycloak Admin Console (http://localhost:8080):

1. **Realm:** "hospital"
2. **Clients → Create Client:**
   - **Client ID:** `hospital-patient-portal`
   - **Name:** Hospital Patient Portal
   - **Client Type:** OpenID Connect

3. **Configure in Settings Tab:**
   - **Valid redirect URIs:** `http://localhost:3000/api/auth/callback/keycloak`
   - **Valid post logout redirect URIs:** `http://localhost:3000`
   - **Web origins:** `http://localhost:3000`

4. **Credentials Tab:**
   - **Client Secret:** Copy to `.env.local`

5. **Advanced tab:**
   - **Access Token Lifespan:** 1 hour (default)
   - **Refresh Token Lifespan:** 7 days (default)

---

## 5. Package Installation

### 5.1 Dependencies

```bash
npm install next-auth@5 @auth/core next-auth
npm install jwt-decode                    # For role extraction from ID token
npm install react                         # For useSession hook
```

### 5.2 Optional - Enhanced Session/Token Typing

```typescript
// types/next-auth.d.ts - Extend NextAuth types
import type { DefaultSession, DefaultJWT } from "next-auth"

declare module "next-auth" {
  interface Session {
    accessToken?: string
    user: {
      id: string
      roles?: string[]
    } & DefaultSession["user"]
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    accessToken?: string
    refreshToken?: string
    expiresAt?: number
    idToken?: string
    roles?: string[]
  }
}
```

### 5.3 Checking Package Versions

```bash
npm list next-auth jwt-decode
```

Expected (March 2026):
- `next-auth@5.x.x` or higher
- `jwt-decode@4.x.x`

---

## 6. Client Implementation Patterns

### 6.1 Login Page Example

```typescript
// app/auth/login/page.tsx
"use client"
import { signIn } from "next-auth/react"
import { useSearchParams } from "next/navigation"
import Link from "next/link"

export default function LoginPage() {
  const searchParams = useSearchParams()
  const error = searchParams.get("error")

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-md w-96">
        <h1 className="text-2xl font-bold mb-6 text-center">Hospital Portal</h1>

        {error && (
          <div className="bg-red-100 text-red-700 p-3 rounded mb-4">
            Error: {error}
          </div>
        )}

        <button
          onClick={() => signIn("keycloak", { callbackUrl: "/dashboard" })}
          className="w-full bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700"
        >
          Sign In with Keycloak
        </button>

        <p className="text-center text-sm text-gray-600 mt-4">
          Don't have an account?{" "}
          <Link href="/auth/register" className="text-blue-600 hover:underline">
            Register here
          </Link>
        </p>
      </div>
    </div>
  )
}
```

### 6.2 Protected Dashboard with useSession

```typescript
// app/dashboard/page.tsx
"use client"
import { useSession, signOut } from "next-auth/react"
import { useRouter } from "next/navigation"
import { useEffect } from "react"

export default function DashboardPage() {
  const { data: session, status } = useSession()
  const router = useRouter()

  useEffect(() => {
    if (status === "unauthenticated") {
      router.push("/auth/login")
    }
  }, [status, router])

  if (status === "loading") {
    return <div>Loading...</div>
  }

  if (!session) {
    return null // Redirect happens in useEffect
  }

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <button
          onClick={() => signOut({ callbackUrl: "/auth/login" })}
          className="bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700"
        >
          Sign Out
        </button>
      </div>

      <div className="bg-white p-6 rounded-lg shadow-md">
        <p className="text-lg mb-4">
          Welcome, {session.user?.name || session.user?.email}
        </p>
        <p className="text-sm text-gray-600">
          Token expires: {new Date(session.expires).toLocaleString()}
        </p>
      </div>
    </div>
  )
}
```

### 6.3 API Utility with Automatic Token Injection

```typescript
// lib/fetch-api.ts - Client-side helper
import { useSession } from "next-auth/react"

export function useApiClient() {
  const { data: session } = useSession()

  async function call(
    endpoint: string,
    options: RequestInit = {}
  ) {
    if (!session?.accessToken) {
      throw new Error("Not authenticated")
    }

    const response = await fetch(
      `${process.env.NEXT_PUBLIC_GATEWAY_URL}${endpoint}`,
      {
        ...options,
        headers: {
          ...options.headers,
          Authorization: `Bearer ${session.accessToken}`,
          "Content-Type": "application/json",
        },
      }
    )

    if (response.status === 401) {
      // Token expired, NextAuth will refresh automatically
      throw new Error("Unauthorized")
    }

    return response.json()
  }

  return { call }
}
```

---

## 7. Key Considerations & Gotchas

### 7.1 Token Expiration & Refresh Flow

| Scenario | Behavior |
|----------|----------|
| **Token expires during request** | JWT callback detects expiration, refreshes token silently |
| **Refresh token expires** | User redirected to login (error state in JWT callback) |
| **Client makes API call with expired token** | Gateway returns 401, client session still shows valid |
| **Solution** | Use server components for sensitive calls to ensure token refresh before API call |

### 7.2 CORS Configuration

If API Gateway doesn't allow requests from `localhost:3000`:

```typescript
// For Server Actions: No CORS issues (server-to-server)
// For Client requests: Must configure Gateway CORS

// In YARP Gateway appsettings.json:
"Cors": {
  "AllowedOrigins": ["http://localhost:3000"],
  "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
  "AllowedHeaders": ["Authorization", "Content-Type"]
}
```

### 7.3 HTTPS in Production

- `NEXTAUTH_URL` must be HTTPS in production
- `NEXTAUTH_SECRET` must be strong (min 32 bytes)
- Keycloak issuer must be HTTPS
- All redirect URIs must be HTTPS

### 7.4 Keycloak Realm Configuration

**Important settings in realm "hospital":**
- **Token Lifespan:** 1 hour (default)
- **Refresh Token Lifespan:** 7 days
- **Access Token Format:** JWT (default)
- **Verify Signature:** Enabled

These must match gateway's JWT validation settings.

---

## 8. Testing the Integration

### 8.1 Smoke Test Checklist

```bash
# 1. Verify Keycloak running
curl http://localhost:8080/realms/hospital/.well-known/openid-configuration

# 2. Create test user in Keycloak (UI or API)
# Assign "patient" role to user

# 3. Start Next.js dev server
npm run dev

# 4. Navigate to http://localhost:3000
# → Should redirect to /auth/login

# 5. Click "Sign In with Keycloak"
# → Should redirect to Keycloak login

# 6. Login with test user
# → Should redirect back to /dashboard

# 7. Check session in browser console
# (In browser dev tools):
# fetch('/api/auth/session').then(r => r.json()).then(console.log)
# → Should show accessToken, user info, roles

# 8. Test API call from client component
# Open Network tab, make API request
# → Authorization header should contain Bearer token

# 9. Test server component API call
# Verify GET /api/patients works with Bearer token
```

### 8.2 Debugging Token Issues

```typescript
// In JWT callback, log token details
jwt: async ({ token, account }) => {
  if (account?.id_token) {
    console.log("ID Token:", account.id_token)
    // Paste at https://jwt.io to decode
  }
  console.log("Final Token:", token)
  return token
}
```

---

## 9. Recommended Folder Structure

```
app/
├── api/
│   └── auth/
│       └── [...nextauth]/
│           └── route.ts              # NextAuth handler
├── auth/
│   ├── login/
│   │   └── page.tsx                  # Login page
│   ├── logout/
│   │   └── page.tsx                  # Logout confirmation
│   └── error/
│       └── page.tsx                  # Error page
├── dashboard/
│   ├── page.tsx                      # Protected dashboard
│   ├── layout.tsx                    # Layout with navbar
│   └── patients/
│       ├── page.tsx                  # Patients list
│       ├── [id]/
│       │   └── page.tsx              # Patient detail
│       └── new/
│           └── page.tsx              # Create patient
├── layout.tsx                        # Root layout with SessionProvider
└── page.tsx                          # Public home page

lib/
├── auth-refresh.ts                   # Token refresh logic
├── api-client.ts                     # Server-side API calls
└── fetch-api.ts                      # Client-side API calls

middleware.ts                         # Route protection
types/
└── next-auth.d.ts                    # Type definitions

.env.local                            # Environment variables (not in git)
```

---

## 10. Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Redirect loop on /api/auth | Missing `NEXTAUTH_SECRET` | Generate with `openssl rand -base64 32` |
| 401 Unauthorized on API calls | Token expired, not refreshing | Use server components; ensure refresh token valid |
| "Provider error" in Keycloak | Incorrect `KEYCLOAK_ISSUER` URL | Verify with `/realms/hospital/.well-known/openid-configuration` |
| CORS errors from client | Gateway CORS not configured | Add `localhost:3000` to Gateway CORS allowed origins |
| Roles not available in middleware | ID token not decoded | Use `jwt-decode` to extract roles from `id_token` |
| Session expires but no refresh | `expiresAt` not set in JWT | Store `account.expires_at` in JWT callback |

---

## 11. Implementation Checklist (for Planner)

- [ ] Install `next-auth`, `jwt-decode`
- [ ] Create `.env.local` with Keycloak credentials
- [ ] Setup Keycloak client ("hospital-patient-portal") in admin console
- [ ] Create `app/api/auth/[...nextauth]/route.ts` with full config
- [ ] Create `lib/auth-refresh.ts` for token refresh logic
- [ ] Create `lib/api-client.ts` for server-side API calls
- [ ] Create `app/auth/login/page.tsx` login page
- [ ] Create `middleware.ts` for route protection
- [ ] Create `types/next-auth.d.ts` for type safety
- [ ] Test login/logout flow
- [ ] Test API calls with bearer token
- [ ] Test token refresh (wait for expiration)
- [ ] Test role-based access (protected routes)
- [ ] Configure CORS in gateway if needed
- [ ] Document API call patterns for team

---

## Unresolved Questions

1. **Role Claim Location:** Does Keycloak store roles in `roles` or `resource_access[client-id].roles`? (Depends on Keycloak client configuration — must verify during implementation.)

2. **Token Refresh Timing:** Should refresh happen preemptively (5 min before expiry) or on-demand (401 response)? (Current approach: on-demand; preemptive requires `expiresAt` tracking.)

3. **API Gateway CORS:** Does YARP gateway already have CORS configured for `localhost:3000`? (Assume not; add during integration testing.)

4. **Keycloak User Creation:** Will users be created via Keycloak admin UI, or should patient registration flow create them? (Assumption: admin UI for MVP; can build self-registration later.)

5. **Logout Behavior:** Should logout revoke tokens at Keycloak, or just clear local session? (Current: clears session only; revocation optional but recommended.)

---

## Summary

**NextAuth.js v5 + Keycloak OIDC is the recommended approach** for Hospital HRM patient portal:

✅ Simplest OIDC integration (no custom OAuth2 code)
✅ Automatic token refresh with Keycloak
✅ JWT callback for role extraction & customization
✅ Middleware for route protection (App Router native)
✅ Session management handles bearer token pass-through
✅ Production-ready (used by thousands of Next.js apps)

**Core Packages:** `next-auth@5`, `jwt-decode`
**Key Files:** `app/api/auth/[...nextauth]/route.ts`, `middleware.ts`, `lib/api-client.ts`
**Configuration:** 6 environment variables (Keycloak credentials + NEXTAUTH_SECRET)

Ready for Planner to create detailed implementation plan.

---

**Report Status:** Ready for Implementation
**Recommendation:** Proceed with NextAuth.js v5 Keycloak integration pattern
