export type ApiOptions = RequestInit & { json?: unknown };

const BASE = '/api';

export async function api<T = any>(path: string, opts: ApiOptions = {}): Promise<T> {
  const { json, headers, ...rest } = opts;
  const init: RequestInit = {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(headers || {}),
    },
    ...rest,
    body: json !== undefined ? JSON.stringify(json) : (opts.body as any),
  };
  const res = await fetch(`${BASE}${path}`, init);
  const text = await res.text();
  let data: any;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  if (!res.ok) {
    const msg = (data && (data.message || data.error)) || `HTTP ${res.status}`;
    throw new Error(Array.isArray(msg) ? msg.join(', ') : String(msg));
  }
  return data as T;
}

export const Api = {
  // auth
  me: () => api<{ user: any; company: any }>('/auth/me').catch(() => null as any),
  login: (username: string, password: string) =>
    api('/auth/login', { method: 'POST', json: { username, password } }),
  logout: () => api('/auth/logout', { method: 'POST' }),
  register: (payload: any) => api('/auth/register', { method: 'POST', json: payload }),
  /** dbo.Locations — site names for AspNetUsers.Location (same as legacy MVC user form). */
  registerLocations: () => api<Array<{ id?: number; code: string; name: string }>>('/auth/register-locations'),
  forgot: (email: string) => api('/auth/forgot', { method: 'POST', json: { email } }),
  reset: (payload: any) => api('/auth/reset', { method: 'POST', json: payload }),

  // loads
  loadsList: (q: Record<string, string> = {}) => {
    const search = new URLSearchParams(q).toString();
    return api<any[]>(`/loads${search ? `?${search}` : ''}`);
  },
  loadById: (id: string) => api<any>(`/loads/${id}`),
  loadTypes: () => api<Array<{ id: number; name: string }>>('/loads/types'),
  createLoad: (payload: any) => api('/loads', { method: 'POST', json: payload }),
  updateLoad: (id: string, payload: any) =>
    api(`/loads/${id}`, { method: 'PATCH', json: payload }),
  duplicateLoad: (id: string) => api(`/loads/${id}/duplicate`, { method: 'POST' }),
  setLoadStatus: (id: string, status: string) =>
    api(`/loads/${id}/status`, { method: 'PATCH', json: { status } }),
  assignCarrier: (id: string, carrierUserId: string) =>
    api(`/loads/${id}/assign`, { method: 'PATCH', json: { carrierUserId } }),
  deleteLoad: (id: string) => api(`/loads/${id}`, { method: 'DELETE' }),

  // templates
  templates: () => api<any[]>('/templates'),
  saveTemplate: (payload: any) => api('/templates', { method: 'POST', json: payload }),
  deleteTemplate: (id: string) => api(`/templates/${id}`, { method: 'DELETE' }),

  // claims (Nest: POST /api/claims, GET /api/claims?loadId=…)
  submitClaim: (payload: any) => api('/claims', { method: 'POST', json: payload }),
  myClaims: () => api<any[]>('/claims/mine'),
  claimsForLoad: (loadId: string) =>
    api<any[]>(`/claims?loadId=${encodeURIComponent(loadId)}`),
  acceptClaim: (id: string) => api(`/claims/${id}/accept`, { method: 'PATCH' }),
  rejectClaim: (id: string) => api(`/claims/${id}/reject`, { method: 'PATCH' }),

  // companies / users (admin)
  companies: (q: Record<string, string> = {}) => {
    const s = new URLSearchParams(q).toString();
    return api<any[]>(`/companies${s ? `?${s}` : ''}`);
  },
  setCompanyStatus: (id: string, status: string) =>
    api(`/companies/${id}/status/${status}`, { method: 'PATCH' }),
  users: () => api<any[]>('/users'),
  createUser: (payload: Record<string, unknown>) => api('/users', { method: 'POST', json: payload }),
  updateUser: (id: string, payload: Record<string, unknown>) =>
    api(`/users/${encodeURIComponent(id)}`, { method: 'PATCH', json: payload }),
  deleteUser: (id: string) => api(`/users/${encodeURIComponent(id)}`, { method: 'DELETE' }),
  setUserActive: (id: string, active: boolean) =>
    api(`/users/${encodeURIComponent(id)}/active/${active}`, { method: 'PATCH' }),

  // stats
  kpis: () => api<any>('/stats/kpis'),

  // locations (States + OriginDestination cities)
  locationsStates: () => api<{ id: number; code: string; name: string }[]>('/locations/states'),
  locationsCities: (stateCode: string) =>
    api<{ city: string }[]>(`/locations/cities?stateCode=${encodeURIComponent(stateCode)}`),
  locationsPlaces: (q: string, take?: number, init?: RequestInit) => {
    const sp = new URLSearchParams({ q });
    if (take != null) sp.set('take', String(take));
    return api<{ id: number; city: string; stateCode: string }[]>(`/locations/places?${sp}`, init ?? {});
  },
};
