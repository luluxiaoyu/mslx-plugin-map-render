<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { LocationIcon, HomeIcon, DownloadIcon, ServerIcon, ViewListIcon } from 'tdesign-icons-vue-next';
import { MessagePlugin } from 'tdesign-vue-next';
import {
  getWorldSpawn,
  getTileUrl,
  ensureMediaToken,
  getMediaTokenParam,
  getPublicServers,
  type PublicServerItem,
} from '../api/map';

const loading = ref(true);
const publicServers = ref<PublicServerItem[]>([]);
const currentServerId = ref<number | null>(null);
const errorMessage = ref<string>('');

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
const currentDimension = ref<'overworld' | 'the_nether' | 'the_end'>('overworld');
const dimensionOptions = [
  { label: '主世界', value: 'overworld' },
  { label: '下界', value: 'the_nether' },
  { label: '末地', value: 'the_end' },
];

const currentDimensionLabel = computed(() => {
  const opt = dimensionOptions.find((d) => d.value === currentDimension.value);
  return opt ? opt.label : '主世界';
});

const currentServerName = computed(() => {
  const s = publicServers.value.find((item) => item.id === currentServerId.value);
  return s ? s.name : `实例 #${currentServerId.value}`;
});

// 导出功能状态
const exportDialogVisible = ref(false);
const exportMode = ref<'loaded' | 'custom'>('loaded');
const isExporting = ref(false);
const exportRange = ref({
  startX: 0,
  startZ: 0,
  endX: 0,
  endZ: 0,
});

const openExportDialog = () => {
  exportRange.value = {
    startX: centerCoordinates.value.regionX - 1,
    startZ: centerCoordinates.value.regionZ - 1,
    endX: centerCoordinates.value.regionX + 1,
    endZ: centerCoordinates.value.regionZ + 1,
  };
  exportDialogVisible.value = true;
};

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

const handleServerChange = async () => {
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
    const spawn = await getWorldSpawn(currentServerId.value, currentDimension.value);
    inputX.value = spawn.x;
    inputZ.value = spawn.z;
    jumpToCoordinates(spawn.x, spawn.z);
  } catch {
    inputX.value = 0;
    inputZ.value = 0;
    jumpToCoordinates(0, 0);
  }
};

