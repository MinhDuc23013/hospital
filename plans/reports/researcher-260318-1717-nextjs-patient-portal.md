# Next.js 14 App Router Patient Portal Research Report

**Date:** 2026-03-18
**Status:** Complete
**Context:** Hospital HRM Patient Portal Web Application

---

## Executive Summary

Next.js 14 App Router is ideally suited for building a secure, performant patient portal. Key findings:

- **App Router file-based routing** aligns perfectly with multi-page portal structure (dashboard, appointments, records, prescriptions, profile)
- **React Server Components (RSC)** enable secure server-side data fetching from microservices without exposing credentials
- **Route groups** organize auth-protected pages separately from public routes
- **TanStack Query** handles efficient client-side state & caching for interactive features
- **shadcn/ui + Tailwind** accelerates healthcare-compliant UI with dark mode, accessibility built-in
- **TypeScript + Zod** ensures type-safe API contracts and form validation
- **Multi-stage Docker** compresses Next.js production builds to ~200MB

---

## 1. Project Structure (Next.js 14 App Router)

### Folder Hierarchy

```
app/
├── (auth)/                          # Route group: unprotected auth pages
│   ├── login/
│   │   └── page.tsx
│   ├── register/
│   │   └── page.tsx
│   └── forgot-password/
│       └── page.tsx
├── (dashboard)/                     # Route group: protected patient pages
│   ├── layout.tsx                   # Shared sidebar, nav, auth check
│   ├── page.tsx                     # Dashboard (overview)
│   ├── appointments/
│   │   ├── layout.tsx
│   │   ├── page.tsx                 # List appointments
│   │   ├── [id]/
│   │   │   ├── page.tsx             # Appointment details
│   │   │   └── cancel/
│   │   │       └── page.tsx
│   │   └── schedule/
│   │       └── page.tsx
│   ├── medical-records/
│   │   ├── page.tsx                 # Medical records list
│   │   └── [id]/
│   │       └── page.tsx             # Record details + lab results
│   ├── prescriptions/
│   │   ├── page.tsx                 # Active prescriptions
│   │   └── [id]/
│   │       └── page.tsx
│   └── profile/
│       ├── page.tsx                 # View profile
│       └── edit/
│           └── page.tsx
├── api/                             # API routes for backend proxy
│   ├── auth/
│   │   ├── login/route.ts
│   │   └── logout/route.ts
│   ├── patients/[id]/route.ts       # Proxy to Patient Service
│   ├── appointments/
│   │   ├── route.ts
│   │   └── [id]/route.ts
│   ├── medical-records/
│   │   └── [id]/route.ts
│   ├── prescriptions/
│   │   └── [id]/route.ts
│   └── search/route.ts
├── components/                      # Shared UI components
│   ├── auth/
│   │   ├── login-form.tsx
│   │   └── logout-button.tsx
│   ├── dashboard/
│   │   ├── upcoming-appointments.tsx
│   │   └── recent-records.tsx
│   ├── appointments/
│   │   ├── appointment-card.tsx
│   │   ├── appointment-calendar.tsx
│   │   └── schedule-form.tsx
│   ├── medical-records/
│   │   ├── record-list.tsx
│   │   ├── lab-results-table.tsx
│   │   └── document-viewer.tsx
│   ├── ui/                          # shadcn/ui components
│   │   ├── button.tsx
│   │   ├── card.tsx
│   │   ├── dialog.tsx
│   │   ├── form.tsx
│   │   ├── input.tsx
│   │   ├── table.tsx
│   │   ├── toast.tsx
│   │   └── ...
│   └── layout/
│       ├── navbar.tsx
│       ├── sidebar.tsx
│       └── footer.tsx
├── lib/                             # Utilities & helpers
│   ├── api-client.ts                # Axios instance with auth headers
│   ├── auth.ts                      # JWT/session utilities
│   ├── query-client.ts              # TanStack Query configuration
│   ├── hooks/
│   │   ├── use-auth.ts
│   │   ├── use-appointments.ts      # TanStack Query hooks
│   │   ├── use-medical-records.ts
│   │   ├── use-prescriptions.ts
│   │   └── use-patient-profile.ts
│   ├── validators/                  # Zod schemas
│   │   ├── appointment.schema.ts
│   │   ├── patient.schema.ts
│   │   ├── auth.schema.ts
│   │   └── index.ts
│   ├── types/                       # TypeScript types (from API responses)
│   │   ├── appointment.ts
│   │   ├── patient.ts
│   │   ├── medical-record.ts
│   │   ├── prescription.ts
│   │   └── api-response.ts
│   └── utils/
│       ├── date-utils.ts
│       ├── format-utils.ts
│       └── error-handlers.ts
├── middleware.ts                    # Auth + route protection
├── layout.tsx                       # Root layout (html, body, providers)
├── page.tsx                         # Redirect to /dashboard or /login
├── not-found.tsx                    # 404 page
├── error.tsx                        # Error boundary
└── globals.css                      # Tailwind + global styles

public/
├── icons/
├── images/
└── logos/

.env.local                          # Development env vars
.env.example                        # Template (no secrets)
```

