// Navigation link config for doctor sidebar — label, href, and lucide icon name.
export interface NavLink {
  label: string;
  href: string;
  /** Lucide icon name (PascalCase) */
  iconName: string;
}

export const NAV_LINKS: NavLink[] = [
  { label: "Dashboard", href: "/", iconName: "LayoutDashboard" },
  { label: "My Schedule", href: "/schedule", iconName: "Calendar" },
  { label: "Appointments", href: "/appointments", iconName: "ClipboardList" },
  { label: "Patients", href: "/patients", iconName: "Users" },
  { label: "Medical Records", href: "/medical-records", iconName: "FileText" },
  { label: "Prescriptions", href: "/prescriptions", iconName: "Pill" },
];
