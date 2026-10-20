<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { LocationIcon, HomeIcon, DownloadIcon, LinkIcon, AddIcon, RemoveIcon } from 'tdesign-icons-vue-next';
import { MessagePlugin } from 'tdesign-vue-next';
import { getWorldSpawn, getTileUrl, ensureMediaToken, getMediaTokenParam } from '../api/map';

const props = defineProps<{
  serverId: number;
}>();

const visible = ref(false);
const showCoordSearch = ref(false);
const mobileHudExpanded = ref(false);

const scale = ref(1);
const translateX = ref(0);
const translateY = ref(0);
let isDragging = false;
let startX = 0;
let startY = 0;

let isTouching = false;
let isPinching = false;
let touchStartX = 0;
let touchStartY = 0;
let initialPinchDistance = 0;
let initialPinchScale = 1;
let pinchMidpointX = 0;
let pinchMidpointY = 0;

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

// 导出功能相关状态
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
  try {
    const spawn = await getWorldSpawn(props.serverId, currentDimension.value);
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
  const cx = centerCoordinates.value.regionX;
  const cz = centerCoordinates.value.regionZ;
  const missingTiles: { x: number; z: number; key: string; distance: number }[] = [];

  for (let dx = -1; dx <= 1; dx++) {
    for (let dz = -1; dz <= 1; dz++) {
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
    const src = getTileUrl(props.serverId, item.x, item.z, tokenParam, currentDimension.value);

    loadedRegions.value.push({
      x: item.x,
      z: item.z,
      key: `${currentDimension.value}_${item.key}`,
      src,
      loaded: false,
    });
  });
};

watch(
  () => visible.value,
  async (val) => {
    if (val) {
      await ensureMediaToken();
      scale.value = 1;
      loadedRegions.value = [];
      await jumpToSpawn();
    } else {
      loadedRegions.value = [];
    }
  },
);

