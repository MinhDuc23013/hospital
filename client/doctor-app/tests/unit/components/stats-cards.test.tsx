import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { StatsCards } from "@/components/dashboard/stats-cards";

describe("StatsCards", () => {
  it("renders today count", () => {
    render(<StatsCards todayCount={5} completedCount={2} pendingPrescriptions={1} />);
    expect(screen.getByText("5")).toBeInTheDocument();
  });

  it("renders completed count", () => {
    render(<StatsCards todayCount={5} completedCount={2} pendingPrescriptions={1} />);
    expect(screen.getByText("2")).toBeInTheDocument();
  });

  it("renders pending prescriptions", () => {
    render(<StatsCards todayCount={5} completedCount={2} pendingPrescriptions={3} />);
    expect(screen.getByText("3")).toBeInTheDocument();
  });
});