### Key Conventions

- **Route Groups** (`(auth)`, `(dashboard)`): Organize routes without affecting URL paths
- **Dynamic Routes** (`[id]`, `[...slug]`): Handle patient IDs, appointment IDs
- **API Routes** (`app/api/`): Proxy to microservices, handle auth
- **Layout Nesting**: Dashboard layout protects all children with auth check

---

## 2. Data Fetching Patterns

### Pattern 1: Server Components for Protected Data

**When:** Initial page load, secure operations

```typescript
// app/(dashboard)/medical-records/page.tsx
import { getAuthToken } from '@/lib/auth'

async function getMedicalRecords(patientId: string) {
  const token = await getAuthToken()
  const res = await fetch(`http://localhost:5003/api/medical-records/${patientId}`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: 'no-store' // Fresh data on every request
  })
  if (!res.ok) throw new Error('Failed to fetch records')
  return res.json()
}

export default async function MedicalRecordsPage() {
  const records = await getMedicalRecords(userId)

  return (
    <Suspense fallback={<RecordsSkeleton />}>
      <RecordsList records={records} />
    </Suspense>
  )
}
```

**Pros:**
- No credential exposure to browser
- Server-side caching via Next.js fetch
- Smaller initial JS bundle

**Cons:**
- Can't use interactive hooks (React Query, useState)
- Full page refresh needed for updates

---

### Pattern 2: Client Components + TanStack Query for Interactive Features

**When:** Real-time filtering, pagination, refetch on user action

```typescript
// lib/hooks/use-appointments.ts
'use client'
import { useQuery, useMutation } from '@tanstack/react-query'
import { apiClient } from '@/lib/api-client'

export function useAppointments(patientId: string) {
  return useQuery({
    queryKey: ['appointments', patientId],
    queryFn: () => apiClient.get(`/api/appointments?patientId=${patientId}`),
    staleTime: 5 * 60 * 1000, // Cache 5 min
    refetchInterval: 30 * 1000 // Refresh every 30s
  })
}