const startDrag = (e: MouseEvent) => {
  if (isTouching) return;
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

const handleTouchStart = (e: TouchEvent) => {
  if (e.touches.length === 1) {
    isTouching = true;
    isPinching = false;
    touchStartX = e.touches[0].clientX - translateX.value;
    touchStartY = e.touches[0].clientY - translateY.value;
  } else if (e.touches.length >= 2) {
    isTouching = true;
    isPinching = true;
    const t0 = e.touches[0];
    const t1 = e.touches[1];
    initialPinchDistance = Math.hypot(t0.clientX - t1.clientX, t0.clientY - t1.clientY);
    initialPinchScale = scale.value;
    pinchMidpointX = (t0.clientX + t1.clientX) / 2;
    pinchMidpointY = (t0.clientY + t1.clientY) / 2;
  }
};

const handleTouchMove = (e: TouchEvent) => {
  if (!isTouching) return;

  if (e.touches.length === 1 && !isPinching) {
    translateX.value = e.touches[0].clientX - touchStartX;
    translateY.value = e.touches[0].clientY - touchStartY;
    updateVisibleRegions();
  } else if (e.touches.length >= 2) {
    const t0 = e.touches[0];
    const t1 = e.touches[1];
    const curDist = Math.hypot(t0.clientX - t1.clientX, t0.clientY - t1.clientY);

    if (initialPinchDistance > 0 && curDist > 0) {
      const pinchRatio = curDist / initialPinchDistance;
      const targetScale = Math.min(Math.max(0.2, initialPinchScale * pinchRatio), 5);

      if (scale.value !== targetScale) {
        const scaleRatio = targetScale / scale.value;
        translateX.value = translateX.value * scaleRatio;
        translateY.value = translateY.value * scaleRatio;
        scale.value = targetScale;
      }

      const curMidX = (t0.clientX + t1.clientX) / 2;
      const curMidY = (t0.clientY + t1.clientY) / 2;
      translateX.value += curMidX - pinchMidpointX;
      translateY.value += curMidY - pinchMidpointY;
      pinchMidpointX = curMidX;
      pinchMidpointY = curMidY;

      updateVisibleRegions();
    }
  }
};

const handleTouchEnd = (e: TouchEvent) => {
  if (e.touches.length === 1) {
    isPinching = false;
    touchStartX = e.touches[0].clientX - translateX.value;
    touchStartY = e.touches[0].clientY - translateY.value;
  } else if (e.touches.length === 0) {
    isTouching = false;
    isPinching = false;
  }
};

const zoomBy = (delta: number) => {
  const newScale = Math.min(Math.max(0.2, scale.value + delta), 5);
  if (newScale === scale.value) return;
  const scaleRatio = newScale / scale.value;
  translateX.value = translateX.value * scaleRatio;
  translateY.value = translateY.value * scaleRatio;
  scale.value = newScale;
  updateVisibleRegions();
};

const zoomIn = () => zoomBy(0.25);
const zoomOut = () => zoomBy(-0.25);

const handleScroll = (e: WheelEvent) => {
  e.preventDefault();
  const zoomDirection = e.deltaY > 0 ? -0.1 : 0.1;
  const newScale = Math.min(Math.max(0.2, scale.value + zoomDirection), 5);

  const scaleRatio = newScale / scale.value;
  translateX.value = translateX.value * scaleRatio;
  translateY.value = translateY.value * scaleRatio;
  scale.value = newScale;

  updateVisibleRegions();
};

const handleExportMap = async () => {
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
            src: getTileUrl(props.serverId, rx, rz, tokenParam, currentDimension.value),
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
    a.download = `${currentDimension.value}_map_region_${minX}_${minZ}_to_${maxX}_${maxZ}.png`;
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

const openPublicMap = () => {
  window.open(`/mslx-plugin-map-render/public?serverId=${props.serverId}`, '_blank');
};

const handleClose = () => {
  visible.value = false;
};

defineExpose({
  open: () => {
    visible.value = true;
  },
  close: () => {
    visible.value = false;
  },
});
</script>

<template>
  <div>
    <t-dialog
      attach="body"
      :visible="visible"
      header="网页地图渲染器"
      width="min(1000px, 95vw)"
      placement="center"
      :footer="false"
      @close="handleClose"
    >
      <div
        class="w-full h-[70vh] min-h-[380px] sm:min-h-[500px] relative overflow-hidden bg-zinc-50 dark:bg-zinc-950 rounded-b-xl select-none cursor-grab active:cursor-grabbing custom-grid-bg touch-none"
        style="touch-action: none;"
        @wheel="handleScroll"
        @mousedown="startDrag"
        @mousemove="onDrag"
        @mouseup="stopDrag"
        @mouseleave="stopDrag"
        @touchstart.passive="handleTouchStart"
        @touchmove.prevent="handleTouchMove"
        @touchend="handleTouchEnd"
        @touchcancel="handleTouchEnd"
      >
        <div
          class="absolute top-1/2 left-1/2 origin-center transition-transform duration-[50ms] ease-linear pointer-events-none"
          :style="{ transform: `translate(${translateX}px, ${translateY}px) scale(${scale})` }"
        >
          <div
            v-for="tile in loadedRegions"
            :key="tile.key"
            class="absolute w-[512px] h-[512px] -ml-[256px] -mt-[256px] flex items-center justify-center bg-zinc-100/50 dark:bg-zinc-900/50 pointer-events-none"
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

        <!-- 中心十字定位标记（双轴明确渲染，杜绝伪元素兼容问题） -->
        <div
          class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none z-10 w-4 h-4 flex items-center justify-center"
        >
          <div class="absolute w-[2px] h-3.5 bg-red-500 shadow-[0_0_2px_rgba(0,0,0,0.6)]"></div>
          <div class="absolute w-3.5 h-[2px] bg-red-500 shadow-[0_0_2px_rgba(0,0,0,0.6)]"></div>
        </div>

        <!-- 顶部定位与工具栏：移动端与桌面端自适应 -->
        <div
          class="absolute top-3 left-3 right-3 sm:right-auto sm:top-4 sm:left-4 p-2 sm:p-2.5 flex flex-col gap-2 bg-[var(--td-bg-color-container)] opacity-95 backdrop-blur-md rounded-xl border border-[var(--td-component-border)] shadow-md z-20 cursor-default select-none"
          @mousedown.stop
          @wheel.stop
          @touchstart.stop
          @touchmove.stop
        >
          <!-- 主功能行 -->
          <div class="flex items-center justify-between sm:justify-start gap-1.5 sm:gap-2 w-full">
            <div class="flex items-center gap-1.5 sm:gap-2 min-w-0">
              <t-select
                v-model="currentDimension"
                :options="dimensionOptions"
                class="!w-[88px] sm:!w-[115px] shrink-0"
                :popup-props="{ zIndex: 9999 }"
                @change="handleDimensionChange"
              />
            </div>

            <!-- 右侧操作组 -->
            <div class="flex items-center gap-1.5 shrink-0">
              <!-- 移动端专属坐标跳转展开按钮 -->
              <t-button
                variant="outline"
                shape="square"
                class="sm:hidden !rounded-lg"
                :class="{ '!bg-sky-50 dark:!bg-sky-950/50 !border-sky-500 !text-sky-500': showCoordSearch }"
                title="坐标跳转"
                @click="showCoordSearch = !showCoordSearch"
              >
                <template #icon><location-icon /></template>
              </t-button>

              <!-- 桌面端内联坐标跳转 -->
              <div class="hidden sm:flex items-center gap-1.5">
                <div class="w-px h-5 bg-zinc-200 dark:bg-zinc-700 mx-0.5"></div>
                <t-input v-model="inputX" type="number" placeholder="X" class="!w-[76px]" @enter="handleSearchJump" />
                <t-input v-model="inputZ" type="number" placeholder="Z" class="!w-[76px]" @enter="handleSearchJump" />
                <t-button theme="primary" class="!rounded-lg shadow-sm" @click="handleSearchJump">
                  <template #icon><location-icon /></template> 定位
                </t-button>
              </div>

              <div class="hidden sm:block w-px h-5 bg-zinc-200 dark:bg-zinc-700 mx-0.5"></div>

              <!-- 回到出生点 -->
              <t-button
                variant="outline"
                shape="square"
                class="!rounded-lg !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-600 dark:!text-zinc-300 transition-colors"
                @click="jumpToSpawn"
                title="回到出生点"
              >
                <template #icon><home-icon /></template>
              </t-button>

              <!-- 导出地图 -->
              <t-button
                variant="outline"
                class="hidden sm:inline-flex !rounded-lg !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-600 dark:!text-zinc-300 transition-colors"
                @click="openExportDialog"
                title="导出地图"
              >
                <template #icon><download-icon /></template> 导出
              </t-button>
              <t-button
                variant="outline"
                shape="square"
                class="sm:hidden !rounded-lg !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-600 dark:!text-zinc-300 transition-colors"
                @click="openExportDialog"
                title="导出地图"
              >
                <template #icon><download-icon /></template>
              </t-button>

              <!-- 打开全屏页面 -->
              <t-button
                variant="outline"
                class="hidden sm:inline-flex !rounded-lg !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-600 dark:!text-zinc-300 transition-colors"
                @click="openPublicMap"
                title="在新标签页中打开全屏公开地图"
              >
                <template #icon><link-icon /></template> 全屏页面
              </t-button>
              <t-button
                variant="outline"
                shape="square"
                class="sm:hidden !rounded-lg !bg-zinc-100 dark:!bg-zinc-800 !border-zinc-200 dark:!border-zinc-700 hover:!bg-zinc-200 dark:hover:!bg-zinc-700 !text-zinc-600 dark:!text-zinc-300 transition-colors"
                @click="openPublicMap"
                title="在新标签页中打开全屏公开地图"
              >
                <template #icon><link-icon /></template>
              </t-button>
            </div>
          </div>

          <!-- 移动端坐标跳转下拉面板 -->
          <div
            v-if="showCoordSearch"
            class="sm:hidden flex items-center gap-2 pt-2 border-t border-[var(--td-component-border)]"
          >
            <t-input v-model="inputX" type="number" placeholder="X" class="!flex-1" @enter="handleSearchJump" />
            <t-input v-model="inputZ" type="number" placeholder="Z" class="!flex-1" @enter="handleSearchJump" />
            <t-button theme="primary" class="!rounded-lg shrink-0" @click="handleSearchJump">
              <template #icon><location-icon /></template> 定位
            </t-button>
          </div>
        </div>

        <!-- 快捷缩放浮动按钮组（左下角） -->
        <div
          class="absolute bottom-4 left-4 p-1 flex flex-col gap-1 bg-[var(--td-bg-color-container)] opacity-95 backdrop-blur-md rounded-xl border border-[var(--td-component-border)] shadow-md z-20 cursor-default select-none"
          @mousedown.stop
          @wheel.stop
          @touchstart.stop
          @touchmove.stop
        >
          <t-button
            variant="text"
            shape="square"
            size="small"
            class="!rounded-lg !text-[var(--td-text-color-primary)] hover:!bg-zinc-100 dark:hover:!bg-zinc-800"
            title="放大"
            @click="zoomIn"
          >
            <template #icon><add-icon /></template>
          </t-button>
          <div class="h-px bg-zinc-200 dark:bg-zinc-700/60 mx-1"></div>
          <t-button
            variant="text"
            shape="square"
            size="small"
            class="!rounded-lg !text-[var(--td-text-color-primary)] hover:!bg-zinc-100 dark:hover:!bg-zinc-800"
            title="缩小"
            @click="zoomOut"
          >
            <template #icon><remove-icon /></template>
          </t-button>
        </div>

        <!-- 底部坐标与比例尺信息卡片：移动端为紧凑胶囊，支持点击展开 -->
        <div
          class="absolute bottom-4 right-4 p-2 sm:p-4 flex flex-col font-mono text-xs sm:text-sm bg-[var(--td-bg-color-container)] opacity-95 backdrop-blur-md rounded-xl border border-[var(--td-component-border)] shadow-md z-20 cursor-pointer sm:cursor-default max-w-[calc(100vw-5rem)] sm:min-w-[210px] select-none"
          @click="mobileHudExpanded = !mobileHudExpanded"
          @mousedown.stop
          @wheel.stop
          @touchstart.stop
          @touchmove.stop
        >
          <!-- 移动端默认紧凑单行胶囊展示 -->
          <div class="flex sm:hidden items-center justify-between gap-3 font-semibold text-xs py-0.5">
            <span class="text-[var(--td-text-color-primary)]">
              X: {{ centerCoordinates.blockX }}, Z: {{ centerCoordinates.blockZ }}
            </span>
            <span class="text-[var(--color-primary)]">{{ Math.round(scale * 100) }}%</span>
          </div>

          <!-- 详细信息：桌面端常显，移动端点击胶囊后展开 -->
          <div :class="['flex-col', mobileHudExpanded ? 'flex pt-2 mt-1 border-t border-dashed border-zinc-200 dark:border-zinc-700/60 sm:border-t-0 sm:pt-0 sm:mt-0' : 'hidden sm:flex']">
            <div class="flex justify-between items-center gap-6 mb-2">
              <span class="text-xs text-[var(--td-text-color-secondary)]">当前维度:</span>
              <span class="font-bold text-[var(--color-primary)]">{{ currentDimensionLabel }}</span>
            </div>

            <div class="hidden sm:flex justify-between items-center gap-6 mb-2">
              <span class="text-xs text-[var(--td-text-color-secondary)]">方块坐标:</span>
              <span class="font-bold text-[var(--color-primary)]">
                X: {{ centerCoordinates.blockX }}, Z: {{ centerCoordinates.blockZ }}
              </span>
            </div>

            <div class="flex justify-between items-center gap-6 mb-2">
              <span class="text-xs text-[var(--td-text-color-secondary)]">区块区号:</span>
              <span class="font-bold text-[var(--color-primary)]">
                r.{{ centerCoordinates.regionX }}.{{ centerCoordinates.regionZ }}
              </span>
            </div>

            <div class="hidden sm:flex justify-between items-center gap-6 mb-3">
              <span class="text-xs text-[var(--td-text-color-secondary)]">当前缩放:</span>
              <span class="font-bold text-[var(--color-primary)]">{{ Math.round(scale * 100) }}%</span>
            </div>

            <div
              class="pt-2 border-t border-dashed border-zinc-200/80 dark:border-zinc-700/80 text-right text-[11px] text-[var(--td-text-color-secondary)] font-sans tracking-widest"
            >
              双指缩放 | 拖拽移动
            </div>
          </div>
        </div>
      </div>
    </t-dialog>

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
    linear-gradient(rgba(161, 161, 170, 0.2) 1px, transparent 1px),
    linear-gradient(90deg, rgba(161, 161, 170, 0.2) 1px, transparent 1px);
  background-size: 32px 32px;
}

:global(html[theme-mode='dark']) .custom-grid-bg,
:global(html.dark) .custom-grid-bg {
  background-image:
    linear-gradient(rgba(82, 82, 91, 0.3) 1px, transparent 1px),
    linear-gradient(90deg, rgba(82, 82, 91, 0.3) 1px, transparent 1px);
}
</style>
