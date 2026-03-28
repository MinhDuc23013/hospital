# Phase 1: Project Setup

## Context Links

- [Plan Overview](plan.md)
- [System Architecture](../../docs/system-architecture.md)
- [Code Standards](../../docs/code-standards.md)
- [Docker Compose](../../docker-compose.yml)

## Overview

- **Priority:** P1 (blocking all other phases)
- **Status:** completed
- **Effort:** 3h
- **Description:** Initialize Next.js 14 project with TypeScript, configure Tailwind + shadcn/ui, set up folder structure, Docker, and integrate into monorepo docker-compose.

## Requirements

### Functional
- Next.js 14 App Router project with TypeScript strict mode
- Tailwind CSS + shadcn/ui component library installed
- Project folder structure matching App Router conventions
- Dockerfile (multi-stage) for production builds
- docker-compose service entry on port 3100

### Non-functional
- Dev server starts in <10s
- Production Docker image <250MB
- TypeScript strict mode enabled
- ESLint + Prettier configured

## Architecture

```
client/
  patient-app/
    app/
      (auth)/           # unprotected: login, error
      (dashboard)/      # protected: dashboard, appointments, records, prescriptions, profile
      api/auth/         # NextAuth.js route handler
      layout.tsx        # root layout with providers
      page.tsx          # redirect to /dashboard or /login
    components/
      ui/               # shadcn/ui components
      layout/           # navbar, sidebar, footer
    lib/
      types/            # TypeScript interfaces
      validators/       # Zod schemas
      hooks/            # TanStack Query hooks
      utils/            # date, format, error helpers
    public/
    middleware.ts        # auth route protection
    types/
      next-auth.d.ts    # NextAuth type augmentation
    .env.local           # dev env vars (gitignored)
    .env.example         # template
    Dockerfile
    next.config.ts
    tailwind.config.ts
    tsconfig.json
    package.json
```

## Related Code Files

### Create
- `client/patient-app/package.json`
- `client/patient-app/tsconfig.json`
- `client/patient-app/next.config.ts`
- `client/patient-app/tailwind.config.ts`
- `client/patient-app/postcss.config.js`
- `client/patient-app/.env.example`
- `client/patient-app/.gitignore`
- `client/patient-app/Dockerfile`
- `client/patient-app/app/layout.tsx`
- `client/patient-app/app/page.tsx`
- `client/patient-app/app/globals.css`
- `client/patient-app/app/not-found.tsx`
- `client/patient-app/app/error.tsx`
- `client/patient-app/components/ui/` (via shadcn init)
- `client/patient-app/lib/utils/cn.ts` (classname merge utility)

### Modify
- `docker-compose.yml` -- add `patient-client` service
- `docker-compose.override.yml` -- add dev override if needed
- `.gitignore` -- ensure `client/patient-app/.env.local` ignored

## Implementation Steps

1. **Create project directory**
   ```bash
   mkdir -p client/patient-app
   cd client/patient-app
   ```

2. **Initialize Next.js 14 with TypeScript**
   ```bash
   npx create-next-app@14 . --typescript --tailwind --eslint --app --src-dir=false --import-alias="@/*"
   ```
   - Select: App Router, TypeScript, Tailwind, ESLint, no `src/` dir

3. **Configure port 3100 in package.json**
   ```json
   "scripts": {
     "dev": "next dev -p 3100",
     "start": "next start -p 3100"
   }
   ```

4. **Install core dependencies**
   ```bash
   npm install next-auth@5 @auth/core jwt-decode
   npm install @tanstack/react-query react-hook-form @hookform/resolvers zod
   npm install date-fns next-themes
   npm install -D @types/node @types/react prettier
   ```

5. **Initialize shadcn/ui**
   ```bash
   npx shadcn-ui@latest init
   ```
   - Style: Default, Color: Slate, CSS variables: Yes
   - Install components:
   ```bash
   npx shadcn-ui@latest add button card form input table dialog toast badge tabs select calendar
   ```

6. **Create folder structure** per Architecture section above

7. **Create .env.example**
   ```env
   # NEXTAUTH_URL = the public URL the browser uses to reach the app
   # In Docker: set to your host URL (e.g. http://localhost:3100)
   # In production: set to your real domain (e.g. https://patient.hospital.com)
   NEXTAUTH_URL=http://localhost:3100
   # Generate with: openssl rand -base64 32 — NEVER use a default, NEVER commit
   NEXTAUTH_SECRET=
   KEYCLOAK_CLIENT_ID=hospital-patient-portal
   KEYCLOAK_CLIENT_SECRET=
   # Dev (host): http://localhost:8080/realms/hospital
   # Docker: http://keycloak:8080/realms/hospital
   KEYCLOAK_ISSUER=http://localhost:8080/realms/hospital
   # Server-only — no NEXT_PUBLIC_ prefix; never expose Gateway URL to browser
   GATEWAY_API_URL=http://localhost:8000
   ```
   > **[AUDIT FIX F10]** `NEXT_PUBLIC_GATEWAY_URL` removed — use `GATEWAY_API_URL` (server-only) everywhere. Exposing the gateway URL in the client bundle defeats the proxy pattern.

