import MapRenderDialog from './views/MapRenderDialog.vue';
import PublicMapView from './views/PublicMapView.vue';
import './style.css';
import { MapIcon } from 'tdesign-icons-vue-next';

export const pluginConfig = {
  name: 'MSLX.Plugin.MapRender',
  version: '1.0.0.6',

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
      path: '/public/map',
      name: 'PublicMapAlias',
      component: PublicMapView,
      meta: {
        title: '服务器世界地图',
        anonymous: true,
        hidden: true,
      },
    },
  ],

  // 注入到实例控制台“更多功能”下拉菜单
  extensions: [
    {
      slot: 'instance-console-dropdown',
      component: MapRenderDialog,
      label: '网页地图渲染器',
      icon: MapIcon,
    },
  ],
};