const updateVisibleRegions = () => {
  if (currentServerId.value === null) return;

  const cx = centerCoordinates.value.regionX;
  const cz = centerCoordinates.value.regionZ;
  const missingTiles: { x: number; z: number; key: string; distance: number }[] = [];

  // 全屏模式下视口更大，视野扩展到 5x5 区块
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
    const src = getTileUrl(currentServerId.value!, item.x, item.z, tokenParam, currentDimension.value);

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

// 导出地图
const handleExportMap = async () => {
  if (currentServerId.value === null) return;
  isExporting.value = true;
  try {
    let tilesToExport: { x: number; z: number; src: string }[] = [];

    if (exportMode.value === 'loaded') {
      tilesToExport = loadedRegions.value.filter((t) => t.loaded);
      if (tilesToExport.length === 0) {
        MessagePlugin.warning('当前没有已加载的地图区块');
        isExporting.value = false;
        return;
      }
    } else {
      const { startX, startZ, endX, endZ } = exportRange.value;
      const minX = Math.min(startX, endX);
      const maxX = Math.max(startX, endX);
      const minZ = Math.min(startZ, endZ);
      const maxZ = Math.max(startZ, endZ);

      const totalTiles = (maxX - minX + 1) * (maxZ - minZ + 1);
      if (totalTiles > 100) {
        MessagePlugin.warning(`选择的区块数量(${totalTiles})过大，最大支持同时导出 100 个区块`);
        isExporting.value = false;
        return;
      }

      const tokenParam = getMediaTokenParam();
      for (let rx = minX; rx <= maxX; rx++) {
        for (let rz = minZ; rz <= maxZ; rz++) {
          tilesToExport.push({
            x: rx,
            z: rz,
            src: getTileUrl(currentServerId.value!, rx, rz, tokenParam, currentDimension.value),
          });
        }
      }
    }

    const loadMessage = MessagePlugin.loading('正在拼接地图图像，请稍候...');

    const minX = Math.min(...tilesToExport.map((t) => t.x));
    const maxX = Math.max(...tilesToExport.map((t) => t.x));
    const minZ = Math.min(...tilesToExport.map((t) => t.z));
    const maxZ = Math.max(...tilesToExport.map((t) => t.z));

    const canvas = document.createElement('canvas');
    canvas.width = (maxX - minX + 1) * 512;
    canvas.height = (maxZ - minZ + 1) * 512;
    const ctx = canvas.getContext('2d');

    if (!ctx) throw new Error('无法创建 Canvas 上下文');

    ctx.fillStyle = document.documentElement.classList.contains('dark') ? '#18181b' : '#fafafa';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    const drawPromises = tilesToExport.map((tile) => {
      return new Promise<void>((resolve) => {
        const img = new Image();
        img.crossOrigin = 'anonymous';
        img.onload = () => {
          ctx.drawImage(img, (tile.x - minX) * 512, (tile.z - minZ) * 512, 512, 512);
          resolve();
        };
        img.onerror = () => {
          resolve();
        };
        img.src = tile.src;
      });
    });

    await Promise.all(drawPromises);

    const dataUrl = canvas.toDataURL('image/png');
    const a = document.createElement('a');
    a.href = dataUrl;
    a.download = `${currentServerName.value}_${currentDimension.value}_r.${minX}.${minZ}_to_r.${maxX}.${maxZ}.png`;
    a.click();

    MessagePlugin.close(loadMessage);
    MessagePlugin.success('地图导出成功');
    exportDialogVisible.value = false;
  } catch (error) {
    console.error('地图导出失败:', error);
    MessagePlugin.error('地图导出失败，请检查网络或控制台日志');
  } finally {
    isExporting.value = false;
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

onMounted(async () => {
  document.addEventListener('fullscreenchange', handleFullscreenChange);
  await ensureMediaToken();

  try {
    const servers = await getPublicServers();
    publicServers.value = servers || [];

    if (publicServers.value.length === 0) {
      errorMessage.value = '暂无可公开查看的服务器实例';
      loading.value = false;
      return;
    }

    // 读取 URL 参数中的 serverId
    const urlParams = new URLSearchParams(window.location.search);
    const paramServerId = urlParams.get('serverId');
    if (paramServerId && publicServers.value.some((s) => s.id === Number(paramServerId))) {
      currentServerId.value = Number(paramServerId);
    } else {
      currentServerId.value = publicServers.value[0].id;
    }

    // 读取 URL 参数中的初始坐标与维度
    const paramX = urlParams.get('x');
    const paramZ = urlParams.get('z');
    const paramDim = urlParams.get('dimension');
    if (paramDim && dimensionOptions.some((d) => d.value === paramDim)) {
      currentDimension.value = paramDim as any;
    }

    if (paramX !== null && paramZ !== null) {
      inputX.value = Number(paramX);
      inputZ.value = Number(paramZ);
      jumpToCoordinates(Number(paramX), Number(paramZ));
    } else {
      await jumpToSpawn();
    }
  } catch (err: any) {
    console.error('获取公开服务器地图失败:', err);
    errorMessage.value = err?.message || '无法获取公开服务器列表，请确认宿主已开启公开页面访问';
  } finally {
    loading.value = false;
  }
});

onUnmounted(() => {
  document.removeEventListener('fullscreenchange', handleFullscreenChange);
});
</script>

<template>
  <div class="w-screen h-screen overflow-hidden relative select-none bg-zinc-950 font-sans text-zinc-100">
    <!-- 加载中状态 -->
    <div v-if="loading" class="w-full h-full flex flex-col items-center justify-center gap-3">
      <t-loading size="large" />
      <span class="text-sm text-zinc-400">正在载入世界地图...</span>
    </div>

    <!-- 异常状态（未公开或无可用服务器） -->
    <div
      v-else-if="errorMessage || publicServers.length === 0"
      class="w-full h-full flex flex-col items-center justify-center gap-4 p-6 text-center"
    >
      <div class="w-16 h-16 rounded-2xl bg-zinc-800/80 border border-zinc-700/60 flex items-center justify-center text-zinc-400">
        <server-icon size="32px" />
      </div>
      <h2 class="text-xl font-bold text-zinc-200 m-0">未开启公开地图访问</h2>
      <p class="text-sm text-zinc-400 max-w-md m-0 leading-relaxed">
        {{ errorMessage || '管理员尚未为任何服务器开启免登录公开地图访问权限。' }}
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
      <!-- 地图瓦片承载容器 -->
      <div
        class="absolute top-1/2 left-1/2 origin-center transition-transform duration-[50ms] ease-linear"
        :style="{ transform: `translate(${translateX}px, ${translateY}px) scale(${scale})` }"
      >
        <div
          v-for="tile in loadedRegions"
          :key="tile.key"
          class="absolute w-[512px] h-[512px] -ml-[256px] -mt-[256px] flex items-center justify-center bg-zinc-900/40"
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
        class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none z-10 w-5 h-5 flex items-center justify-center"
      >
        <div class="absolute w-[2px] h-4 bg-red-500 shadow-[0_0_3px_rgba(0,0,0,0.8)]"></div>
        <div class="absolute w-4 h-[2px] bg-red-500 shadow-[0_0_3px_rgba(0,0,0,0.8)]"></div>
      </div>

      <!-- 顶部控制与搜索浮动栏 -->
      <div
        class="absolute top-4 left-4 p-3 flex flex-wrap items-center gap-2 bg-zinc-900/90 backdrop-blur-md rounded-2xl border border-zinc-700/60 shadow-2xl z-20 cursor-default"
        @mousedown.stop
        @wheel.stop
      >
        <!-- 服务器实例切换（多实例时展示） -->
        <t-select
          v-if="publicServers.length > 1"
          v-model="currentServerId"
          class="!w-[140px]"
          :popup-props="{ zIndex: 9999 }"
          @change="handleServerChange"
        >
          <t-option v-for="s in publicServers" :key="s.id" :value="s.id" :label="s.name" />
        </t-select>

        <div v-if="publicServers.length > 1" class="w-px h-6 bg-zinc-700/60 mx-0.5"></div>

        <!-- 维度选择 -->
        <t-select
          v-model="currentDimension"
          :options="dimensionOptions"
          class="!w-[110px]"
          :popup-props="{ zIndex: 9999 }"
          @change="handleDimensionChange"
        />

        <div class="w-px h-6 bg-zinc-700/60 mx-0.5"></div>

        <!-- 坐标跳转 -->
        <t-input v-model="inputX" type="number" placeholder="X" class="!w-[80px]" @enter="handleSearchJump" />
        <t-input v-model="inputZ" type="number" placeholder="Z" class="!w-[80px]" @enter="handleSearchJump" />

        <t-button theme="primary" class="!rounded-xl shadow-sm" @click="handleSearchJump">
          <template #icon><location-icon /></template> 定位
        </t-button>

        <!-- 回到出生点 -->
        <t-button
          variant="outline"
          class="!rounded-xl !bg-zinc-800 !border-zinc-700 hover:!bg-zinc-700 !text-zinc-200 transition-colors"
          title="回到出生点"
          @click="jumpToSpawn"
        >
          <template #icon><home-icon /></template>
        </t-button>

        <div class="w-px h-6 bg-zinc-700/60 mx-0.5"></div>

        <!-- 导出图片 -->
        <t-button
          variant="outline"
          class="!rounded-xl !bg-zinc-800 !border-zinc-700 hover:!bg-zinc-700 !text-zinc-200 transition-colors"
          title="导出地图"
          @click="openExportDialog"
        >
          <template #icon><download-icon /></template> 导出
        </t-button>

        <!-- 全屏切换 -->
        <t-button
          variant="outline"
          class="!rounded-xl !bg-zinc-800 !border-zinc-700 hover:!bg-zinc-700 !text-zinc-200 transition-colors"
          :title="isFullscreen ? '退出全屏' : '全屏显示'"
          @click="toggleFullscreen"
        >
          <template #icon><view-list-icon /></template>
        </t-button>
      </div>

      <!-- 底部坐标与比例尺信息卡片 -->
      <div
        class="absolute bottom-4 right-4 p-4 flex flex-col font-mono text-sm bg-zinc-900/90 backdrop-blur-md rounded-2xl border border-zinc-700/60 shadow-2xl z-20 pointer-events-none min-w-[220px]"
      >
        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-400">服务器:</span>
          <span class="font-bold text-sky-400 truncate max-w-[120px]">{{ currentServerName }}</span>
        </div>

        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-400">当前维度:</span>
          <span class="font-bold text-emerald-400">{{ currentDimensionLabel }}</span>
        </div>

        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-400">方块坐标:</span>
          <span class="font-bold text-zinc-100"
            >X: {{ centerCoordinates.blockX }}, Z: {{ centerCoordinates.blockZ }}</span
          >
        </div>

        <div class="flex justify-between items-center gap-6 mb-2">
          <span class="text-xs text-zinc-400">区块区号:</span>
          <span class="font-bold text-zinc-300"
            >r.{{ centerCoordinates.regionX }}.{{ centerCoordinates.regionZ }}</span
          >
        </div>

        <div class="flex justify-between items-center gap-6 mb-3">
          <span class="text-xs text-zinc-400">当前缩放:</span>
          <span class="font-bold text-amber-400">{{ Math.round(scale * 100) }}%</span>
        </div>

        <div
          class="pt-2.5 border-t border-dashed border-zinc-700/80 text-right text-[11px] text-zinc-400 font-sans tracking-widest"
        >
          滚轮缩放 | 按住拖拽
        </div>
      </div>
    </div>

    <!-- 导出地图子弹窗 -->
    <t-dialog
      v-model:visible="exportDialogVisible"
      header="导出地图为 PNG"
      attach="body"
      width="min(500px, 92vw)"
      :confirmBtn="{
        content: '开始导出',
        theme: 'primary',
        loading: isExporting,
      }"
      :cancelBtn="{ content: '取消', disabled: isExporting }"
      @confirm="handleExportMap"
    >
      <div class="flex flex-col gap-4 py-2">
        <div class="flex items-center gap-4">
          <span class="text-sm text-[var(--td-text-color-primary)] whitespace-nowrap">导出模式</span>
          <t-radio-group v-model="exportMode" variant="default-filled">
            <t-radio-button value="loaded">当前可视范围(已加载)</t-radio-button>
            <t-radio-button value="custom">自定义区块范围</t-radio-button>
          </t-radio-group>
        </div>

        <div
          v-if="exportMode === 'custom'"
          class="flex flex-col gap-3 p-4 bg-zinc-50 dark:bg-zinc-900 rounded-lg border border-zinc-200 dark:border-zinc-800"
        >
          <div class="text-xs text-[var(--td-text-color-secondary)] mb-1">
            注意：单位为区块(Region)坐标，而非方块坐标。为防止内存溢出，单次最多导出 100 个区块。
          </div>

          <div class="flex items-center gap-2">
            <span class="shrink-0 whitespace-nowrap text-sm text-[var(--td-text-color-secondary)]">起始点：</span>
            <t-input v-model.number="exportRange.startX" type="number" placeholder="Region X" />
            <span class="text-zinc-400 shrink-0">,</span>
            <t-input v-model.number="exportRange.startZ" type="number" placeholder="Region Z" />
          </div>

          <div class="flex items-center gap-2">
            <span class="shrink-0 whitespace-nowrap text-sm text-[var(--td-text-color-secondary)]">结束点：</span>
            <t-input v-model.number="exportRange.endX" type="number" placeholder="Region X" />
            <span class="text-zinc-400 shrink-0">,</span>
            <t-input v-model.number="exportRange.endZ" type="number" placeholder="Region Z" />
          </div>
        </div>
      </div>
    </t-dialog>
  </div>
</template>

<style scoped>
.custom-grid-bg {
  background-image:
    linear-gradient(rgba(82, 82, 91, 0.25) 1px, transparent 1px),
    linear-gradient(90deg, rgba(82, 82, 91, 0.25) 1px, transparent 1px);
  background-size: 32px 32px;
}
</style>
