import { Outlet, NavLink } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', roles: [] },
  { to: '/patients', label: 'Patients', roles: ['admin', 'doctor', 'receptionist', 'nurse'] },
  { to: '/doctors', label: 'Doctors', roles: ['admin', 'receptionist'] },
  { to: '/appointments', label: 'Appointments', roles: ['admin', 'doctor', 'receptionist', 'patient'] },
  { to: '/medical-records', label: 'Medical Records', roles: ['admin', 'doctor', 'nurse'] },
  { to: '/pharmacy', label: 'Pharmacy', roles: ['admin', 'pharmacist', 'doctor'] },
  { to: '/billing', label: 'Billing', roles: ['admin', 'receptionist'] },
  { to: '/admin/users', label: 'Admin: Users', roles: ['admin'] },
] as const;

export function DashboardLayout() {
  const { hasRole, fullName, logout } = useAuth();

  return (
    <div className="flex min-h-screen">
      {/* Sidebar */}
      <aside className="w-56 bg-gray-900 text-white flex flex-col">
        <div className="px-4 py-5 text-lg font-bold border-b border-gray-700">Hospital HRM</div>
        <nav className="flex-1 p-3 space-y-1">
          {navItems.map(item => {
            if (item.roles.length > 0 && !hasRole(...(item.roles as any))) return null;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `block px-3 py-2 rounded text-sm ${isActive ? 'bg-blue-600' : 'hover:bg-gray-700'}`
                }
              >
                {item.label}
              </NavLink>
            );
          })}
        </nav>
        <div className="p-4 border-t border-gray-700 text-sm">
          <div className="text-gray-400 truncate">{fullName}</div>
          <button onClick={logout} className="mt-2 text-red-400 hover:text-red-300">
            Logout
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 bg-gray-50 p-6 overflow-auto">
        <Outlet />
      </main>
    </div>
  );
}
