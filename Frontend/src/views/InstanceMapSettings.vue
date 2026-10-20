<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { MessagePlugin } from 'tdesign-vue-next';
import {
  LinkIcon,
  BrowseIcon,
  LockOnIcon,
  CheckCircleIcon,
  InfoCircleIcon,
} from 'tdesign-icons-vue-next';
import {
  getInstanceMapConfig,
  saveInstanceMapConfig,
  hashPassword,
} from '../api/map';

const props = defineProps<{
  serverId?: number;
  instanceId?: number;
}>();

const emits = defineEmits<{
  (e: 'success'): void;
  (e: 'saved'): void;
}>();

const currentServerId = computed(() => {
  return props.serverId || props.instanceId || 0;
});

const loading = ref(false);
const saving = ref(false);

const formData = ref<{
  enabled: boolean;
  slug: string;
  title: string;
  allowedDimensions: string[];
  passwordEnabled: boolean;
  password: string;
  hasPassword: boolean;
}>({
  enabled: false,
  slug: '',
  title: '',
  allowedDimensions: ['overworld', 'the_nether', 'the_end'],
  passwordEnabled: false,
  password: '',
  hasPassword: false,
});

const dimensionOptions = [
  { label: '主世界 (Overworld)', value: 'overworld' },
  { label: '下界 (The Nether)', value: 'the_nether' },
  { label: '末地 (The End)', value: 'the_end' },
];

const origin = computed(() => {
  return typeof window !== 'undefined' ? window.location.origin : '';
});

const fullPublicUrl = computed(() => {
  const s = formData.value.slug.trim() || `server-${currentServerId.value}`;
  return `${origin.value}/public/map/${s}`;
});

const loadConfig = async () => {
  if (!currentServerId.value) return;
  loading.value = true;
  try {
    const res: any = await getInstanceMapConfig(currentServerId.value);
    const rawDimensions = res?.allowedDimensions ?? res?.AllowedDimensions;
    const enabled = res?.enabled ?? res?.Enabled ?? false;
    const slug = res?.slug ?? res?.Slug ?? `server-${currentServerId.value}`;
    const title = res?.title ?? res?.Title ?? '';
    const passwordEnabled = res?.passwordEnabled ?? res?.PasswordEnabled ?? false;
    const hasPassword = res?.hasPassword ?? res?.HasPassword ?? false;

    formData.value = {
      enabled,
      slug,
      title,
      allowedDimensions: Array.isArray(rawDimensions) && rawDimensions.length > 0
        ? rawDimensions
        : ['overworld', 'the_nether', 'the_end'],
      passwordEnabled,
      password: '',
      hasPassword,
    };
  } catch (err: any) {
    MessagePlugin.error(err?.message || '获取实例地图配置失败');
  } finally {
    loading.value = false;
  }
};

watch(
  () => currentServerId.value,
  (val) => {
    if (val) loadConfig();
  },
  { immediate: true }
);

const copyPublicUrl = async () => {
  try {
    await navigator.clipboard.writeText(fullPublicUrl.value);
    MessagePlugin.success('访问链接已复制到剪贴板');
  } catch {
    MessagePlugin.warning('复制失败，请手动长按或选中复制链接');
  }
};

const openPublicPreview = () => {
  window.open(fullPublicUrl.value, '_blank');
};

const handleSave = async () => {
  if (!currentServerId.value) return;

  const rawSlug = formData.value.slug.trim();
  if (!/^[a-zA-Z0-9_-]{1,32}$/.test(rawSlug)) {
    MessagePlugin.warning('路径别名仅支持 1-32 位的英文字母、数字、下划线(_)或短横线(-)');
    return;
  }

  if (!formData.value.allowedDimensions || formData.value.allowedDimensions.length === 0) {
    MessagePlugin.warning('请至少选择一个开放的地图维度');
    return;
  }

  let newPasswordHash: string | null = null;
  if (formData.value.passwordEnabled) {
    if (formData.value.password.trim()) {
      newPasswordHash = await hashPassword(formData.value.password.trim());
    } else if (!formData.value.hasPassword) {
      MessagePlugin.warning('已启用密码保护，请输入访问密码');
      return;
    }
  }

  saving.value = true;
  try {
    const payload: any = {
      serverId: currentServerId.value,
      ServerId: currentServerId.value,
      enabled: formData.value.enabled,
      Enabled: formData.value.enabled,
      slug: rawSlug,
      Slug: rawSlug,
      title: formData.value.title.trim(),
      Title: formData.value.title.trim(),
      allowedDimensions: formData.value.allowedDimensions,
      AllowedDimensions: formData.value.allowedDimensions,
      passwordEnabled: formData.value.passwordEnabled,
      PasswordEnabled: formData.value.passwordEnabled,
      newPasswordHash,
      NewPasswordHash: newPasswordHash,
    };

    await saveInstanceMapConfig(currentServerId.value, payload);
    MessagePlugin.success('设置已保存');
    formData.value.password = '';
    if (newPasswordHash) {
      formData.value.hasPassword = true;
    }
    emits('success');
    emits('saved');
  } catch (err: any) {
    MessagePlugin.error(err?.message || '保存设置失败');
  } finally {
    saving.value = false;
  }
};
</script>

