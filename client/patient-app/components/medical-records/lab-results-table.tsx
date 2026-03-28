// Lab results table using shadcn Table — shows test name, result, normal range, timestamp.
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatDate } from "@/lib/utils/date-utils";
import type { LabResult } from "@/lib/types";

interface LabResultsTableProps {
  results: LabResult[];
}

export function LabResultsTable({ results }: LabResultsTableProps) {
  if (results.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-4 text-center">
        No lab results recorded.
      </p>
    );
  }

  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Test Name</TableHead>
            <TableHead>Result</TableHead>
            <TableHead>Normal Range</TableHead>
            <TableHead>Date</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {results.map((lab, idx) => (
            <TableRow key={idx}>
              <TableCell className="font-medium">{lab.testName}</TableCell>
              <TableCell>{lab.result}</TableCell>
              <TableCell className="text-muted-foreground">{lab.normalRange}</TableCell>
              <TableCell className="text-muted-foreground">{formatDate(lab.timestamp)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
