import MapRenderDialog from './views/MapRenderDialog.vue';
import PublicMapView from './views/PublicMapView.vue';
import InstanceMapSettings from './views/InstanceMapSettings.vue';
import './style.css';
import { MapIcon } from 'tdesign-icons-vue-next';

export const pluginConfig = {
  name: 'MSLX.Plugin.MapRender',
  version: '1.0.1.0',

  // 免登录全屏公开页面路由
  routes: [
    {
      path: '/mslx-plugin-map-render/public',
      name: 'MslxPluginMapRenderPublic',
      component: PublicMapView,
      meta: {
        title: '服务器世界地图',
        anonymous: true,
        hidden: true,
      },
    },
    {
      path: '/mslx-plugin-map-render/public/:slug',
      name: 'MslxPluginMapRenderPublicSlug',
      component: PublicMapView,
      meta: {
        title: '服务器世界地图',
        anonymous: true,
        hidden: true,
      },
    },
    {
      path: '/public/map',
      name: 'PublicMapRoot',
      component: PublicMapView,
      meta: {
        title: '服务器世界地图',
        anonymous: true,
        hidden: true,
      },
    },
    {
      path: '/public/map/:slug',
      name: 'PublicMapSlug',
      component: PublicMapView,
      meta: {
        title: '服务器世界地图',
        anonymous: true,
        hidden: true,
      },
    },
  ],

  // UI 扩展插槽注入
  extensions: [
    // 注入到实例控制台“更多功能”下拉菜单
    {
      slot: 'instance-console-dropdown',
      component: MapRenderDialog,
      label: '网页地图渲染器',
      icon: MapIcon,
    },
    // 注入到实例配置弹窗左侧 Tab 导航
    {
      slot: 'instance-settings-tab',
      component: InstanceMapSettings,
      label: '地图设置',
      icon: MapIcon,
    },
  ],
};
