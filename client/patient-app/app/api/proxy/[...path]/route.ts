// API proxy — forwards authenticated client requests to the API Gateway.
// Browser never calls the Gateway directly; all calls go through here.
// [AUDIT FIX F2] Explicit path allowlist prevents IDOR probing of admin/internal routes.
// GATEWAY_API_URL is server-only (no NEXT_PUBLIC_ prefix).
import { NextResponse } from "next/server";
import { getAccessToken } from "@/lib/auth-session";

// Only patient-facing paths are allowed through this proxy.
const ALLOWED_PATHS = [
  /^appointments(\/[^/]+)?(\/cancel)?$/,
  /^medical-records(\/[^/]+)?$/,
  /^prescriptions(\/[^/]+)?$/,
  /^patients\/[^/]+$/,
  /^providers\/?$/,
];

function isAllowedPath(path: string): boolean {
  return ALLOWED_PATHS.some((pattern) => pattern.test(path));
}

async function proxyRequest(
  req: Request,
  params: { path: string[] },
  method: string
): Promise<NextResponse> {
  const path = params.path.join("/");

  if (!isAllowedPath(path)) {
    return NextResponse.json({ error: "Not allowed" }, { status: 403 });
  }

  const accessToken = await getAccessToken();
  if (!accessToken) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  const gatewayBase = process.env.GATEWAY_API_URL;
  if (!gatewayBase) {
    return NextResponse.json({ error: "Gateway not configured" }, { status: 500 });
  }

  // Preserve query string from original request
  const { search } = new URL(req.url);
  const targetUrl = `${gatewayBase}/api/${path}${search}`;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    Authorization: `Bearer ${accessToken}`,
  };

  const init: RequestInit = { method, headers, cache: "no-store" };

  // Forward body for mutating methods
  if (method !== "GET" && method !== "HEAD") {
    init.body = await req.text();
  }

  const upstream = await fetch(targetUrl, init);

  // Handle 204 No Content and non-JSON responses (e.g. DELETE success, upstream errors)
  const contentType = upstream.headers.get("content-type") ?? "";
  if (upstream.status === 204 || !contentType.includes("application/json")) {
    return new NextResponse(null, { status: upstream.status });
  }

  try {
    const data = await upstream.json();
    return NextResponse.json(data, { status: upstream.status });
  } catch {
    // Upstream returned malformed JSON (e.g. HTML error page from gateway)
    return NextResponse.json(
      { error: "Upstream returned invalid response" },
      { status: 502 }
    );
  }
}

// Route segment params type for Next.js 14 catch-all dynamic routes
type RouteContext = { params: { path: string[] } };

export async function GET(req: Request, { params }: RouteContext) {
  return proxyRequest(req, params, "GET");
}

export async function POST(req: Request, { params }: RouteContext) {
  return proxyRequest(req, params, "POST");
}

export async function PUT(req: Request, { params }: RouteContext) {
  return proxyRequest(req, params, "PUT");
}

export async function DELETE(req: Request, { params }: RouteContext) {
  return proxyRequest(req, params, "DELETE");
}
