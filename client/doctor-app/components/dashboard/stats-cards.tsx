"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Calendar, CheckCircle, ClipboardList } from "lucide-react";

interface StatsCardsProps {
  todayCount: number;
  completedCount: number;
  pendingPrescriptions: number;
}

export function StatsCards({ todayCount, completedCount, pendingPrescriptions }: StatsCardsProps) {
  const stats = [
    {
      title: "Today's Appointments",
      value: todayCount,
      icon: Calendar,
      description: "Scheduled for today",
    },
    {
      title: "Completed",
      value: completedCount,
      icon: CheckCircle,
      description: "Completed today",
    },
    {
      title: "Pending Prescriptions",
      value: pendingPrescriptions,
      icon: ClipboardList,
      description: "Awaiting fulfillment",
    },
  ];

  return (
    <div className="grid gap-4 md:grid-cols-3">
      {stats.map((stat) => {
        const Icon = stat.icon;
        return (
          <Card key={stat.title}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">{stat.title}</CardTitle>
              <Icon className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stat.value}</div>
              <p className="text-xs text-muted-foreground">{stat.description}</p>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
