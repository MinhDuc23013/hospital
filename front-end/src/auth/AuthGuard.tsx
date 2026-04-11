import { type ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth, type Role } from './useAuth';

interface Props {
  children: ReactNode;
  roles?: Role[];
}

/** Blocks rendering if user doesn't have required role. */
export function AuthGuard({ children, roles }: Props) {
  const { hasRole } = useAuth();

  if (roles && !hasRole(...roles)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}
