import request from 'mslx-request';

export function getUserStore() {
  if (typeof window !== 'undefined') {
    const stores = (window as any).MSLX_Stores;
    if (stores) {
      try {
        return stores.getUserStore?.() || stores.useUserStore?.();
      } catch {
        return null;
      }
    }
  }
  return null;
}

export function getBaseUrl(): string {
  const userStore = getUserStore();
  if (userStore?.baseUrl) {
    return userStore.baseUrl;
  }
  return localStorage.getItem('mslx-base-url') || (typeof window !== 'undefined' ? window.location.origin : '');
}

export function getMediaTokenParam(): string {
  const userStore = getUserStore();
  if (userStore?.getMediaTokenParam) {
    return userStore.getMediaTokenParam();
  }
  const token = localStorage.getItem('mslx-web-token') || localStorage.getItem('token');
  return token ? `?x-user-token=${encodeURIComponent(token)}` : '';
}

export async function ensureMediaToken(): Promise<string> {
  const userStore = getUserStore();
  if (userStore?.getMediaToken) {
    try {
      return await userStore.getMediaToken();
    } catch (e) {
      console.error('Failed to get media token from userStore', e);
    }
  }
  return '';
}

export async function getWorldSpawn(serverId: number): Promise<{ x: number; z: number }> {
  return await request.get({
    url: `/api/plugin/mslx-plugin-map-render/map/spawn/${serverId}`,
  });
}

export function getTileUrl(serverId: number, regionX: number, regionZ: number, tokenParam = ''): string {
  const host = getBaseUrl();
  return `${host}/api/plugin/mslx-plugin-map-render/map/${serverId}/${regionX}/${regionZ}${tokenParam}`;
}
