<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  LocationIcon,
  HomeIcon,
  ServerIcon,
  ViewListIcon,
  LockOnIcon,
  ChevronRightIcon,
  ShareIcon,
  SunnyIcon,
  MoonIcon,
} from 'tdesign-icons-vue-next';
import { MessagePlugin } from 'tdesign-vue-next';
import {
  getWorldSpawn,
  getTileUrl,
  ensureMediaToken,
  getMediaTokenParam,
  getPublicMapInfo,
  authPublicMap,
  getPublicServers,
  hashPassword,
  getStoredMapToken,
  setStoredMapToken,
  removeStoredMapToken,
  type PublicMapInfoDto,
  type PublicServerItem,
} from '../api/map';

const route = useRoute();
const router = useRouter();

const loading = ref(true);
const publicMapInfo = ref<PublicMapInfoDto | null>(null);
const publicServerList = ref<PublicServerItem[]>([]);
const isDirectoryView = ref(false);
const errorMessage = ref<string>('');

const currentMapToken = ref<string>('');
const needsPassword = ref(false);
const inputPassword = ref('');
const passwordError = ref('');
const verifyingPassword = ref(false);

const isDark = ref(false);

const scale = ref(1);
const translateX = ref(0);
const translateY = ref(0);
let isDragging = false;
let startX = 0;
let startY = 0;

const inputX = ref<number | null>(0);
const inputZ = ref<number | null>(0);

interface RegionTile {
  x: number;
  z: number;
  key: string;
  src: string;
  loaded: boolean;
}
const loadedRegions = ref<RegionTile[]>([]);

const centerCoordinates = computed(() => {
  const currentScale = scale.value || 1;
  const worldX = -translateX.value / currentScale;
  const worldZ = -translateY.value / currentScale;

  return {
    blockX: Math.round(worldX),
    blockZ: Math.round(worldZ),
    regionX: Math.round(worldX / 512),
    regionZ: Math.round(worldZ / 512),
  };
});

// 维度配置
const allDimensionOptions = [
  { label: '主世界', value: 'overworld' },
  { label: '下界', value: 'the_nether' },
  { label: '末地', value: 'the_end' },
];

const availableDimensionOptions = computed(() => {
  const allowed = publicMapInfo.value?.allowedDimensions;
  if (!allowed || allowed.length === 0) return allDimensionOptions;
  return allDimensionOptions.filter((d) => allowed.includes(d.value));
});

const currentDimension = ref<'overworld' | 'the_nether' | 'the_end'>('overworld');

const currentDimensionLabel = computed(() => {
  const opt = availableDimensionOptions.value.find((d) => d.value === currentDimension.value);
  return opt ? opt.label : '主世界';
});

const currentServerId = computed(() => {
  return publicMapInfo.value?.serverId ?? null;
});

const currentServerTitle = computed(() => {
  if (!publicMapInfo.value) return '';
  return publicMapInfo.value.title || publicMapInfo.value.name || `实例 #${publicMapInfo.value.serverId}`;
});

const jumpToCoordinates = (targetX: number, targetZ: number) => {
  translateX.value = -targetX * scale.value;
  translateY.value = -targetZ * scale.value;
  updateVisibleRegions();
};

const handleDimensionChange = async () => {
  loadedRegions.value = [];
  scale.value = 1;
  await jumpToSpawn();
};

const handleSearchJump = () => {
  const x = Number(inputX.value) || 0;
  const z = Number(inputZ.value) || 0;
  jumpToCoordinates(x, z);
};

const jumpToSpawn = async () => {
  if (currentServerId.value === null) return;
  try {
    const spawn = await getWorldSpawn(currentServerId.value, currentDimension.value, currentMapToken.value);
    inputX.value = spawn.x;
    inputZ.value = spawn.z;
    jumpToCoordinates(spawn.x, spawn.z);
  } catch (err: any) {
    if (err?.code === 401 || err?.message?.includes('密码') || err?.message?.includes('凭证')) {
      if (currentServerId.value) removeStoredMapToken(currentServerId.value);
      if (publicMapInfo.value?.slug) removeStoredMapToken(publicMapInfo.value.slug);
      needsPassword.value = true;
      return;
    }
    inputX.value = 0;
    inputZ.value = 0;
    jumpToCoordinates(0, 0);
  }
};

