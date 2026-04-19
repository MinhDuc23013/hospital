import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthGuard } from '../auth/AuthGuard';
import { DashboardLayout } from '../layouts/DashboardLayout';
import { useAuthStore } from '../store/authStore';

// Lazy-loaded pages
import { lazy, Suspense } from 'react';

const LandingPage = lazy(() => import('../modules/landing/LandingPage'));
const LoginPage = lazy(() => import('../modules/auth/LoginPage'));
const RegisterPage = lazy(() => import('../modules/auth/RegisterPage'));
const AuthCallbackPage = lazy(() => import('../modules/auth/AuthCallbackPage'));
const DashboardPage = lazy(() => import('../modules/dashboard/DashboardPage'));
const PatientListPage = lazy(() => import('../modules/patient/PatientListPage'));
const PatientRegisterPage = lazy(() => import('../modules/patient/PatientRegisterPage'));
const DoctorListPage = lazy(() => import('../modules/doctor/DoctorListPage'));
const AppointmentListPage = lazy(() => import('../modules/appointment/AppointmentListPage'));
const MedicalRecordsPage = lazy(() => import('../modules/medical-records/MedicalRecordsPage'));
const PharmacyPage = lazy(() => import('../modules/pharmacy/PharmacyPage'));
const BillingPage = lazy(() => import('../modules/billing/BillingPage'));
const CashierPage = lazy(() => import('../modules/billing/CashierPage'));
const AdminUsersPage = lazy(() => import('../modules/admin/AdminUsersPage'));
const DoctorSchedulePage = lazy(() => import('../modules/doctor/DoctorSchedulePage'));
const UnauthorizedPage = lazy(() => import('./UnauthorizedPage'));

function RequireAuth({ children }: { children: React.ReactNode }) {
  const authenticated = useAuthStore(s => s.authenticated);
  if (!authenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export function AppRoutes() {
  return (
    <Suspense fallback={<div className="p-8 text-center">Loading...</div>}>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/auth/callback" element={<AuthCallbackPage />} />
        <Route path="/unauthorized" element={<UnauthorizedPage />} />

        <Route element={<RequireAuth><DashboardLayout /></RequireAuth>}>
          <Route path="/dashboard" element={<DashboardPage />} />

          <Route path="/patients" element={
            <AuthGuard roles={['admin', 'doctor', 'receptionist', 'nurse']}>
              <PatientListPage />
            </AuthGuard>
          } />

          <Route path="/patients/register" element={
            <AuthGuard roles={['admin', 'receptionist']}>
              <PatientRegisterPage />
            </AuthGuard>
          } />

          <Route path="/doctors" element={
            <AuthGuard roles={['admin', 'receptionist']}>
              <DoctorListPage />
            </AuthGuard>
          } />

          <Route path="/doctors/schedules" element={
            <AuthGuard roles={['admin', 'doctor']}>
              <DoctorSchedulePage />
            </AuthGuard>
          } />

          <Route path="/appointments" element={
            <AuthGuard roles={['admin', 'doctor', 'receptionist', 'patient']}>
              <AppointmentListPage />
            </AuthGuard>
          } />

          <Route path="/medical-records" element={
            <AuthGuard roles={['admin', 'doctor', 'nurse']}>
              <MedicalRecordsPage />
            </AuthGuard>
          } />

          <Route path="/pharmacy" element={
            <AuthGuard roles={['admin', 'pharmacist', 'doctor']}>
              <PharmacyPage />
            </AuthGuard>
          } />

          <Route path="/billing" element={
            <AuthGuard roles={['admin', 'receptionist']}>
              <BillingPage />
            </AuthGuard>
          } />

          <Route path="/cashier" element={
            <AuthGuard roles={['admin', 'receptionist']}>
              <CashierPage />
            </AuthGuard>
          } />

          <Route path="/admin/users" element={
            <AuthGuard roles={['admin']}>
              <AdminUsersPage />
            </AuthGuard>
          } />
        </Route>
      </Routes>
    </Suspense>
  );
}
