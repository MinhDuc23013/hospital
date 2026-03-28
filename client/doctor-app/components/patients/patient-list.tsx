"use client";

// Client component — search input + filtered list of patient cards + create action.
import { useState } from "react";
import Link from "next/link";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { PatientCard } from "@/components/patients/patient-card";
import { EmptyState } from "@/components/shared/empty-state";
import type { Patient } from "@/lib/types";

interface PatientListProps {
  initialPatients: Patient[];
}

export function PatientList({ initialPatients }: PatientListProps) {
  const [search, setSearch] = useState("");

  const filtered = search
    ? initialPatients.filter((p) =>
        `${p.firstName} ${p.lastName} ${p.email}`
          .toLowerCase()
          .includes(search.toLowerCase())
      )
    : initialPatients;

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Input
          placeholder="Search patients…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-sm"
        />
        <Button asChild>
          <Link href="/patients/new">New Patient</Link>
        </Button>
      </div>
      {filtered.length === 0 ? (
        <EmptyState
          title="No patients found"
          description={
            search ? "Try a different search term." : "No patients assigned yet."
          }
        />
      ) : (
        <div className="space-y-3">
          {filtered.map((p) => (
            <PatientCard key={p.id} patient={p} />
          ))}
        </div>
      )}
    </div>
  );
}
