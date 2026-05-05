import { create } from 'zustand';

export interface UserInfo {
  userId: string;
  email: string;
  fullName: string | null;
  roles: string[];
  expiresAt: number;
}

interface AuthState {
  initialized: boolean;
  authenticated: boolean;
  user: UserInfo | null;
  setInitialized: (v: boolean) => void;
  setAuthenticated: (v: boolean) => void;
  setUser: (user: UserInfo | null) => void;
}

export const useAuthStore = create<AuthState>(set => ({
  initialized: false,
  authenticated: false,
  user: null,
  setInitialized: v => set({ initialized: v }),
  setAuthenticated: v => set({ authenticated: v }),
  setUser: user => set({ user, authenticated: user !== null }),
}));
