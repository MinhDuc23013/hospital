// Medical record detail page — Server Component: full findings, diagnosis, lab results.
import { notFound } from "next/navigation";
import Link from "next/link";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { LabResultsTable } from "@/components/medical-records/lab-results-table";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { formatDate } from "@/lib/utils/date-utils";
import type { MedicalRecord } from "@/lib/types";

interface Props {
  params: { id: string };
}

export default async function MedicalRecordDetailPage({ params }: Props) {
  let record: MedicalRecord;
  try {
    record = await callGatewayAPI<MedicalRecord>(`/api/medical-records/record/${params.id}`);
  } catch {
    notFound();
  }

  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader
        title="Medical Record"
        description={`Created on ${formatDate(record.createdAt)}`}
        action={
          <Button asChild variant="outline" size="sm">
            <Link href="/medical-records">Back to list</Link>
          </Button>
        }
      />

      {/* Diagnosis */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Diagnosis</CardTitle>
        </CardHeader>
        <CardContent>
          {record.diagnosis.length === 0 ? (
            <p className="text-sm text-muted-foreground">No diagnosis recorded.</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {record.diagnosis.map((d, i) => (
                <Badge key={i} variant="secondary">{d}</Badge>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Findings */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Clinical Findings</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm whitespace-pre-wrap">
            {record.findings || "No findings recorded."}
          </p>
        </CardContent>
      </Card>

      {/* Lab Results */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Lab Results</CardTitle>
        </CardHeader>
        <CardContent>
          <LabResultsTable results={record.labResults} />
        </CardContent>
      </Card>
    </div>
  );
}
