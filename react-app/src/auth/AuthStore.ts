import create from 'zustand';
import AuthClient from './AuthClient';
import { UserDto } from '@/api/AppDtos';

const delay = (ms: number) => new Promise(res => setTimeout(res, ms));
interface AuthState {
  token: string | null;
  refreshToken: string | null;
  user: UserDto | null;
  // Concurrency guards for refresh flows
  isRefreshing: boolean;
  _refreshPromise: Promise<void> | null;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string, name?: string) => Promise<void>;
  signOut: () => Promise<void>;
  changePassword: (newPassword: string) => Promise<void>;
  sendPasswordResetEmail: (email: string, redirectTo: string) => Promise<void>;
  setSession: (token: string | null, refreshToken: string | null, user: UserDto | null) => void;
  refreshSession: () => Promise<void>;
}

const useAuthStore = create<AuthState>((set, get) => ({
  token: localStorage.getItem('auth_token'),
  refreshToken: localStorage.getItem('refresh_token'),
  user: localStorage.getItem('auth_user') ? JSON.parse(localStorage.getItem('auth_user')!) : null,
  isRefreshing: false,
  _refreshPromise: null,

  setSession: (token, refreshToken, user) => {
    set({ token, refreshToken, user });
    if (token) localStorage.setItem('auth_token', token);
    else localStorage.removeItem('auth_token');
    if (refreshToken) localStorage.setItem('refresh_token', refreshToken);
    else localStorage.removeItem('refresh_token');
    if (user) localStorage.setItem('auth_user', JSON.stringify(user));
    else localStorage.removeItem('auth_user');
  },

  async signIn(email, password) {
    const result = await AuthClient.login(email, password);
    if (!result.Success || !result.JwtToken || !result.RefreshToken || !result.User) throw new Error(result.Message || 'Login failed');
    get().setSession(result.JwtToken, result.RefreshToken, result.User);
  },

  async signUp(email, password, name) {
    const result = await AuthClient.signUp(email, password, name);
    if (!result.Success || !result.User) throw new Error(result.Message || 'Sign up failed');
    // No session set on signup. User must explicitly log in after sign up.
  },

  async signOut() {
    await AuthClient.signOut();
    set({ token: null, refreshToken: null, user: null });
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('auth_user');
  },

  async changePassword(newPassword) {
    const result = await AuthClient.changePassword(newPassword);
    if (!result.Success) throw new Error(result.Message || 'Failed to change password');
  },

  async sendPasswordResetEmail(email, redirectTo) {
    const result = await AuthClient.sendPasswordResetEmail(email, redirectTo);
    if (!result.Success) throw new Error(result.Message || 'Failed to send password reset');
  },

  async refreshSession() {
    // Coalesce concurrent refresh attempts into a single in-flight promise
    const existing = get()._refreshPromise;
    if (get().isRefreshing && existing) {
      return existing;
    }

    const currentRefresh = get().refreshToken || localStorage.getItem('refresh_token');
    const currentToken = get().token || localStorage.getItem('auth_token');
    if (!currentRefresh) throw new Error('No refresh token');
    if (!currentToken) throw new Error('No access token');

    const promise = (async () => {
      try {
        set({ isRefreshing: true });
        // Retry with simple backoff to reduce transient-error logouts
        let attempt = 0;
        let lastError: any = null;
        while (attempt < 3) {
          try {
            const result = await AuthClient.refreshToken(currentToken, currentRefresh);
            if (result.Success && result.JwtToken && result.RefreshToken && result.User) {
              set({ token: result.JwtToken, refreshToken: result.RefreshToken, user: result.User });
              localStorage.setItem('auth_token', result.JwtToken);
              localStorage.setItem('refresh_token', result.RefreshToken);
              localStorage.setItem('auth_user', JSON.stringify(result.User));
              return; // success
            } else {
              throw new Error(result.Message || 'Token refresh failed');
            }
          } catch (err) {
            lastError = err;
            attempt++;
            if (attempt < 3) await delay(250 * attempt); // 250ms, 500ms backoff
          }
        }
        {
          set({ token: null, refreshToken: null, user: null });
          localStorage.removeItem('auth_token');
          localStorage.removeItem('refresh_token');
          localStorage.removeItem('auth_user');
          throw lastError || new Error('Token refresh failed');
        }
      } finally {
        set({ isRefreshing: false, _refreshPromise: null });
      }
    })();

    set({ _refreshPromise: promise });
    return promise;
  }
}));

export default useAuthStore;
