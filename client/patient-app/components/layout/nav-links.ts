// Navigation link config for sidebar — label, href, and lucide icon name.
export interface NavLink {
  label: string;
  href: string;
  /** Lucide icon name (PascalCase) */
  iconName: string;
}

export const NAV_LINKS: NavLink[] = [
  { label: "Dashboard", href: "/", iconName: "LayoutDashboard" },
  { label: "Appointments", href: "/appointments", iconName: "CalendarDays" },
  { label: "Medical Records", href: "/medical-records", iconName: "FileText" },
  { label: "Prescriptions", href: "/prescriptions", iconName: "Pill" },
  { label: "Profile", href: "/profile", iconName: "User" },
];
