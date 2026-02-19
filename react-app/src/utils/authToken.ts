import useAuthStore from "@/auth/AuthStore";

// Helper to decode JWT expiry (returns unix seconds)
export function getJwtExpiration(token: string): number | null {
  try {
    const [, payload] = token.split(".");
    if (!payload) return null;
    const decoded = JSON.parse(atob(payload));
    return decoded.exp; // Expiry unix timestamp (in seconds)
  } catch {
    return null;
  }
}

const TOKEN_EXPIRY_BUFFER_SEC = 60;

/**
 * Returns the up-to-date JWT, auto-refreshing via refreshSession if it is expired/about to expire.
 * If no valid token can be obtained, triggers logout and returns null.
 */
export async function getValidAccessTokenOrRefresh(): Promise<string | null> {
  const { token, refreshToken, refreshSession, signOut } = useAuthStore.getState();
  if (!token || !refreshToken) {
    if (signOut) await signOut();
    return null;
  }
  const exp = getJwtExpiration(token);
  const now = Math.floor(Date.now() / 1000);
  if (!exp || exp > now + TOKEN_EXPIRY_BUFFER_SEC) {
    return token; // Valid, not expiring soon
  }
  try {
    await refreshSession();
    const newToken = useAuthStore.getState().token;
    return newToken || null;
  } catch {
    if (signOut) await signOut();
    return null;
  }
}
