# Phase 01 — Bootstrap & Project Setup

**Status:** Complete
**Priority:** Critical (blocks all other phases)
**Effort:** Medium

## Context Links

- Reference: `client/patient-app/package.json`, `client/patient-app/tailwind.config.ts`
- Code standards: `docs/code-standards.md`

## Overview

Scaffold `client/doctor-app/` as a Next.js 14 app using identical toolchain as patient-app.
Configure Keycloak OIDC auth (NextAuth v5), Tailwind CSS, shadcn/ui component system.

## Requirements

- Next.js 14 (App Router), TypeScript strict mode
- NextAuth v5 with Keycloak provider (same realm `hospital`, new client `doctor-app`)
- Tailwind CSS + shadcn/ui (copy config from patient-app)
- React Hook Form + Zod for forms
- TanStack Query v5 for client-side data fetching
- Vitest for unit tests

## Architecture

```
client/doctor-app/
├── app/
│   ├── (auth)/
│   │   ├── layout.tsx
│   │   ├── login/page.tsx
│   │   └── error/page.tsx
│   ├── (dashboard)/          # protected routes
│   │   └── layout.tsx        # auth guard + sidebar/topbar
│   ├── api/
│   │   ├── auth/[...nextauth]/route.ts
│   │   └── proxy/[...path]/route.ts
│   ├── layout.tsx
│   └── globals.css
├── components/
│   ├── ui/                   # shadcn/ui primitives
│   ├── layout/               # sidebar, topbar
│   ├── auth/
│   ├── shared/
│   └── providers/
├── lib/
│   ├── api-client.ts
│   ├── auth-config.ts
│   ├── auth-session.ts
│   ├── auth-refresh.ts
│   ├── types/
│   ├── hooks/
│   ├── utils/
│   └── validators/
├── middleware.ts
├── Dockerfile
├── .env.example
├── package.json
├── tsconfig.json
├── next.config.ts
├── tailwind.config.ts
├── postcss.config.js
└── components.json
```

## Related Code Files

**Create:**
- `client/doctor-app/package.json`
- `client/doctor-app/tsconfig.json`
- `client/doctor-app/next.config.ts`
- `client/doctor-app/tailwind.config.ts`
- `client/doctor-app/postcss.config.js`
- `client/doctor-app/components.json`
- `client/doctor-app/.env.example`
- `client/doctor-app/Dockerfile`
- `client/doctor-app/app/layout.tsx`
- `client/doctor-app/app/globals.css`
- `client/doctor-app/middleware.ts`
- `client/doctor-app/lib/auth-config.ts`
- `client/doctor-app/lib/auth-session.ts`
- `client/doctor-app/lib/auth-refresh.ts`
- `client/doctor-app/lib/types/next-auth.d.ts`
- `client/doctor-app/app/api/auth/[...nextauth]/route.ts`
- `client/doctor-app/app/(auth)/layout.tsx`
- `client/doctor-app/app/(auth)/login/page.tsx`
- `client/doctor-app/app/(auth)/error/page.tsx`
- `client/doctor-app/components/providers/session-provider.tsx`
- `client/doctor-app/components/providers/query-client-provider.tsx`
- `client/doctor-app/components/auth/login-form.tsx`
- `client/doctor-app/components/auth/logout-button.tsx`

## Implementation Steps

1. Create `client/doctor-app/package.json` — copy patient-app deps, rename to `doctor-app`, port `3200`
2. Create `tsconfig.json`, `next.config.ts`, `tailwind.config.ts`, `postcss.config.js` — copy from patient-app
3. Create `components.json` (shadcn config) — copy from patient-app
4. Create `.env.example`:
   ```
   NEXTAUTH_URL=http://localhost:3200
   NEXTAUTH_SECRET=change_me_32_chars_minimum
   KEYCLOAK_CLIENT_ID=doctor-app
   KEYCLOAK_CLIENT_SECRET=
   KEYCLOAK_ISSUER=http://localhost:8080/realms/hospital
   GATEWAY_API_URL=http://localhost:8000
   ```
5. Create `app/globals.css` + `app/layout.tsx` — copy from patient-app (update title to "Doctor Portal")
6. Create `lib/auth-config.ts` — copy from patient-app, change `clientId` to `KEYCLOAK_CLIENT_ID` env var
7. Create `lib/auth-session.ts`, `lib/auth-refresh.ts`, `lib/types/next-auth.d.ts` — copy verbatim from patient-app
8. Create `app/api/auth/[...nextauth]/route.ts` — copy verbatim
9. Create `middleware.ts` — copy verbatim
10. Create `app/(auth)/` pages — copy login/error pages, update title to "Doctor Portal"
11. Create provider components — copy from patient-app
12. Create `Dockerfile` — copy patient-app Dockerfile, update port to 3200
13. Run `npm install` in `client/doctor-app/`

## Todo

- [ ] Create package.json
- [ ] Create tsconfig.json, next.config.ts, tailwind.config.ts, postcss.config.js
- [ ] Create components.json
- [ ] Create .env.example
- [ ] Create app/layout.tsx + globals.css
- [ ] Create lib/auth-config.ts (Keycloak client = `doctor-app`)
- [ ] Create lib/auth-session.ts, lib/auth-refresh.ts, types/next-auth.d.ts
- [ ] Create app/api/auth route
- [ ] Create middleware.ts
- [ ] Create (auth) layout/login/error pages
- [ ] Create provider components
- [ ] Create Dockerfile
- [ ] npm install

## Success Criteria

- `npm run dev` starts on port 3200
- Login page renders at `http://localhost:3200/login`
- Keycloak redirect works (requires Keycloak `doctor-app` client configured)
- `npm run typecheck` passes
- `npm run lint` passes

## Risk Assessment

- Keycloak `doctor-app` client must be manually configured in Keycloak admin (infra concern)
- NextAuth v5 beta — pin exact version from patient-app to avoid breaking changes

## Next Steps

→ Phase 02: Shared Infrastructure (API client, proxy, types, layout components)
