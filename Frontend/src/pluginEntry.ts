import MapRenderDialog from './views/MapRenderDialog.vue';
import './style.css';
import { MapIcon } from 'tdesign-icons-vue-next';

export const pluginConfig = {
  name: 'MSLX.Plugin.MapRender',
  version: '1.0.0',

  // 无独立整页路由，纯模态扩展
  routes: [],

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