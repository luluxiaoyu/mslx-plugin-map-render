declare module 'mslx-request' {
    const request: any;
    export default request;
}

declare module 'vue-router' {
    export interface RouteLocationNormalizedLoaded {
        params: Record<string, string | string[]>;
        query: Record<string, string | string[]>;
        path: string;
        fullPath: string;
    }
    export interface Router {
        push(to: any): Promise<any>;
        replace(to: any): Promise<any>;
    }
    export function useRoute(): RouteLocationNormalizedLoaded;
    export function useRouter(): Router;
}

declare global {
    interface Window {
        MSLX_Stores: any;
        MSLX_API: any;
        mslxRequest: any;
        VueRouter: any;
    }
}

declare module '*.vue' {
    import type { DefineComponent } from 'vue';
    // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/ban-types
    const component: DefineComponent<{}, {}, any>;
    export default component;
}