const updateVisibleRegions = () => {
  if (currentServerId.value === null || needsPassword.value) return;

  const cx = centerCoordinates.value.regionX;
  const cz = centerCoordinates.value.regionZ;
  const missingTiles: { x: number; z: number; key: string; distance: number }[] = [];

  for (let dx = -2; dx <= 2; dx++) {
    for (let dz = -2; dz <= 2; dz++) {
      const rx = cx + dx;
      const rz = cz + dz;
      const key = `${rx}_${rz}`;

      if (!loadedRegions.value.find((r) => r.key === `${currentDimension.value}_${key}`)) {
        const distance = dx * dx + dz * dz;
        missingTiles.push({ x: rx, z: rz, key, distance });
      }
    }
  }

  missingTiles.sort((a, b) => a.distance - b.distance);

  const tokenParam = getMediaTokenParam();
  missingTiles.forEach((item) => {
    const src = getTileUrl(
      currentServerId.value!,
      item.x,
      item.z,
      tokenParam,
      currentDimension.value,
      currentMapToken.value
    );

    loadedRegions.value.push({
      x: item.x,
      z: item.z,
      key: `${currentDimension.value}_${item.key}`,
      src,
      loaded: false,
    });
  });
};

const startDrag = (e: MouseEvent) => {
  isDragging = true;
  startX = e.clientX - translateX.value;
  startY = e.clientY - translateY.value;
};

const onDrag = (e: MouseEvent) => {
  if (!isDragging) return;
  translateX.value = e.clientX - startX;
  translateY.value = e.clientY - startY;
  updateVisibleRegions();
};

const stopDrag = () => {
  isDragging = false;
};

const handleScroll = (e: WheelEvent) => {
  e.preventDefault();
  const zoomDirection = e.deltaY > 0 ? -0.1 : 0.1;
  const newScale = Math.min(Math.max(0.15, scale.value + zoomDirection), 6);

  const scaleRatio = newScale / scale.value;
  translateX.value = translateX.value * scaleRatio;
  translateY.value = translateY.value * scaleRatio;
  scale.value = newScale;

  updateVisibleRegions();
};

// 分享地图链接
const handleShare = async () => {
  const slugOrId = publicMapInfo.value?.slug || currentServerId.value || '';
  const shareUrl = `${window.location.origin}/public/map/${slugOrId}?dimension=${currentDimension.value}&x=${centerCoordinates.value.blockX}&z=${centerCoordinates.value.blockZ}`;
  try {
    await navigator.clipboard.writeText(shareUrl);
    MessagePlugin.success('地图链接已复制到剪贴板');
  } catch {
    MessagePlugin.warning('复制失败，请手动复制当前浏览器地址');
  }
};

// 主题切换
const checkInitialTheme = () => {
  const mode = document.documentElement.getAttribute('theme-mode');
  if (mode === 'dark') return true;
  if (mode === 'light') return false;
  return (
    document.documentElement.classList.contains('dark') ||
    (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches)
  );
};

const toggleTheme = () => {
  isDark.value = !isDark.value;
  applyTheme(isDark.value);
};

const applyTheme = (dark: boolean) => {
  if (dark) {
    document.documentElement.setAttribute('theme-mode', 'dark');
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.setAttribute('theme-mode', 'light');
    document.documentElement.classList.remove('dark');
  }
};

// 全屏控制
const isFullscreen = ref(false);
const toggleFullscreen = () => {
  if (!document.fullscreenElement) {
    document.documentElement.requestFullscreen().then(() => {
      isFullscreen.value = true;
    }).catch(() => {});
  } else {
    document.exitFullscreen().then(() => {
      isFullscreen.value = false;
    }).catch(() => {});
  }
};

const handleFullscreenChange = () => {
  isFullscreen.value = !!document.fullscreenElement;
};

