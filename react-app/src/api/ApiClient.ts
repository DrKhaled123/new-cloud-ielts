import { ServiceInvocationRequestDto } from "./AppDtos";
import { getValidAccessTokenOrRefresh } from "@/utils/authToken";
import useAuthStore from "@/auth/AuthStore";

export interface ApiClientRequestOptions {
  signal?: AbortSignal;
}

const invokeMethod = async <T>(
  serviceName: string,
  managerName: string,
  methodName: string,
  params: any,
  options?: ApiClientRequestOptions
): Promise<T> => {
  const apiUrl = import.meta.env.VITE_API_URL;

  // Auto-refresh JWT if needed 
  const token = await getValidAccessTokenOrRefresh();
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  let request: ServiceInvocationRequestDto = {
    ManagerName: managerName,
    MethodName: methodName,
    Parameters: Array.isArray(params) ? params : [params]
  };

  const doFetch = async (overrideToken?: string) => {
    const hdrs: Record<string, string> = { ...headers };
    if (overrideToken) hdrs['Authorization'] = `Bearer ${overrideToken}`;
    const resp = await fetch(`${apiUrl}/${serviceName}/invoke`, {
      method: 'POST',
      headers: hdrs,
      body: JSON.stringify(request),
      signal: options?.signal
    });
    return resp;
  };

  // First attempt
  let response = await doFetch();

  // If unauthorized, try a single refresh and retry once
  if (response.status === 401) {
    try {
      await useAuthStore.getState().refreshSession();
      const newToken = useAuthStore.getState().token;
      response = await doFetch(newToken ?? undefined);
    } catch (e) {
      // If refresh fails, sign out and surface error
      try { await useAuthStore.getState().signOut(); } catch {}
    }
  }

  if (!response.ok) {
    throw new Error(`Error with status code: ${response.status}`);
  }
  return response.json() as Promise<T>;
};

export default { invokeMethod };
