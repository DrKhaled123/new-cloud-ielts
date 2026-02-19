import { useEffect, useState, ReactNode } from 'react';
import useAuthStore from './AuthStore';

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
  const refreshSession = useAuthStore((state) => state.refreshSession);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    const bootstrapSession = async () => {
      try {
        // Only attempt refresh once on mount if we actually have tokens in storage
        const access = localStorage.getItem('auth_token');
        const refresh = localStorage.getItem('refresh_token');
        if (access && refresh) await refreshSession();
      } finally {
        if (mounted) setLoading(false);
      }
    };
    bootstrapSession();
    return () => { mounted = false; };
  }, [refreshSession]);

  if (loading) {
    return <div>Loading...</div>;
  }

  return <>{children}</>;
};
