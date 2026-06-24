import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { apiFetch, setToken } from '../api/client';
import type { LoginResponse, UserProfile } from '../types';

interface AuthContextValue {
  user: UserProfile | null;
  loading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  refresh: () => Promise<void>;
  hasFeature: (code: string, write?: boolean) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = async () => {
    try {
      const profile = await apiFetch<UserProfile>('/api/auth/me');
      setUser(profile);
    } catch {
      setUser(null);
      setToken(null);
    }
  };

  useEffect(() => {
    refresh().finally(() => setLoading(false));
  }, []);

  const login = async (username: string, password: string) => {
    const result = await apiFetch<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    setToken(result.token);
    setUser(result.user);
  };

  const logout = () => {
    setToken(null);
    setUser(null);
  };

  const hasFeature = (code: string, write = false) => {
    const f = user?.features.find((x) => x.code === code);
    if (!f) return false;
    return write ? f.canWrite : f.canRead;
  };

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, refresh, hasFeature }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
