// Dashboard widget: 3 most recent medical records with date + diagnosis preview.
import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/shared/empty-state";
import { formatDate } from "@/lib/utils/date-utils";
import type { MedicalRecord } from "@/lib/types";

interface RecentRecordsProps {
  records: MedicalRecord[];
}

export function RecentRecords({ records }: RecentRecordsProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-base font-semibold">Recent Medical Records</CardTitle>
        <Button asChild variant="ghost" size="sm">
          <Link href="/medical-records">View all</Link>
        </Button>
      </CardHeader>
      <CardContent>
        {records.length === 0 ? (
          <EmptyState
            title="No records yet"
            description="Your medical records will appear here."
            className="border-none py-6"
          />
        ) : (
          <ul className="divide-y">
            {records.map((record) => (
              <li key={record._id} className="py-3">
                <Link
                  href={`/medical-records/${record._id}`}
                  className="group flex items-start justify-between"
                >
                  <div>
                    <p className="text-sm font-medium group-hover:underline">
                      {record.diagnosis.length > 0
                        ? record.diagnosis.slice(0, 2).join(", ")
                        : "No diagnosis listed"}
                    </p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {formatDate(record.createdAt)}
                    </p>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
