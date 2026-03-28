"use client";

// TanStack Query hooks for doctor appointment data — all calls go through /api/proxy.
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { Appointment } from "@/lib/types";

export const APPOINTMENTS_KEY = "doctor-appointments";

interface AppointmentFilters {
  status?: string;
  from?: string;
  to?: string;
  patientId?: string;
}

async function fetchAppointments(filters?: AppointmentFilters): Promise<Appointment[]> {
  const params = new URLSearchParams();
  if (filters?.status) params.set("status", filters.status);
  if (filters?.from) params.set("from", filters.from);
  if (filters?.to) params.set("to", filters.to);
  if (filters?.patientId) params.set("patientId", filters.patientId);
  const query = params.toString() ? `?${params.toString()}` : "";
  const res = await fetch(`/api/proxy/appointments${query}`);
  if (!res.ok) throw new Error(`Failed to fetch appointments: ${res.status}`);
  return res.json();
}

export function useDoctorAppointments(filters?: AppointmentFilters) {
  return useQuery({
    queryKey: [APPOINTMENTS_KEY, filters],
    queryFn: () => fetchAppointments(filters),
  });
}

export function useCompleteAppointment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (appointmentId: string) => {
      const res = await fetch(`/api/proxy/appointments/${appointmentId}/complete`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
      });
      if (!res.ok) throw new Error(`Failed to complete appointment: ${res.status}`);
      return res.status === 204 ? null : res.json();
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [APPOINTMENTS_KEY] }),
  });
}

export function useCancelAppointment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ appointmentId, reason }: { appointmentId: string; reason: string }) => {
      const res = await fetch(`/api/proxy/appointments/${appointmentId}`, {
        method: "DELETE",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ reason }),
      });
      if (!res.ok) throw new Error(`Failed to cancel appointment: ${res.status}`);
      return res.status === 204 ? null : res.json();
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [APPOINTMENTS_KEY] }),
  });
}
