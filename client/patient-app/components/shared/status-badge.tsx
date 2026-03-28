// Color-coded badge component for appointment/prescription/record statuses.
import { Badge } from "@/components/ui/badge";
import { formatStatus } from "@/lib/utils/format-utils";

type Status =
  | "Scheduled"
  | "InProgress"
  | "Completed"
  | "Cancelled"
  | "Pending"
  | "Dispensed"
  | "Expired"
  | string;

const STATUS_VARIANTS: Record<string, string> = {
  Scheduled: "bg-blue-100 text-blue-800 border-blue-200",
  InProgress: "bg-yellow-100 text-yellow-800 border-yellow-200",
  Completed: "bg-green-100 text-green-800 border-green-200",
  Cancelled: "bg-red-100 text-red-800 border-red-200",
  Pending: "bg-yellow-100 text-yellow-800 border-yellow-200",
  Dispensed: "bg-green-100 text-green-800 border-green-200",
  Expired: "bg-gray-100 text-gray-600 border-gray-200",
};

interface StatusBadgeProps {
  status: Status;
  className?: string;
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const colorClass = STATUS_VARIANTS[status] ?? "bg-gray-100 text-gray-700 border-gray-200";
  return (
    <Badge
      variant="outline"
      className={`${colorClass} font-medium ${className ?? ""}`}
    >
      {formatStatus(status)}
    </Badge>
  );
}
