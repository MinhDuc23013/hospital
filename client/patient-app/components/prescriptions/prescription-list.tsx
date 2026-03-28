"use client";

// Client Component — status filter tabs + TanStack Query refetch.
import { useState } from "react";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { PrescriptionCard } from "./prescription-card";
import { EmptyState } from "@/components/shared/empty-state";
import { usePrescriptions } from "@/lib/hooks/use-prescriptions";
import { Pill } from "lucide-react";
import type { Prescription, PrescriptionStatus } from "@/lib/types";

type FilterTab = "All" | PrescriptionStatus;
const TABS: FilterTab[] = ["All", "Pending", "Dispensed", "Expired"];

interface PrescriptionListProps {
  initialPrescriptions: Prescription[];
}

export function PrescriptionList({ initialPrescriptions }: PrescriptionListProps) {
  const [activeTab, setActiveTab] = useState<FilterTab>("All");
  const { data: prescriptions } = usePrescriptions();

  const allItems = prescriptions ?? initialPrescriptions;
  const displayList =
    activeTab === "All"
      ? allItems
      : allItems.filter((p) => p.status === activeTab);

  return (
    <div className="space-y-4">
      <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as FilterTab)}>
        <TabsList>
          {TABS.map((tab) => (
            <TabsTrigger key={tab} value={tab}>
              {tab}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {displayList.length === 0 ? (
        <EmptyState
          title="No prescriptions found"
          description={
            activeTab === "All"
              ? "Your prescriptions will appear here."
              : `No ${activeTab.toLowerCase()} prescriptions.`
          }
          icon={<Pill className="h-10 w-10" />}
        />
      ) : (
        <div className="space-y-3">
          {displayList.map((rx) => (
            <PrescriptionCard key={rx.id} prescription={rx} />
          ))}
        </div>
      )}
    </div>
  );
}
