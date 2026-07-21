import { Outlet, NavLink } from 'react-router-dom';
import {
  HomeOutlined,
  CalendarOutlined,
  FileTextOutlined,
  MedicineBoxOutlined,
  CreditCardOutlined,
  TeamOutlined,
  UserOutlined,
  SafetyOutlined,
  LogoutOutlined,
  WalletOutlined,
  FolderOutlined,
} from '@ant-design/icons';
import type { ComponentType } from 'react';
import { useAuth } from '../auth/useAuth';

interface NavItem {
  to: string;
  label: string;
  Icon: ComponentType<{ className?: string }>;
  roles: readonly string[];
}

const navItems: readonly NavItem[] = [
  { to: '/dashboard',       label: 'Dashboard',         Icon: HomeOutlined,        roles: [] },
  { to: '/appointments',    label: 'Appointments',      Icon: CalendarOutlined,    roles: ['admin', 'doctor', 'receptionist', 'patient'] },
  { to: '/medical-records', label: 'Medical Records',   Icon: FileTextOutlined,    roles: ['admin', 'doctor', 'nurse', 'patient'] },
  { to: '/pharmacy',        label: 'E-Prescriptions',   Icon: MedicineBoxOutlined, roles: ['admin', 'pharmacist', 'doctor', 'patient'] },
  { to: '/billing',         label: 'Billing',           Icon: CreditCardOutlined,  roles: ['admin', 'receptionist', 'patient'] },
  { to: '/cashier',         label: 'Cashier',           Icon: WalletOutlined,      roles: ['admin', 'receptionist'] },
  { to: '/patients',        label: 'Patients',          Icon: TeamOutlined,        roles: ['admin', 'doctor', 'receptionist', 'nurse'] },
  { to: '/doctors',         label: 'Doctors',           Icon: UserOutlined,        roles: ['admin', 'receptionist'] },
  { to: '/files',           label: 'Files',             Icon: FolderOutlined,      roles: ['admin', 'doctor', 'receptionist', 'nurse'] },
  { to: '/admin/users',     label: 'User Management',   Icon: SafetyOutlined,      roles: ['admin'] },
];

export function DashboardLayout() {
  const { hasRole, fullName, email, logout } = useAuth();

  return (
    <div className="flex min-h-screen">
      {/* Sidebar */}
      <aside className="w-60 bg-emerald-900 text-emerald-50 flex flex-col">
        <div className="px-5 py-5 text-lg font-bold border-b border-emerald-800 flex items-center gap-2">
          <div className="w-8 h-8 rounded-lg bg-emerald-600 flex items-center justify-center text-white">
            <MedicineBoxOutlined />
          </div>
          <span>Hospital HRM</span>
        </div>
        <nav className="flex-1 p-3 space-y-1">
          {navItems.map(item => {
            if (item.roles.length > 0 && !hasRole(...(item.roles as any))) return null;
            const Icon = item.Icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2 rounded text-sm transition ${
                    isActive
                      ? 'bg-emerald-600 text-white'
                      : 'text-emerald-100 hover:bg-emerald-800'
                  }`
                }
              >
                <Icon className="text-base" />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>
      </aside>

      {/* Main area */}
      <div className="flex-1 flex flex-col bg-emerald-50/30 overflow-hidden">
        <header className="h-14 bg-white border-b border-emerald-100 flex items-center justify-end px-6 gap-4">
          <div className="text-right">
            <div className="text-sm font-medium text-gray-800 leading-tight">
              {fullName || 'User'}
            </div>
            <div className="text-xs text-gray-500 leading-tight">{email}</div>
          </div>
          <div className="w-9 h-9 rounded-full bg-emerald-600 text-white flex items-center justify-center text-sm font-semibold">
            {(fullName || email || '?').charAt(0).toUpperCase()}
          </div>
          <button
            onClick={logout}
            className="px-3 py-1.5 text-sm text-red-600 hover:bg-red-50 rounded transition-colors flex items-center gap-1.5"
          >
            <LogoutOutlined /> Logout
          </button>
        </header>

        <main className="flex-1 p-6 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
