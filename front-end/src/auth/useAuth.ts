import { useAuthStore } from '../store/authStore';

export type Role = 'admin' | 'doctor' | 'nurse' | 'pharmacist' | 'receptionist' | 'patient';

export function useAuth() {
  const user = useAuthStore(s => s.user);
  const setUser = useAuthStore(s => s.setUser);
  const setAuthenticated = useAuthStore(s => s.setAuthenticated);

  const roles = (user?.roles ?? []) as Role[];
  const hasRole = (...required: Role[]) => required.some(r => roles.includes(r));

  const logout = async () => {
    await fetch('/bff/logout', { method: 'POST', credentials: 'include' });
    setUser(null);
    setAuthenticated(false);
    window.location.href = '/login';
  };

  return {
    user,
    roles,
    hasRole,
    logout,
    userId: user?.userId,
    email: user?.email,
    fullName: user?.fullName,
  };
}
