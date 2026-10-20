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

// 纯 JS SHA-256 兼容备用方案（用于非 HTTPS/非 localhost 环境下 WebCrypto 不可用的场景）
function sha256Fallback(ascii: string): string {
  function rightRotate(value: number, amount: number) {
    return (value >>> amount) | (value << (32 - amount));
  }
  const mathPow = Math.pow;
  const maxWord = mathPow(2, 32);
  const lengthProperty = 'length';
  let i = 0;
  let j = 0;
  let result = '';
  const words: number[] = [];
  const asciiBitLength = ascii[lengthProperty] * 8;
  let hash: number[] = [];
  const k: number[] = [];
  let primeCounter = 0;
  const isComposite: Record<number, number> = {};

  for (let candidate = 2; primeCounter < 64; candidate++) {
    if (!isComposite[candidate]) {
      for (i = 0; i < 300; i += candidate) {
        isComposite[i] = candidate;
      }
      hash[primeCounter] = (mathPow(candidate, 0.5) * maxWord) | 0;
      k[primeCounter++] = (mathPow(candidate, 1 / 3) * maxWord) | 0;
    }
  }

  hash = hash.slice(0, 8);
  ascii += '\x80';
  while ((ascii[lengthProperty] % 64) - 56) ascii += '\x00';
  for (i = 0; i < ascii[lengthProperty]; i++) {
    j = ascii.charCodeAt(i);
    words[i >> 2] |= j << ((3 - (i % 4)) * 8);
  }
  words[words[lengthProperty]] = (asciiBitLength / maxWord) | 0;
  words[words[lengthProperty]] = asciiBitLength;

  for (j = 0; j < words[lengthProperty]; ) {
    const w = words.slice(j, (j += 16));
    const oldHash = hash;
    hash = hash.slice(0, 8);
    for (i = 0; i < 64; i++) {
      const w15 = w[i - 15];
      const w2 = w[i - 2];
      const a = hash[0];
      const e = hash[4];
      const temp1 =
        hash[7] +
        (rightRotate(e, 6) ^ rightRotate(e, 11) ^ rightRotate(e, 25)) +
        ((e & hash[5]) ^ (~e & hash[6])) +
        k[i] +
        (w[i] =
          i < 16
            ? w[i]
            : (w[i - 16] +
                (rightRotate(w15, 7) ^ rightRotate(w15, 18) ^ (w15 >>> 3)) +
                w[i - 7] +
                (rightRotate(w2, 17) ^ rightRotate(w2, 19) ^ (w2 >>> 10))) |
              0);
      const temp2 =
        (rightRotate(a, 2) ^ rightRotate(a, 13) ^ rightRotate(a, 22)) +
        ((a & hash[1]) ^ (a & hash[2]) ^ (hash[1] & hash[2]));

      hash = [(temp1 + temp2) | 0].concat(hash);
      hash[4] = (hash[4] + temp1) | 0;
    }
    for (i = 0; i < 8; i++) {
      hash[i] = (hash[i] + oldHash[i]) | 0;
    }
  }

  for (i = 0; i < 8; i++) {
    for (j = 3; j + 1; j--) {
      const b = (hash[i] >> (j * 8)) & 255;
      result += (b < 16 ? '0' : '') + b.toString(16);
    }
  }
  return result;
}

export async function hashPassword(password: string): Promise<string> {
  if (typeof window !== 'undefined' && window.crypto && window.crypto.subtle) {
    try {
      const encoder = new TextEncoder();
      const data = encoder.encode(password);
      const hashBuffer = await window.crypto.subtle.digest('SHA-256', data);
      const hashArray = Array.from(new Uint8Array(hashBuffer));
      return hashArray.map((b) => b.toString(16).padStart(2, '0')).join('');
    } catch {
      // fallback
    }
  }
  return sha256Fallback(password);
}

// 本地会话 Token 存取
export function getStoredMapToken(identifier: string | number): string {
  if (typeof window === 'undefined') return '';
  return sessionStorage.getItem(`mslx_map_token_${identifier}`) || '';
}

export function setStoredMapToken(identifier: string | number, token: string): void {
  if (typeof window === 'undefined') return;
  sessionStorage.setItem(`mslx_map_token_${identifier}`, token);
}