export function useCancelAppointment() {
  return useMutation({
    mutationFn: (appointmentId: string) =>
      apiClient.delete(`/api/appointments/${appointmentId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] })
    }
  })
}
```

```typescript
// app/(dashboard)/appointments/page.tsx
'use client'
import { useAppointments, useCancelAppointment } from '@/lib/hooks'

export default function AppointmentsPage() {
  const { data, isLoading } = useAppointments(patientId)
  const cancelMutation = useCancelAppointment()

  if (isLoading) return <Skeleton />

  return (
    <div>
      {data?.map(apt => (
        <AppointmentCard
          apt={apt}
          onCancel={() => cancelMutation.mutate(apt.id)}
        />
      ))}
    </div>
  )
}
```

**Pros:**
- Real-time interactivity
- Built-in caching, deduplication, retry logic
- Pessimistic + optimistic updates
- Background refetching

**Cons:**
- Additional JS bundle (~40KB gzipped)
- Requires API route for authentication

---

### Pattern 3: Hybrid (Server Component + Client Component)

**Best Practice:** Default to Server Components, use Client Components for interactive parts

```typescript
// app/(dashboard)/appointments/page.tsx (Server Component)
import { AppointmentList } from './appointment-list' // Client Component

async function getAppointments(patientId: string) {
  // Server-side fetch with auth
  const appointments = await fetch(..., { headers: { Authorization: ... } })
  return appointments.json()
}

export default async function AppointmentsPage() {
  const appointments = await getAppointments(userId)

  return (
    <div>
      <h1>Your Appointments</h1>
      {/* Pass data to Client Component for interactivity */}
      <AppointmentList initialData={appointments} />
    </div>
  )
}
```

```typescript
// app/(dashboard)/appointments/appointment-list.tsx (Client Component)
'use client'
import { useState } from 'react'
import { useAppointments } from '@/lib/hooks'

export function AppointmentList({ initialData }) {
  const [filter, setFilter] = useState('all')
  const { data = initialData } = useAppointments() // Refetch in background

  return (
    <div>
      <FilterButtons onChange={setFilter} />
      {filteredData.map(apt => <AppointmentCard apt={apt} />)}
    </div>
  )
}
```

---

## 3. API Routes as Backend Proxy

### Purpose
- Centralize auth token management
- Rate limiting & validation
- Logging & error handling
- Hide microservice URLs from client

```typescript
// app/api/appointments/route.ts
import { NextRequest } from 'next/server'
import { getAuthToken } from '@/lib/auth'

export async function GET(req: NextRequest) {
  const token = await getAuthToken()
  const patientId = req.nextUrl.searchParams.get('patientId')

  const res = await fetch(
    `http://appointment-service:5002/api/appointments?patientId=${patientId}`,
    { headers: { Authorization: `Bearer ${token}` } }
  )

  if (!res.ok) {
    return Response.json(
      { error: 'Failed to fetch appointments' },
      { status: res.status }
    )
  }

  return Response.json(await res.json())
}

export async function POST(req: NextRequest) {
  const token = await getAuthToken()
  const body = await req.json()

  // Validate
  const result = AppointmentSchema.parse(body)

  const res = await fetch('http://appointment-service:5002/api/appointments', {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(result)
  })

  return Response.json(await res.json(), { status: res.status })
}
```

---

## 4. UI Component Library Recommendation

### Why shadcn/ui + Tailwind CSS

| Feature | shadcn/ui | Material-UI | Bootstrap |
|---------|-----------|------------|-----------|
| **Healthcare Focus** | ✅ Accessible defaults | ⚠️ Clinical feel poor | ❌ Generic |
| **Dark Mode** | ✅ Built-in | ✅ Built-in | ❌ Manual |
| **Tailwind Integration** | ✅ Native | ❌ CSS-in-JS | ❌ Legacy |
| **Bundle Size** | ✅ ~15KB (gzipped) | ❌ ~80KB | ❌ ~50KB |
| **Customization** | ✅ Copy-paste, modify | ⚠️ Theme config | ⚠️ SCSS vars |
| **Healthcare UI Patterns** | ✅ Tables, forms, dialogs | ⚠️ Basic | ❌ Basic |

### Installation

```bash
npm install -D tailwindcss postcss autoprefixer
npx shadcn-ui@latest init

# Install healthcare-relevant components
npx shadcn-ui@latest add button card form input table dialog toast
npx shadcn-ui@latest add calendar date-picker badge badge-variant
npx shadcn-ui@latest add tabs select checkbox textarea
```

### Component Examples

```typescript
// components/appointments/appointment-card.tsx
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

export function AppointmentCard({ appointment }: Props) {
  return (
    <Card>
      <CardHeader>
        <div className="flex justify-between">
          <CardTitle>Dr. {appointment.providerName}</CardTitle>
          <Badge variant={appointment.status === 'scheduled' ? 'default' : 'secondary'}>
            {appointment.status}
          </Badge>
        </div>
        <CardDescription>{appointment.specialization}</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-muted-foreground">
          {new Date(appointment.scheduledTime).toLocaleString()}
        </p>
        <div className="mt-4 flex gap-2">
          <Button variant="default">View Details</Button>
          <Button variant="outline">Reschedule</Button>
          <Button variant="destructive">Cancel</Button>
        </div>
      </CardContent>
    </Card>
  )
}
```

### Dark Mode Support (Built-in)

```typescript
// app/layout.tsx
import { ThemeProvider } from '@/components/theme-provider'

export default function RootLayout({ children }) {
  return (
    <html>
      <body>
        <ThemeProvider attribute="class" defaultTheme="system">
          {children}
        </ThemeProvider>
      </body>
    </html>
  )
}
```

---

## 5. Core Pages & Features

### Page Structure

| Page | Route | Data Pattern | Features |
|------|-------|--------------|----------|
| **Login** | `/login` | Client form | Email/password, OAuth, MFA |
| **Dashboard** | `/dashboard` | Server Component | Upcoming appointments, recent records, stats |
| **Appointments List** | `/dashboard/appointments` | Server + Query | List, filter, search, pagination |
| **Schedule Appointment** | `/dashboard/appointments/schedule` | Client form | Calendar, provider search, confirm |
| **Appointment Details** | `/dashboard/appointments/[id]` | Server Component | Details, notes, reschedule, cancel |
| **Medical Records** | `/dashboard/medical-records` | Server + Query | List, filter by date/type |
| **Record Details** | `/dashboard/medical-records/[id]` | Server Component | Full record, lab results, documents |
| **Prescriptions** | `/dashboard/prescriptions` | Query | Active & past prescriptions, refill |
| **Profile** | `/dashboard/profile` | Server + Client | View/edit demographics, contact |
| **Search** | Global | Query (client) | Search patients, doctors, drugs |

### Example Implementation: Appointments List Page

```typescript
// app/(dashboard)/appointments/page.tsx
import { Suspense } from 'react'
import { AppointmentsList } from './appointments-list'
import { AppointmentListSkeleton } from '@/components/skeletons'

export const metadata = {
  title: 'Your Appointments | Patient Portal'
}

export default async function AppointmentsPage({
  searchParams
}: {
  searchParams: Promise<{ status?: string; month?: string }>
}) {
  const params = await searchParams

  return (
    <div className="container mx-auto p-6">
      <h1 className="text-3xl font-bold mb-6">Your Appointments</h1>

      {/* Interactive filtering via Client Component */}
      <Suspense fallback={<AppointmentListSkeleton />}>
        <AppointmentsList initialFilters={params} />
      </Suspense>
    </div>
  )
}
```

```typescript
// app/(dashboard)/appointments/appointments-list.tsx
'use client'
import { useState } from 'react'
import { useAppointments } from '@/lib/hooks/use-appointments'
import { AppointmentCard } from '@/components/appointments'

export function AppointmentsList({ initialFilters }) {
  const [status, setStatus] = useState(initialFilters.status || 'all')
  const [month, setMonth] = useState(initialFilters.month || '')

  const { data, isLoading } = useAppointments({ status, month })

  return (
    <div>
      {/* Filters */}
      <div className="flex gap-4 mb-6">
        <select value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="all">All Status</option>
          <option value="scheduled">Scheduled</option>
          <option value="completed">Completed</option>
          <option value="cancelled">Cancelled</option>
        </select>
      </div>

      {/* List */}
      {isLoading ? (
        <div>Loading...</div>
      ) : (
        <div className="grid gap-4">
          {data?.map(apt => (
            <AppointmentCard key={apt.id} appointment={apt} />
          ))}
        </div>
      )}
    </div>
  )
}
```

---

## 6. TypeScript Patterns

### API Response Types (from microservices)

```typescript
// lib/types/appointment.ts
export interface Appointment {
  id: string
  patientId: string
  providerId: string
  providerName: string
  specialization: string
  scheduledTime: string // ISO 8601
  duration: string // PT30M format
  status: 'scheduled' | 'completed' | 'cancelled' | 'no-show'
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface AppointmentListResponse {
  data: Appointment[]
  pagination: {
    total: number
    page: number
    pageSize: number
    totalPages: number
  }
}

export type CreateAppointmentRequest = Omit<
  Appointment,
  'id' | 'status' | 'createdAt' | 'updatedAt'
>
```

### Zod Validation Schemas

```typescript
// lib/validators/appointment.schema.ts
import { z } from 'zod'

export const AppointmentFilterSchema = z.object({
  status: z.enum(['all', 'scheduled', 'completed', 'cancelled']).default('all'),
  month: z.string().optional(),
  pageSize: z.number().default(10),
  page: z.number().default(1)
})

export const ScheduleAppointmentSchema = z.object({
  providerId: z.string().uuid('Invalid provider ID'),
  scheduledTime: z.string().datetime('Invalid datetime format'),
  notes: z.string().max(500).optional(),
  duration: z.enum(['PT30M', 'PT60M', 'PT90M']).default('PT30M')
})

export const CancelAppointmentSchema = z.object({
  appointmentId: z.string().uuid(),
  reason: z.string().max(200)
})

// Export types
export type AppointmentFilters = z.infer<typeof AppointmentFilterSchema>
export type ScheduleAppointmentRequest = z.infer<typeof ScheduleAppointmentSchema>
```

### Form Integration (react-hook-form + shadcn)

```typescript
// components/appointments/schedule-form.tsx
'use client'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ScheduleAppointmentSchema } from '@/lib/validators'
import { useScheduleAppointment } from '@/lib/hooks'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage
} from '@/components/ui/form'
import { Button } from '@/components/ui/button'

export function ScheduleAppointmentForm() {
  const form = useForm({
    resolver: zodResolver(ScheduleAppointmentSchema),
    defaultValues: {
      providerId: '',
      scheduledTime: '',
      duration: 'PT30M'
    }
  })

  const scheduleMutation = useScheduleAppointment()

  function onSubmit(data) {
    scheduleMutation.mutate(data, {
      onSuccess: () => {
        form.reset()
        // Navigate or show toast
      }
    })
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        <FormField
          control={form.control}
          name="providerId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Select Provider</FormLabel>
              <FormControl>
                <select {...field}>
                  <option value="">Choose a provider...</option>
                  {/* Options from API */}
                </select>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="scheduledTime"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Appointment Date & Time</FormLabel>
              <FormControl>
                <input type="datetime-local" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" disabled={scheduleMutation.isPending}>
          {scheduleMutation.isPending ? 'Scheduling...' : 'Schedule Appointment'}
        </Button>
      </form>
    </Form>
  )
}
```

---

## 7. Authentication & Middleware

### JWT Management

```typescript
// lib/auth.ts
import { cookies } from 'next/headers'
import { jwtVerify } from 'jose'

const secret = new TextEncoder().encode(process.env.JWT_SECRET!)

export async function getAuthToken() {
  const cookieStore = await cookies()
  return cookieStore.get('authToken')?.value
}

export async function verifyAuth(token: string) {
  try {
    const verified = await jwtVerify(token, secret)
    return verified.payload
  } catch {
    return null
  }
}

export async function setAuthToken(token: string) {
  const cookieStore = await cookies()
  cookieStore.set('authToken', token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'strict',
    maxAge: 7 * 24 * 60 * 60 // 7 days
  })
}
```

### Middleware for Route Protection

```typescript
// middleware.ts
import { NextRequest, NextResponse } from 'next/server'
import { verifyAuth } from '@/lib/auth'

