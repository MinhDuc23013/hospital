"use client";

// TanStack Query hooks for patient profile — all calls go through /api/proxy.
// patientId comes from NextAuth session (session.user.id = Keycloak sub = Patient Service UUID).
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSession } from "next-auth/react";
import type { Patient } from "@/lib/types";
import type { UpdatePatientInput } from "@/lib/validators";

export const PATIENT_PROFILE_QUERY_KEY = "patient-profile";

async function fetchPatientProfile(patientId: string): Promise<Patient> {
  const res = await fetch(`/api/proxy/patients/${patientId}`);
  if (!res.ok) throw new Error(`Failed to fetch patient profile: ${res.status}`);
  return res.json();
}

export function usePatientProfile() {
  const { data: session } = useSession();
  const patientId = session?.user?.id ?? "";

  return useQuery({
    queryKey: [PATIENT_PROFILE_QUERY_KEY, patientId],
    queryFn: () => fetchPatientProfile(patientId),
    enabled: !!patientId,
  });
}

export function useUpdateProfile() {
  const { data: session } = useSession();
  const queryClient = useQueryClient();
  const patientId = session?.user?.id ?? "";

  return useMutation({
    mutationFn: async (data: UpdatePatientInput) => {
      const res = await fetch(`/api/proxy/patients/${patientId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!res.ok) throw new Error(`Failed to update profile: ${res.status}`);
      return res.json() as Promise<Patient>;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [PATIENT_PROFILE_QUERY_KEY, patientId],
      });
    },
  });
}
