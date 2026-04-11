import { create } from 'zustand';

interface AuthState {
  initialized: boolean;
  authenticated: boolean;
  setInitialized: (v: boolean) => void;
  setAuthenticated: (v: boolean) => void;
}

export const useAuthStore = create<AuthState>(set => ({
  initialized: false,
  authenticated: false,
  setInitialized: v => set({ initialized: v }),
  setAuthenticated: v => set({ authenticated: v }),
}));
