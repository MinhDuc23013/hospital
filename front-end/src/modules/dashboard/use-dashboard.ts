import { useQuery } from '@tanstack/react-query';
import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';

interface DashboardStats {
  todayAppointments: number;
  monthRevenue: number;
  weekNewPatients: number;
  todayAvailableSlots: number;
}

export function useDashboard() {
  return useQuery({
    queryKey: ['dashboard'],
    queryFn: () =>
      api.get<DashboardStats>(ENDPOINTS.DASHBOARD).then(r => r.data),
  });
}