export async function middleware(req: NextRequest) {
  const token = req.cookies.get('authToken')?.value

  // Unprotected routes
  if (req.nextUrl.pathname.startsWith('/(auth)')) {
    if (token) {
      // Redirect logged-in users away from login page
      return NextResponse.redirect(new URL('/dashboard', req.url))
    }
    return NextResponse.next()
  }

  // Protected routes
  if (req.nextUrl.pathname.startsWith('/(dashboard)')) {
    if (!token || !(await verifyAuth(token))) {
      return NextResponse.redirect(new URL('/login', req.url))
    }
  }

  return NextResponse.next()
}

export const config = {
  matcher: ['/((?!api|_next|public).*)']
}
```

---

## 8. Docker Containerization

### Multi-Stage Dockerfile

```dockerfile
# Stage 1: Dependencies (cached layer)
FROM node:18-alpine AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production && npm cache clean --force

# Stage 2: Build
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 3: Runtime (minimal image)
FROM node:18-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Copy runtime dependencies from stage 1
COPY --from=deps /app/node_modules ./node_modules

# Copy built application from stage 2
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/package.json ./

# Non-root user for security
RUN addgroup -g 1001 -S nodejs && adduser -S nextjs -u 1001
USER nextjs

EXPOSE 3000
CMD ["npm", "start"]
```

### Docker Compose Integration

```yaml
version: '3.8'