<template>
  <div class="instance-map-settings p-1 md:p-2 text-zinc-800 dark:text-zinc-100">
    <t-loading :loading="loading">
      <!-- 提示栏 -->
      <div class="mb-5 p-3.5 rounded-xl bg-blue-500/10 border border-blue-500/20 text-xs text-blue-600 dark:text-blue-400 flex items-start gap-2.5 leading-relaxed">
        <info-circle-icon class="text-base shrink-0 mt-0.5" />
        <div>
          提示：外部访客免登录访问前，请确认宿主插件列表中已开启本插件的公开访问权限。
        </div>
      </div>

      <!-- 板块一：公开访问与路由 -->
      <div class="flex items-center gap-2 mt-4 mb-4 pb-2 border-b border-dashed border-zinc-200/60 dark:border-zinc-700/60">
        <div class="w-1 h-4 bg-[var(--color-primary)] rounded-full"></div>
        <h2 class="text-base font-bold text-[var(--td-text-color-primary)] m-0">公开访问与路由</h2>
      </div>

      <!-- 公开访问开关 -->
      <div class="flex flex-col md:flex-row md:items-start justify-between p-3 md:p-4 border-b border-dashed border-zinc-100 dark:border-zinc-800/60 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors rounded-xl">
        <div class="flex-1 pr-0 md:pr-8 mb-3 md:mb-0 min-w-[200px]">
          <div class="text-sm font-medium text-[var(--td-text-color-primary)] leading-snug">公开访问</div>
          <div class="text-xs text-[var(--td-text-color-secondary)] mt-1 leading-relaxed">
            允许外部访客免登录查看此服务器的网页地图。
          </div>
        </div>
        <div class="w-full md:w-[340px] shrink-0 flex items-center justify-end">
          <t-switch v-model="formData.enabled" size="large" />
        </div>
      </div>

      <!-- 显示名称 -->
      <div class="flex flex-col md:flex-row md:items-start justify-between p-3 md:p-4 border-b border-dashed border-zinc-100 dark:border-zinc-800/60 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors rounded-xl">
        <div class="flex-1 pr-0 md:pr-8 mb-3 md:mb-0 min-w-[200px]">
          <div class="text-sm font-medium text-[var(--td-text-color-primary)] leading-snug">显示名称</div>
          <div class="text-xs text-[var(--td-text-color-secondary)] mt-1 leading-relaxed">
            地图顶栏及标签页显示的标题，留空则默认使用服务器名称。
          </div>
        </div>
        <div class="w-full md:w-[340px] shrink-0">
          <t-input
            v-model="formData.title"
            placeholder="留空默认使用服务器名称"
            clearable
          />
        </div>
      </div>

      <!-- 访问路径 (Slug) -->
      <div class="flex flex-col md:flex-row md:items-start justify-between p-3 md:p-4 border-b border-dashed border-zinc-100 dark:border-zinc-800/60 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors rounded-xl">
        <div class="flex-1 pr-0 md:pr-8 mb-3 md:mb-0 min-w-[200px]">
          <div class="text-sm font-medium text-[var(--td-text-color-primary)] leading-snug">访问路径</div>
          <div class="text-xs text-[var(--td-text-color-secondary)] mt-1 leading-relaxed">
            自定义 URL 后缀，仅支持字母、数字、下划线及短横线。
          </div>
        </div>
        <div class="w-full md:w-[340px] shrink-0 flex flex-col gap-2">
          <t-input
            v-model="formData.slug"
            placeholder="例如：survival"
            clearable
          >
            <template #prefix-icon>
              <span class="text-xs text-zinc-400 select-none">/public/map/</span>
            </template>
          </t-input>

          <!-- 访问预览卡片 -->
          <div class="p-2.5 rounded-lg bg-zinc-100 dark:bg-zinc-900 border border-zinc-200/70 dark:border-zinc-800 text-xs flex flex-col gap-2">
            <div class="flex items-center gap-1.5 text-zinc-400">
              <link-icon size="14px" />
              <span>访问链接预览：</span>
            </div>
            <div class="font-mono text-zinc-600 dark:text-zinc-300 break-all select-all">
              {{ fullPublicUrl }}
            </div>
            <div class="flex items-center gap-2 mt-1">
              <t-button size="small" variant="text" theme="primary" class="!px-1.5" @click="copyPublicUrl">
                <template #icon><link-icon /></template>
                复制链接
              </t-button>
              <t-button size="small" variant="text" theme="default" class="!px-1.5" @click="openPublicPreview">
                <template #icon><browse-icon /></template>
                打开预览
              </t-button>
            </div>
          </div>
        </div>
      </div>

      <!-- 板块二：开放维度设置 -->
      <div class="flex items-center gap-2 mt-8 mb-4 pb-2 border-b border-dashed border-zinc-200/60 dark:border-zinc-700/60">
        <div class="w-1 h-4 bg-[var(--color-primary)] rounded-full"></div>
        <h2 class="text-base font-bold text-[var(--td-text-color-primary)] m-0">维度设置</h2>
      </div>

      <div class="flex flex-col md:flex-row md:items-start justify-between p-3 md:p-4 border-b border-dashed border-zinc-100 dark:border-zinc-800/60 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors rounded-xl">
        <div class="flex-1 pr-0 md:pr-8 mb-3 md:mb-0 min-w-[200px]">
          <div class="text-sm font-medium text-[var(--td-text-color-primary)] leading-snug">开放维度</div>
          <div class="text-xs text-[var(--td-text-color-secondary)] mt-1 leading-relaxed">
            配置允许对外展示和加载的地图维度。
          </div>
        </div>
        <div class="w-full md:w-[340px] shrink-0">
          <t-checkbox-group
            v-model="formData.allowedDimensions"
            :options="dimensionOptions"
            class="flex flex-col gap-2.5"
          />
        </div>
      </div>

      <!-- 板块三：访问密码 -->
      <div class="flex items-center gap-2 mt-8 mb-4 pb-2 border-b border-dashed border-zinc-200/60 dark:border-zinc-700/60">
        <div class="w-1 h-4 bg-[var(--color-primary)] rounded-full"></div>
        <h2 class="text-base font-bold text-[var(--td-text-color-primary)] m-0">访问密码</h2>
      </div>

      <!-- 启用访问密码 -->
      <div class="flex flex-col md:flex-row md:items-start justify-between p-3 md:p-4 border-b border-dashed border-zinc-100 dark:border-zinc-800/60 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors rounded-xl">
        <div class="flex-1 pr-0 md:pr-8 mb-3 md:mb-0 min-w-[200px]">
          <div class="text-sm font-medium text-[var(--td-text-color-primary)] leading-snug">密码保护</div>
          <div class="text-xs text-[var(--td-text-color-secondary)] mt-1 leading-relaxed">
            开启后，访客需输入访问密码方可查看地图。
          </div>
        </div>
        <div class="w-full md:w-[340px] shrink-0 flex items-center justify-end">
          <t-switch v-model="formData.passwordEnabled" size="large" />
        </div>
      </div>

      <!-- 访问密码输入 -->
      <div
        v-if="formData.passwordEnabled"
        class="flex flex-col md:flex-row md:items-start justify-between p-3 md:p-4 border-b border-dashed border-zinc-100 dark:border-zinc-800/60 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors rounded-xl"
      >
        <div class="flex-1 pr-0 md:pr-8 mb-3 md:mb-0 min-w-[200px]">
          <div class="text-sm font-medium text-[var(--td-text-color-primary)] leading-snug">访问密码</div>
          <div class="text-xs text-[var(--td-text-color-secondary)] mt-1 leading-relaxed">
            {{ formData.hasPassword ? '已设置密码，留空表示保持当前密码。' : '设置访客访问地图时所需的密码。' }}
          </div>
        </div>
        <div class="w-full md:w-[340px] shrink-0 flex flex-col gap-1.5">
          <t-input
            v-model="formData.password"
            type="password"
            :placeholder="formData.hasPassword ? '留空保持原密码不变' : '请输入访问密码'"
            clearable
          >
            <template #prefix-icon>
              <lock-on-icon />
            </template>
          </t-input>
          <div v-if="formData.hasPassword && !formData.password" class="flex items-center gap-1 text-[11px] text-emerald-500">
            <check-circle-icon size="13px" />
            <span>已设置有效访问密码</span>
          </div>
        </div>
      </div>

      <!-- 保存操作栏 -->
      <div class="mt-8 pt-4 pb-6 flex items-center justify-end gap-3 border-t border-zinc-200/50 dark:border-zinc-800/50">
        <t-button theme="default" variant="base" @click="loadConfig">
          重置配置
        </t-button>
        <t-button theme="primary" :loading="saving" @click="handleSave">
          保存设置
        </t-button>
      </div>
    </t-loading>
  </div>
</template>

<style scoped>
.instance-map-settings {
  width: 100%;
}
</style>