// 密码验证
const handleVerifyPassword = async () => {
  if (!inputPassword.value.trim()) {
    passwordError.value = '请输入访问密码';
    return;
  }

  verifyingPassword.value = true;
  passwordError.value = '';
  try {
    const hash = await hashPassword(inputPassword.value.trim());
    const res: any = await authPublicMap({
      slug: publicMapInfo.value?.slug,
      serverId: publicMapInfo.value?.serverId,
      passwordHash: hash,
    });

    const token = res?.mapToken ?? res?.MapToken;
    currentMapToken.value = token;
    if (publicMapInfo.value?.slug) {
      setStoredMapToken(publicMapInfo.value.slug, token);
    }
    if (publicMapInfo.value?.serverId) {
      setStoredMapToken(publicMapInfo.value.serverId, token);
    }

    needsPassword.value = false;
    inputPassword.value = '';
    MessagePlugin.success('验证通过');
    await initMapContent();
  } catch (err: any) {
    passwordError.value = err?.message || '访问密码错误，请重试';
  } finally {
    verifyingPassword.value = false;
  }
};

// 初始化地图视角与切片
const initMapContent = async () => {
  const allowed = publicMapInfo.value?.allowedDimensions || [];
  if (allowed.length > 0 && !allowed.includes(currentDimension.value)) {
    currentDimension.value = allowed[0] as any;
  }

  const urlParams = new URLSearchParams(window.location.search);
  const paramDim = urlParams.get('dimension');
  if (paramDim && allowed.includes(paramDim)) {
    currentDimension.value = paramDim as any;
  }

  const paramX = urlParams.get('x');
  const paramZ = urlParams.get('z');
  if (paramX !== null && paramZ !== null) {
    inputX.value = Number(paramX);
    inputZ.value = Number(paramZ);
    jumpToCoordinates(Number(paramX), Number(paramZ));
  } else {
    await jumpToSpawn();
  }
};

const loadInstanceMap = async (slugOrId: string | number) => {
  loading.value = true;
  errorMessage.value = '';
  isDirectoryView.value = false;

  const isNumeric = /^\d+$/.test(String(slugOrId));
  const serverId = isNumeric ? Number(slugOrId) : undefined;
  const slug = isNumeric ? undefined : String(slugOrId);

  currentMapToken.value = getStoredMapToken(String(slugOrId));

  try {
    const rawInfo: any = await getPublicMapInfo({
      slug,
      serverId,
      mapToken: currentMapToken.value,
    });

    const info: PublicMapInfoDto = {
      serverId: rawInfo?.serverId ?? rawInfo?.ServerId ?? 0,
      name: rawInfo?.name ?? rawInfo?.Name ?? '',
      title: rawInfo?.title ?? rawInfo?.Title ?? '',
      slug: rawInfo?.slug ?? rawInfo?.Slug ?? '',
      allowedDimensions: rawInfo?.allowedDimensions ?? rawInfo?.AllowedDimensions ?? ['overworld', 'the_nether', 'the_end'],
      requirePassword: rawInfo?.requirePassword ?? rawInfo?.RequirePassword ?? false,
      hasAccess: rawInfo?.hasAccess ?? rawInfo?.HasAccess ?? false,
    };
    publicMapInfo.value = info;

    if (info.title || info.name) {
      window.document.title = `${info.title || info.name} - 世界地图`;
    }

    if (info.requirePassword && !info.hasAccess) {
      needsPassword.value = true;
      loading.value = false;
      return;
    }

    needsPassword.value = false;
    await initMapContent();
  } catch (err: any) {
    console.error('加载地图失败:', err);
    if (err?.code === 401 || err?.message?.includes('密码')) {
      removeStoredMapToken(slugOrId);
      needsPassword.value = true;
    } else {
      errorMessage.value = err?.message || '该服务器暂未开启公开地图访问。';
    }
  } finally {
    loading.value = false;
  }
};

const handleSelectDirectoryServer = (server: PublicServerItem) => {
  const target = server.slug || server.id;
  router.push(`/public/map/${target}`);
};

const initPage = async () => {
  isDark.value = checkInitialTheme();
  await ensureMediaToken();

  const routeSlug = (route.params.slug as string) || '';
  if (routeSlug) {
    await loadInstanceMap(routeSlug);
    return;
  }

  const queryServerId = route.query.serverId as string;
  const querySlug = route.query.slug as string;
  if (queryServerId) {
    await loadInstanceMap(Number(queryServerId));
    return;
  }
  if (querySlug) {
    await loadInstanceMap(querySlug);
    return;
  }

  try {
    loading.value = true;
    const rawServers: any = await getPublicServers();
    publicServerList.value = (rawServers || []).map((s: any) => ({
      id: s?.id ?? s?.Id,
      name: s?.name ?? s?.Name,
      slug: s?.slug ?? s?.Slug,
    }));

    if (publicServerList.value.length === 0) {
      errorMessage.value = '暂无可公开查看的服务器地图';
      loading.value = false;
      return;
    }

    if (publicServerList.value.length === 1) {
      const s = publicServerList.value[0];
      await loadInstanceMap(s.slug || s.id);
      return;
    }

    isDirectoryView.value = true;
    loading.value = false;
  } catch (err: any) {
    errorMessage.value = err?.message || '获取公开服务器列表失败';
    loading.value = false;
  }
};

