import { useAuth } from '../../auth/useAuth';
import { useDashboard } from './use-dashboard';
import PatientDashboard from './PatientDashboard';

const formatVND = (amount: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);

interface StatCardProps {
  label: string;
  value: string | number;
  color: string;
}

function StatCard({ label, value, color }: StatCardProps) {
  return (
    <div className={`rounded-lg border p-5 ${color}`}>
      <p className="text-sm text-gray-500 mb-1">{label}</p>
      <p className="text-2xl font-bold text-gray-800">{value}</p>
    </div>
  );
}

export default function DashboardPage() {
  const { fullName, roles, hasRole } = useAuth();

  // Patients see a dedicated dashboard with features oriented to them
  const isPatient = hasRole('patient') && !hasRole('admin', 'doctor', 'receptionist', 'nurse');
  if (isPatient) {
    return <PatientDashboard />;
  }

  // Staff dashboard (admin / doctor / receptionist / nurse)
  const { data, isLoading, isError } = useDashboard();

  return (
    <div>
      <h1 className="text-2xl font-bold mb-1">Dashboard</h1>
      <p className="text-gray-500 mb-1">Welcome, {fullName}</p>
      <p className="text-sm text-gray-400 mb-6">Roles: {roles.join(', ')}</p>

      {isLoading && <p className="text-gray-500">Loading stats...</p>}
      {isError && (
        <div className="p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600 mb-4">
          Failed to load dashboard stats.
        </div>
      )}

      {data && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            label="Today's Appointments"
            value={data.todayAppointments}
            color="bg-blue-50 border-blue-200"
          />
          <StatCard
            label="Month Revenue"
            value={formatVND(data.monthRevenue)}
            color="bg-green-50 border-green-200"
          />
          <StatCard
            label="New Patients This Week"
            value={data.weekNewPatients}
            color="bg-purple-50 border-purple-200"
          />
          <StatCard
            label="Available Slots Today"
            value={data.todayAvailableSlots}
            color="bg-yellow-50 border-yellow-200"
          />
        </div>
      )}
    </div>
  );
}
