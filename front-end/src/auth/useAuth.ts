import keycloak from './keycloak';

export type Role = 'admin' | 'doctor' | 'nurse' | 'pharmacist' | 'receptionist' | 'patient';

export function useAuth() {
  const token = keycloak.token;
  const userInfo = keycloak.tokenParsed;
  const roles: Role[] = (keycloak.tokenParsed?.realm_access?.roles ?? []) as Role[];

  const hasRole = (...required: Role[]) => required.some(r => roles.includes(r));

  const logout = () => keycloak.logout({ redirectUri: window.location.origin });

  return {
    token,
    userInfo,
    roles,
    hasRole,
    logout,
    userId: userInfo?.sub as string | undefined,
    email: userInfo?.email as string | undefined,
    fullName: userInfo?.name as string | undefined,
  };
}
