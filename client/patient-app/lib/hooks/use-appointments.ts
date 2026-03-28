"use client";

// TanStack Query hooks for appointment data — all calls go through /api/proxy.
// Use ONLY when filter/sort changes without page reload, or mutation needs optimistic update.
// For initial page loads, prefer Server Components with callGatewayAPI() directly.
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { Appointment } from "@/lib/types";
import type { ScheduleAppointmentInput, CancelAppointmentInput } from "@/lib/validators";

export const APPOINTMENTS_QUERY_KEY = "appointments";

interface AppointmentFilters {
  status?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

async function fetchAppointments(filters?: AppointmentFilters): Promise<Appointment[]> {
  const params = new URLSearchParams();
  if (filters?.status) params.set("status", filters.status);
  if (filters?.from) params.set("from", filters.from);
  if (filters?.to) params.set("to", filters.to);
  if (filters?.page) params.set("page", String(filters.page));
  if (filters?.pageSize) params.set("pageSize", String(filters.pageSize));

  const query = params.toString() ? `?${params.toString()}` : "";
  const res = await fetch(`/api/proxy/appointments${query}`);
  if (!res.ok) throw new Error(`Failed to fetch appointments: ${res.status}`);
  return res.json();
}

async function fetchAppointment(id: string): Promise<Appointment> {
  const res = await fetch(`/api/proxy/appointments/${id}`);
  if (!res.ok) throw new Error(`Failed to fetch appointment: ${res.status}`);
  return res.json();
}

export function useAppointments(filters?: AppointmentFilters) {
  return useQuery({
    queryKey: [APPOINTMENTS_QUERY_KEY, filters],
    queryFn: () => fetchAppointments(filters),
  });
}

export function useAppointment(id: string) {
  return useQuery({
    queryKey: [APPOINTMENTS_QUERY_KEY, id],
    queryFn: () => fetchAppointment(id),
    enabled: !!id,
  });
}

export function useScheduleAppointment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: ScheduleAppointmentInput) => {
      const res = await fetch("/api/proxy/appointments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!res.ok) throw new Error(`Failed to schedule appointment: ${res.status}`);
      return res.json() as Promise<Appointment>;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [APPOINTMENTS_QUERY_KEY] });
    },
  });
}

export function useCancelAppointment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ appointmentId, reason }: CancelAppointmentInput) => {
      const res = await fetch(`/api/proxy/appointments/${appointmentId}`, {
        method: "DELETE",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ reason }),
      });
      if (!res.ok) throw new Error(`Failed to cancel appointment: ${res.status}`);
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [APPOINTMENTS_QUERY_KEY] });
    },
  });
}
