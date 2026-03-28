"use client";

// TanStack Query hooks for prescription data — all calls go through /api/proxy.
import { useQuery } from "@tanstack/react-query";
import type { Prescription } from "@/lib/types";

export const PRESCRIPTIONS_QUERY_KEY = "prescriptions";

async function fetchPrescriptions(): Promise<Prescription[]> {
  // patientId is resolved server-side from the Bearer token in the proxy route
  const res = await fetch("/api/proxy/prescriptions");
  if (!res.ok) throw new Error(`Failed to fetch prescriptions: ${res.status}`);
  return res.json();
}

export function usePrescriptions() {
  return useQuery({
    queryKey: [PRESCRIPTIONS_QUERY_KEY],
    queryFn: fetchPrescriptions,
  });
}