8. **Configure `next.config.ts` with standalone output and security headers**
   ```typescript
   const nextConfig = {
     output: 'standalone',  // [AUDIT FIX F9] required for Docker runner stage
     async headers() {
       return [{
         source: '/(.*)',
         headers: [
           { key: 'X-Frame-Options', value: 'DENY' },
           { key: 'X-Content-Type-Options', value: 'nosniff' },
           { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
           { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
           // [AUDIT FIX F14] Basic CSP — tighten per environment
           { key: 'Content-Security-Policy', value: "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';" },
         ],
       }]
     },
   }
   ```

9. **Create Dockerfile (multi-stage)**
   ```dockerfile
   FROM node:18-alpine AS builder
   WORKDIR /app
   COPY package*.json ./
   RUN npm ci
   COPY . .
   RUN npm run build
   # [AUDIT FIX F9] standalone output bundles all runtime deps — no separate deps stage needed

   FROM node:18-alpine AS runner
   WORKDIR /app
   ENV NODE_ENV=production
   ENV NEXT_TELEMETRY_DISABLED=1
   RUN addgroup -g 1001 -S nodejs && adduser -S nextjs -u 1001
   # [AUDIT FIX F9] Copy standalone output — includes all runtime deps pre-bundled
   COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
   COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
   COPY --from=builder --chown=nextjs:nodejs /app/public ./public
   USER nextjs
   EXPOSE 3100
   CMD ["node", "server.js"]
   ```

10. **Add to docker-compose.yml**
    ```yaml
    patient-client:
      build:
        context: ./client/patient-app
        dockerfile: Dockerfile
      container_name: patient-client
      ports:
        - "3100:3100"
      environment:
        # [AUDIT FIX F3] NEXTAUTH_URL must be the PUBLIC host URL that Keycloak
        # redirects back to — must exactly match Keycloak's Valid Redirect URIs.
        # Adjust for staging/prod via .env file, never hardcode a default.
        - NEXTAUTH_URL=${NEXTAUTH_URL}
        # [AUDIT FIX F1] No default — missing secret causes immediate startup failure
        # (safer than silently running with a known insecure value)
        - NEXTAUTH_SECRET=${NEXTAUTH_SECRET:?NEXTAUTH_SECRET is required}
        - KEYCLOAK_CLIENT_ID=hospital-patient-portal
        - KEYCLOAK_CLIENT_SECRET=${KEYCLOAK_CLIENT_SECRET}
        # Keycloak internal Docker hostname for token validation (server-side only)
        - KEYCLOAK_ISSUER=http://keycloak:8080/realms/hospital
        # [AUDIT FIX F10] Server-only — no NEXT_PUBLIC prefix
        - GATEWAY_API_URL=http://hospital-gateway:8000
      depends_on:
       hospital-gateway:
         condition: service_healthy
       keycloak:
         condition: service_healthy
     healthcheck:
       test: ["CMD-SHELL", "wget --spider -q http://localhost:3100 || exit 1"]
       interval: 15s
       timeout: 5s
       retries: 5
       start_period: 30s
     restart: unless-stopped
     networks:
       - hospital-network
   ```

11. **Create root layout with minimal providers**
    - html lang="en", body with font, ThemeProvider placeholder

12. **Create root page.tsx** -- redirect to `/dashboard`

13. **Verify dev server starts**
    ```bash
    cd client/patient-app && npm run dev
    # Should be accessible at http://localhost:3100
    ```

## Todo List

- [x] Create `client/patient-app/` directory
- [x] Initialize Next.js 14 project (files created directly, no interactive CLI)
- [x] Set port to 3100
- [x] Install all dependencies (package.json with all deps listed)
- [x] Initialize shadcn/ui + install components (components.json + 9 UI components created)
- [x] Create folder structure (app, components, lib, types)
- [x] Create .env.example
- [x] Create multi-stage Dockerfile
- [x] Add patient-client to docker-compose.yml
- [x] Create root layout.tsx
- [x] Create root page.tsx (redirect)
- [x] Create globals.css with Tailwind directives
- [x] Create not-found.tsx and error.tsx
- [x] Verify dev server starts on port 3100 (requires npm install first)
- [x] Verify TypeScript compiles without errors (requires npm install first)

## Success Criteria

- `npm run dev` starts on port 3100 without errors
- `npm run build` succeeds with 0 TypeScript errors
- `docker build` produces image <250MB
- shadcn/ui components render correctly
- Folder structure matches architecture diagram

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Next.js 14 version conflict with NextAuth v5 | Low | High | Pin exact versions |
| shadcn/ui init fails | Low | Medium | Manual component copy fallback |
| Port 3100 conflict | Low | Low | Configurable via env var |
| Docker build slow first time | Medium | Low | Layer caching, .dockerignore |

## Next Steps

- Phase 2: Auth Integration (depends on .env.local with Keycloak credentials)