watch(
  () => route.params.slug,
  (newSlug) => {
    if (newSlug) {
      loadedRegions.value = [];
      loadInstanceMap(newSlug as string);
    }
  }
);

onMounted(() => {
  document.addEventListener('fullscreenchange', handleFullscreenChange);
  initPage();
});

onUnmounted(() => {
  document.removeEventListener('fullscreenchange', handleFullscreenChange);
});
</script>

<template>
  <div class="w-screen h-screen overflow-hidden relative select-none bg-zinc-50 dark:bg-zinc-950 font-sans text-zinc-800 dark:text-zinc-100 transition-colors duration-200">
    <!-- 加载状态 -->
    <div v-if="loading" class="w-full h-full flex flex-col items-center justify-center gap-3">
      <t-loading size="large" />
      <span class="text-sm text-zinc-500 dark:text-zinc-400">正在载入世界地图...</span>
    </div>

    <!-- 目录视图 -->
    <div
      v-else-if="isDirectoryView"
      class="w-full h-full flex flex-col items-center justify-center p-6 bg-zinc-50 dark:bg-zinc-950 relative overflow-y-auto"
    >
      <div class="w-full max-w-lg flex flex-col gap-5">
        <div class="text-center">
          <div class="inline-flex p-3 rounded-2xl bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 text-sky-500 mb-3 shadow-md">
            <server-icon size="32px" />
          </div>
          <h2 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100 m-0 mb-1">公开世界地图</h2>
          <p class="text-sm text-zinc-500 dark:text-zinc-400 m-0">请选择要查看的服务器</p>
        </div>

        <div class="flex flex-col gap-2.5">
          <div
            v-for="s in publicServerList"
            :key="s.id"
            class="group flex items-center justify-between p-4 rounded-xl bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 hover:border-zinc-300 dark:hover:border-zinc-700 hover:shadow-md transition-all cursor-pointer"
            @click="handleSelectDirectoryServer(s)"
          >
            <div class="flex items-center gap-3 min-w-0">
              <div class="w-10 h-10 rounded-lg bg-zinc-100 dark:bg-zinc-800 flex items-center justify-center text-zinc-500 dark:text-zinc-300">
                <server-icon size="20px" />
              </div>
              <div class="flex flex-col min-w-0">
                <span class="text-sm font-semibold text-zinc-800 dark:text-zinc-200 group-hover:text-sky-500 transition-colors truncate">
                  {{ s.name }}
                </span>
                <span class="text-xs text-zinc-400 font-mono">
                  /public/map/{{ s.slug || s.id }}
                </span>
              </div>
            </div>
            <chevron-right-icon class="text-zinc-400 group-hover:text-zinc-600 dark:group-hover:text-zinc-200 transition-transform group-hover:translate-x-1" size="20px" />
          </div>
        </div>
      </div>
    </div>

    <!-- 异常状态 -->
    <div
      v-else-if="errorMessage"
      class="w-full h-full flex flex-col items-center justify-center gap-4 p-6 text-center"
    >
      <div class="w-16 h-16 rounded-2xl bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 flex items-center justify-center text-zinc-400 shadow-md">
        <server-icon size="32px" />
      </div>
      <h2 class="text-xl font-bold text-zinc-800 dark:text-zinc-200 m-0">地图未开放</h2>
      <p class="text-sm text-zinc-500 dark:text-zinc-400 max-w-md m-0 leading-relaxed">
        {{ errorMessage }}
      </p>
    </div>

    <!-- 正常地图交互视口 -->
    <div
      v-else
      class="w-full h-full relative overflow-hidden cursor-grab active:cursor-grabbing custom-grid-bg"
      @wheel="handleScroll"
      @mousedown="startDrag"
      @mousemove="onDrag"
      @mouseup="stopDrag"
      @mouseleave="stopDrag"
    >
      <!-- 密码验证卡片 -->
      <div
        v-if="needsPassword"
        class="absolute inset-0 z-50 flex items-center justify-center p-4 bg-zinc-950/70 backdrop-blur-md"
        @mousedown.stop
        @wheel.stop
      >
        <div class="w-full max-w-sm p-6 rounded-2xl bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 shadow-2xl flex flex-col items-center text-center">
          <div class="w-14 h-14 rounded-2xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-500 mb-3 shadow-inner">
            <lock-on-icon size="28px" />
          </div>
          <h3 class="text-lg font-bold text-zinc-900 dark:text-zinc-100 m-0 mb-1">输入访问密码</h3>
          <p class="text-xs text-zinc-500 dark:text-zinc-400 mb-5 leading-relaxed">
            该服务器地图已设置访问密码，请输入密码继续。
          </p>

          <div class="w-full flex flex-col gap-3">
            <t-input
              v-model="inputPassword"
              type="password"
              placeholder="请输入访问密码"
              size="large"
              clearable
              autofocus
              @enter="handleVerifyPassword"
            >
              <template #prefix-icon><lock-on-icon /></template>
            </t-input>

            <div v-if="passwordError" class="text-xs text-rose-500 text-left -mt-1 pl-1">
              {{ passwordError }}
            </div>

            <t-button
              theme="primary"
              size="large"
              block
              :loading="verifyingPassword"
              class="!rounded-xl shadow-md"
              @click="handleVerifyPassword"
            >
              确认解锁
            </t-button>
          </div>
        </div>
      </div>

      <!-- 地图瓦片承载容器 -->
      <div
        class="absolute top-1/2 left-1/2 origin-center transition-transform duration-[50ms] ease-linear"
        :style="{ transform: `translate(${translateX}px, ${translateY}px) scale(${scale})` }"
      >
        <div
          v-for="tile in loadedRegions"
          :key="tile.key"
          class="absolute w-[512px] h-[512px] -ml-[256px] -mt-[256px] flex items-center justify-center bg-zinc-100/50 dark:bg-zinc-900/50"
          :style="{ left: `${tile.x * 512}px`, top: `${tile.z * 512}px` }"
        >
          <img
            :src="tile.src"
            class="w-full h-full pointer-events-none opacity-0 transition-opacity duration-500 ease-out [image-rendering:pixelated]"
            :class="{ 'opacity-100': tile.loaded }"
            @load="tile.loaded = true"
            alt="tile"
            draggable="false"
          />
        </div>
      </div>

      <!-- 中心十字准心 -->
      <div
        class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none z-10 w-4 h-4 flex items-center justify-center"
      >
        <div class="absolute w-[2px] h-3.5 bg-red-500 shadow-[0_0_2px_rgba(0,0,0,0.6)]"></div>
        <div class="absolute w-3.5 h-[2px] bg-red-500 shadow-[0_0_2px_rgba(0,0,0,0.6)]"></div>
      </div>

      <!-- 顶部控制浮动栏 -->
      <div
        class="absolute top-4 left-4 p-2.5 flex flex-wrap items-center gap-2 bg-white/90 dark:bg-zinc-900/90 backdrop-blur-md rounded-2xl border border-zinc-200/80 dark:border-zinc-700/60 shadow-lg z-20 cursor-default"
        @mousedown.stop
        @wheel.stop
      >
        <!-- 当前服务器名称徽章 -->
        <div class="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-zinc-100/90 dark:bg-zinc-800/90 border border-zinc-200/80 dark:border-zinc-700/60 text-xs font-semibold text-zinc-700 dark:text-zinc-200">
          <server-icon size="16px" class="text-sky-500 shrink-0" />
          <span class="max-w-[180px] truncate">{{ currentServerTitle }}</span>
        </div>

        <!-- 维度选择（当开放多个维度时展示） -->
        <template v-if="availableDimensionOptions.length > 1">
          <div class="w-px h-6 bg-zinc-200 dark:bg-zinc-700/60 mx-0.5"></div>
          <t-select
            v-model="currentDimension"
            :options="availableDimensionOptions"
            class="!w-[110px]"
            :popup-props="{ zIndex: 9999 }"
            @change="handleDimensionChange"
          />
        </template>

        <div class="w-px h-6 bg-zinc-200 dark:bg-zinc-700/60 mx-0.5"></div>

        <!-- 坐标跳转 -->
        <t-input v-model="inputX" type="number" placeholder="X" class="!w-[76px]" @enter="handleSearchJump" />
        <t-input v-model="inputZ" type="number" placeholder="Z" class="!w-[76px]" @enter="handleSearchJump" />

        <t-button theme="primary" class="!rounded-xl shadow-sm" @click="handleSearchJump">
          <template #icon><location-icon /></template> 定位
        </t-button>

        <!-- 回到出生点 -->
        <t-button
          variant="outline"
          class="!rounded-xl !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-700 dark:!text-zinc-200 transition-colors"
          title="回到出生点"
          @click="jumpToSpawn"
        >
          <template #icon><home-icon /></template>
        </t-button>

        <div class="w-px h-6 bg-zinc-200 dark:bg-zinc-700/60 mx-0.5"></div>

        <!-- 分享按钮 -->
        <t-button
          variant="outline"
          class="!rounded-xl !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-700 dark:!text-zinc-200 transition-colors"
          title="分享当前位置地图链接"
          @click="handleShare"
        >
          <template #icon><share-icon /></template> 分享
        </t-button>

        <!-- 明暗模式切换 -->
        <t-button
          variant="outline"
          class="!rounded-xl !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-700 dark:!text-zinc-200 transition-colors"
          :title="isDark ? '切换至亮色模式' : '切换至暗色模式'"
          @click="toggleTheme"
        >
          <template #icon>
            <sunny-icon v-if="isDark" />
            <moon-icon v-else />
          </template>
        </t-button>

        <!-- 全屏切换 -->
        <t-button
          variant="outline"
          class="!rounded-xl !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-700 dark:!text-zinc-200 transition-colors"
          :title="isFullscreen ? '退出全屏' : '全屏显示'"
          @click="toggleFullscreen"
        >
          <template #icon><view-list-icon /></template>
        </t-button>
      </div>

      <!-- 底部信息 HUD 卡片 -->
      <div
        class="absolute bottom-4 right-4 p-4 flex flex-col font-mono text-sm bg-white/90 dark:bg-zinc-900/90 backdrop-blur-md rounded-2xl border border-zinc-200/80 dark:border-zinc-700/60 shadow-lg z-20 pointer-events-none min-w-[210px]"
      >
        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-500 dark:text-zinc-400">服务器:</span>
          <span class="font-bold text-sky-500 truncate max-w-[130px]">{{ currentServerTitle }}</span>
        </div>

        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-500 dark:text-zinc-400">当前维度:</span>
          <span class="font-bold text-emerald-500">{{ currentDimensionLabel }}</span>
        </div>

        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-500 dark:text-zinc-400">方块坐标:</span>
          <span class="font-bold text-zinc-800 dark:text-zinc-100">
            X: {{ centerCoordinates.blockX }}, Z: {{ centerCoordinates.blockZ }}
          </span>
        </div>

        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-500 dark:text-zinc-400">区块区号:</span>
          <span class="font-bold text-zinc-600 dark:text-zinc-300">
            r.{{ centerCoordinates.regionX }}.{{ centerCoordinates.regionZ }}
          </span>
        </div>

        <div class="flex justify-between items-center gap-6 mb-3">
          <span class="text-xs text-zinc-500 dark:text-zinc-400">当前缩放:</span>
          <span class="font-bold text-amber-500">{{ Math.round(scale * 100) }}%</span>
        </div>

        <div
          class="pt-2.5 border-t border-dashed border-zinc-200 dark:border-zinc-700/80 text-right text-[11px] text-zinc-400 font-sans tracking-wider"
        >
          滚轮缩放 / 拖拽移动
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.custom-grid-bg {
  background-image:
    linear-gradient(rgba(161, 161, 170, 0.25) 1px, transparent 1px),
    linear-gradient(90deg, rgba(161, 161, 170, 0.25) 1px, transparent 1px);
  background-size: 32px 32px;
}

:global(html[theme-mode='dark']) .custom-grid-bg,
:global(html.dark) .custom-grid-bg {
  background-image:
    linear-gradient(rgba(82, 82, 91, 0.3) 1px, transparent 1px),
    linear-gradient(90deg, rgba(82, 82, 91, 0.3) 1px, transparent 1px);
}
</style>