services:
  patient-portal:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://localhost:8000
      - JWT_SECRET=${JWT_SECRET}
      - KEYCLOAK_URL=http://keycloak:8080
    depends_on:
      - hospital-gateway
    networks:
      - hrm-network
    healthcheck:
      test: [ "CMD", "curl", "-f", "http://localhost:3000" ]
      interval: 30s
      timeout: 10s
      retries: 3

networks:
  hrm-network:
    driver: bridge
```

### Size Optimization

```dockerfile
# In package.json
{
  "scripts": {
    "build": "next build",
    "start": "next start",
    "analyze": "ANALYZE=true next build"  # Bundlesize analysis
  }
}
```

**Expected sizes:**
- Base image: ~170MB
- Dependencies layer: ~450MB (cached after first build)
- Final runtime image: ~200MB
- Production bundle: ~150KB gzipped (without node_modules)

---

## 9. Environment Variables

### `.env.local` (Development)

```env
# API Gateway
NEXT_PUBLIC_API_URL=http://localhost:8000

# Authentication
NEXT_PUBLIC_KEYCLOAK_URL=http://localhost:8080
KEYCLOAK_CLIENT_ID=patient-portal
KEYCLOAK_CLIENT_SECRET=your-secret
JWT_SECRET=your-jwt-secret

