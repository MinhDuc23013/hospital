"use client";

// TanStack Query hook for providers list — used in schedule appointment form dropdown.
// Endpoint confirmed in Validation Session 1: GET /api/providers exists.
import { useQuery } from "@tanstack/react-query";
import type { Provider } from "@/lib/types";

export const PROVIDERS_QUERY_KEY = "providers";

async function fetchProviders(): Promise<Provider[]> {
  const res = await fetch("/api/proxy/providers");
  if (!res.ok) throw new Error(`Failed to fetch providers: ${res.status}`);
  return res.json();
}

export function useProviders() {
  return useQuery({
    queryKey: [PROVIDERS_QUERY_KEY],
    queryFn: fetchProviders,
    // Providers list is relatively static — use longer stale time
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}