export function removeStoredMapToken(identifier: string | number): void {
  if (typeof window === 'undefined') return;
  sessionStorage.removeItem(`mslx_map_token_${identifier}`);
}

// 接口类型定义
export interface InstanceMapConfigDto {
  serverId: number;
  enabled: boolean;
  slug: string;
  title: string;
  allowedDimensions: string[];
  passwordEnabled: boolean;
  hasPassword?: boolean;
  newPasswordHash?: string | null;
}

export interface PublicMapInfoDto {
  serverId: number;
  name: string;
  title: string;
  slug: string;
  allowedDimensions: string[];
  requirePassword: boolean;
  hasAccess: boolean;
}

export interface PublicServerItem {
  id: number;
  name: string;
  slug?: string;
}

// API 请求函数
export async function getInstanceMapConfig(serverId: number): Promise<InstanceMapConfigDto> {
  return await request.get({
    url: `/api/plugin/mslx-plugin-map-render/map/instance-config/${serverId}`,
  });
}

export async function saveInstanceMapConfig(serverId: number, data: InstanceMapConfigDto): Promise<void> {
  return await request.post({
    url: `/api/plugin/mslx-plugin-map-render/map/instance-config/${serverId}`,
    data,
  });
}

export async function getPublicMapInfo(params: { slug?: string; serverId?: number; mapToken?: string }): Promise<PublicMapInfoDto> {
  const queryList: string[] = [];
  if (params.slug) queryList.push(`slug=${encodeURIComponent(params.slug)}`);
  if (params.serverId) queryList.push(`serverId=${encodeURIComponent(params.serverId)}`);
  if (params.mapToken) queryList.push(`map_token=${encodeURIComponent(params.mapToken)}`);

  const qs = queryList.length > 0 ? `?${queryList.join('&')}` : '';
  const headers: Record<string, string> = {};
  if (params.mapToken) {
    headers['X-Map-Token'] = params.mapToken;
  }

  return await request.get({
    url: `/api/plugin/mslx-plugin-map-render/map/public/info${qs}`,
    headers,
  });
}

export async function authPublicMap(data: { slug?: string; serverId?: number; passwordHash: string }): Promise<{ serverId: number; mapToken: string; expiresIn: number }> {
  return await request.post({
    url: `/api/plugin/mslx-plugin-map-render/map/public/auth`,
    data,
  });
}

export async function getPublicServers(): Promise<PublicServerItem[]> {
  return await request.get({
    url: '/api/plugin/mslx-plugin-map-render/map/public/servers',
  });
}

export async function getWorldSpawn(serverId: number, dimension = 'overworld', mapToken = ''): Promise<{ x: number; z: number }> {
  const params: string[] = [];
  if (dimension && dimension !== 'overworld') {
    params.push(`dimension=${encodeURIComponent(dimension)}`);
  }
  if (mapToken) {
    params.push(`map_token=${encodeURIComponent(mapToken)}`);
  }
  const qs = params.length > 0 ? `?${params.join('&')}` : '';
  const headers: Record<string, string> = {};
  if (mapToken) {
    headers['X-Map-Token'] = mapToken;
  }

  return await request.get({
    url: `/api/plugin/mslx-plugin-map-render/map/spawn/${serverId}${qs}`,
    headers,
  });
}

export function getTileUrl(
  serverId: number,
  regionX: number,
  regionZ: number,
  tokenParam = '',
  dimension = 'overworld',
  mapToken = ''
): string {
  const host = getBaseUrl();
  let url = `${host}/api/plugin/mslx-plugin-map-render/map/${serverId}/${regionX}/${regionZ}`;
  const params: string[] = [];

  if (tokenParam) {
    params.push(tokenParam.startsWith('?') ? tokenParam.slice(1) : tokenParam);
  }
  if (dimension && dimension !== 'overworld') {
    params.push(`dimension=${encodeURIComponent(dimension)}`);
  }
  if (mapToken) {
    params.push(`map_token=${encodeURIComponent(mapToken)}`);
  }

  return params.length > 0 ? `${url}?${params.join('&')}` : url;
}
