import request from 'mslx-request';

export function getBaseUrl(): string {
  if (typeof window !== 'undefined') {
    const stores = (window as any).MSLX_Stores;
    if (stores?.useUserStore) {
      try {
        const userStore = stores.useUserStore();
        if (userStore?.baseUrl) return userStore.baseUrl;
      } catch {
        // fallback
      }
    }
  }
  return localStorage.getItem('mslx-base-url') || (typeof window !== 'undefined' ? window.location.origin : '');
}

export function getMediaTokenParam(): string {
  const mediaToken = localStorage.getItem('MSLX_MEDIA_TOKEN');
  if (mediaToken) {
    const expStr = localStorage.getItem('MSLX_MEDIA_TOKEN_EXP');
    const exp = expStr ? parseInt(expStr, 10) : 0;
    if (!exp || Date.now() < exp) {
      return `?media_token=${encodeURIComponent(mediaToken)}`;
    }
  }
  const token = localStorage.getItem('mslx-web-token') || localStorage.getItem('token');
  return token ? `?x-user-token=${encodeURIComponent(token)}` : '';
}

export async function ensureMediaToken(): Promise<string> {
  const mediaToken = localStorage.getItem('MSLX_MEDIA_TOKEN');
  const expStr = localStorage.getItem('MSLX_MEDIA_TOKEN_EXP');
  const exp = expStr ? parseInt(expStr, 10) : 0;
  if (mediaToken && (!exp || Date.now() < exp)) {
    return mediaToken;
  }
  try {
    const res: any = await request.get({ url: '/api/auth/media-token' });
    if (res?.token) {
      localStorage.setItem('MSLX_MEDIA_TOKEN', res.token);
      if (res.expiresIn) {
        localStorage.setItem('MSLX_MEDIA_TOKEN_EXP', (Date.now() + res.expiresIn * 1000).toString());
      }
      return res.token;
    }
  } catch (e) {
    console.error('Failed to get media token', e);
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