# Feature Flags
NEXT_PUBLIC_ENABLE_DARK_MODE=true
NEXT_PUBLIC_ENABLE_NOTIFICATIONS=true

# Analytics (optional)
NEXT_PUBLIC_GA_ID=G-XXXXX
```

### `.env.example` (Template for git)

```env
# Public environment variables (visible in browser)
NEXT_PUBLIC_API_URL=http://gateway:8000
NEXT_PUBLIC_KEYCLOAK_URL=http://keycloak:8080

# Private environment variables (server-only)
KEYCLOAK_CLIENT_SECRET=<insert-secret>
JWT_SECRET=<insert-secret>
```

### Usage in Code

```typescript
// Accessible on server & client
const apiUrl = process.env.NEXT_PUBLIC_API_URL

// Server-only
export async function getAuthToken() {
  const secret = process.env.JWT_SECRET // ✅ Server-only
  // ...
}

// ❌ WRONG: Using private env on client
'use client'
const secret = process.env.JWT_SECRET // Will be undefined
```

---

## 10. Key Dependencies

### Core

```json
{
  "dependencies": {
    "next": "^14.0.0",
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

### UI & Styling

```json
{
  "dependencies": {
    "tailwindcss": "^4.0.0",
    "@tailwindcss/forms": "^0.5.0",
    "@radix-ui/react-*": "^2.0.0",
    "shadcn-ui": "^0.8.0",
    "class-variance-authority": "^0.7.0",
    "clsx": "^2.0.0",
    "tailwind-merge": "^2.0.0"
  }
}
```

### Data Fetching & State

```json
{
  "dependencies": {
    "@tanstack/react-query": "^5.0.0",
    "axios": "^1.6.0"
  }
}
```

### Forms & Validation

```json
{
  "dependencies": {
    "react-hook-form": "^7.48.0",
    "@hookform/resolvers": "^3.3.0",
    "zod": "^3.22.0"
  }
}
```

### Authentication

```json
{
  "dependencies": {
    "jose": "^5.0.0",
    "js-cookie": "^3.0.0"
  }
}
```

### Utilities

```json
{
  "dependencies": {
    "date-fns": "^2.30.0",
    "zustand": "^4.4.0",
    "next-themes": "^0.2.1"
  },
  "devDependencies": {
    "@types/node": "^20.0.0",
    "@types/react": "^18.0.0",
    "typescript": "^5.3.0",
    "eslint": "^8.0.0",
    "prettier": "^3.0.0"
  }
}
```

---

## 11. Performance Optimization

### Image Optimization

```typescript
// components/appointments/provider-image.tsx
import Image from 'next/image'

export function ProviderImage({ src, name }: Props) {
  return (
    <Image
      src={src}
      alt={name}
      width={100}
      height={100}
      className="rounded-full"
      priority={false} // Load on scroll
    />
  )
}
```

### Code Splitting

```typescript
// Lazy load heavy components
import dynamic from 'next/dynamic'

const MedicalRecordViewer = dynamic(
  () => import('@/components/medical-records/viewer'),
  { loading: () => <Skeleton /> }
)
```

### Caching Strategy

```typescript
// Server-side caching (Next.js fetch)
export const revalidate = 300 // Revalidate every 5 min

async function getAppointments() {
  const res = await fetch(apiUrl, {
    cache: 'force-cache', // ISR (Incremental Static Regeneration)
    next: { revalidate: 300 }
  })
}
```

### Bundle Analysis

```bash
npm install -D @next/bundle-analyzer
# In next.config.js
const withBundleAnalyzer = require('@next/bundle-analyzer')({
  enabled: process.env.ANALYZE === 'true'
})

export default withBundleAnalyzer({})

# Run
ANALYZE=true npm run build
```

---

## 12. Architecture Diagram

```
┌─────────────────────────────────────────────┐
│     Browser (Patient Portal Frontend)       │
│        Built with Next.js 14 App Router     │
└──────────┬──────────────────────────────────┘
           │ HTTPS
┌──────────▼──────────────────────────────────┐
│      Next.js Server                         │
│  ├─ Page Rendering (RSC)                    │
│  ├─ API Routes (/api/*)                     │
│  ├─ Middleware (Auth, Rate Limit)           │
│  └─ Data Fetching (fetch with auth)         │
└──────────┬──────────────────────────────────┘
           │ HTTP REST
┌──────────▼──────────────────────────────────┐
│   Hospital API Gateway (Port 8000)          │
│   - JWT Validation                          │
│   - Rate Limiting                           │
│   - Request Routing                         │
└──────────┬──────────────────────────────────┘
           │
    ┌──────┼──────┬──────────┬──────────┐
    │      │      │          │          │
    ▼      ▼      ▼          ▼          ▼
  Patient Appt Medical  Pharmacy   Notification
  Service Service Record Service     Service
           Service
```

---

## 13. Key Implementation Notes

### Server vs Client Components (Decision Tree)

```
Does the component:
├─ Need to fetch data securely? (With auth headers)
│  └─ YES → Use Server Component
├─ Use hooks (useState, useEffect, useContext)?
│  └─ YES → Use Client Component ('use client')
├─ Need real-time updates or user interactions?
│  └─ YES → Use Client Component + React Query
├─ Display sensitive data (PII, medical records)?
│  └─ YES → Fetch on server, pass as props
└─ Just render static UI?
   └─ Either works (prefer Server for smaller JS)
```

### Error Handling Pattern

```typescript
// app/(dashboard)/medical-records/error.tsx
'use client'
export default function Error({ error, reset }: ErrorPageProps) {
  return (
    <div className="p-6">
      <h1>Failed to load medical records</h1>
      <p>{error.message}</p>
      <button onClick={reset}>Try again</button>
    </div>
  )
}
```

### Loading States Pattern

```typescript
// app/(dashboard)/appointments/loading.tsx
export default function Loading() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="h-32 bg-slate-200 rounded animate-pulse" />
      ))}
    </div>
  )
}
```

---

## 14. Security Best Practices

| Practice | Implementation |
|----------|-----------------|
| **HTTPS Only** | Docker container behind reverse proxy |
| **HttpOnly Cookies** | JWT stored in httpOnly cookies (not localStorage) |
| **CSRF Protection** | Middleware + SameSite cookies |
| **XSS Prevention** | React escapes JSX by default; sanitize user input |
| **Rate Limiting** | Implement in API Gateway + Next.js route handlers |
| **PII Protection** | Fetch medical data on server only; don't expose in client |
| **Secrets** | Use env vars, never commit `.env` |
| **CORS** | Handle in Gateway; portal should not call services directly |

---

## 15. Recommended Next Steps

### Phase 1: Setup (Week 1)
- [ ] Initialize Next.js 14 project with TypeScript
- [ ] Configure Tailwind CSS + shadcn/ui
- [ ] Set up project structure (folders, route groups)
- [ ] Create middleware for auth

### Phase 2: Authentication (Week 1-2)
- [ ] Implement login/logout pages
- [ ] JWT token management
- [ ] Protected route middleware
- [ ] Session persistence

### Phase 3: Core Features (Weeks 2-4)
- [ ] Dashboard (appointments overview, records summary)
- [ ] Appointments list + detail + schedule pages
- [ ] Medical records list + detail pages
- [ ] Prescriptions page
- [ ] Profile page

### Phase 4: Polish & Deploy (Week 4-5)
- [ ] Error boundaries & error pages
- [ ] Loading states & skeletons
- [ ] Toast notifications
- [ ] Dark mode theming
- [ ] Docker containerization
- [ ] Staging deployment

---

## Unresolved Questions

1. **Authentication Flow Details:** Will Keycloak be the primary auth provider, or is JWT self-issued? (Assume Keycloak based on README)
2. **Real-time Features:** Should prescriptions or notifications update in real-time (WebSocket), or polling is sufficient?
3. **Mobile Responsiveness:** Is a responsive web UI sufficient, or should a native mobile app be planned?
4. **Accessibility Standards:** What level of WCAG compliance is required (A, AA, AAA)?
5. **Search Feature:** Should global search integrate with Elasticsearch directly, or via Gateway?
6. **Offline Support:** Should patient portal work offline with service workers/sync?
7. **Payment Integration:** Are there prescription refill payments or insurance billing features?
8. **Analytics:** Should patient portal track user behavior (with privacy/HIPAA considerations)?

---

**Report Generated:** 2026-03-18
**Research Duration:** ~2 hours
**Sources:** Next.js 14 official docs, React 19 docs, community best practices
**Recommendations:** Proceed with phased implementation per Phase section; server components as default, TanStack Query for interactivity, shadcn/ui for healthcare-appropriate UI.
