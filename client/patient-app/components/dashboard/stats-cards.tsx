// Stats summary cards shown at the top of the dashboard overview.
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CalendarDays, FileText, Pill } from "lucide-react";

interface StatsCardsProps {
  upcomingCount: number;
  totalRecords: number;
  activePrescriptions: number;
}

export function StatsCards({ upcomingCount, totalRecords, activePrescriptions }: StatsCardsProps) {
  const stats = [
    {
      title: "Upcoming Appointments",
      value: upcomingCount,
      icon: <CalendarDays className="h-5 w-5 text-blue-500" />,
    },
    {
      title: "Medical Records",
      value: totalRecords,
      icon: <FileText className="h-5 w-5 text-green-500" />,
    },
    {
      title: "Active Prescriptions",
      value: activePrescriptions,
      icon: <Pill className="h-5 w-5 text-purple-500" />,
    },
  ];

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      {stats.map((stat) => (
        <Card key={stat.title}>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {stat.title}
            </CardTitle>
            {stat.icon}
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{stat.value}</p>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
