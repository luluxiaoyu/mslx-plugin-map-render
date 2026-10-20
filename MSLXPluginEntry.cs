using Microsoft.AspNetCore.Mvc.ApplicationParts;
using MSLX.SDK;

using MSLX.Plugin.MapRender.Services;

[assembly: ApplicationPart("MSLX.Plugin.MapRender")]

namespace MSLX.Plugin.MapRender;

public class MSLXPluginEntry : IPlugin
{
    public static MSLXPluginEntry Instance { get; private set; } = null!;
    public string Id => "mslx-plugin-map-render"; 
    public string Name => "网页地图渲染器";
    public string Description => "基于 Minecraft 存档 Region 文件的 2D 像素地图渲染与切片拼接导出插件";
    public string Version => "1.0.1.3";
    public string Icon => "yg.gif";
    public bool HasPublicPages => true;
    public string MinSDKVersion => "1.7.4";
    public string Developer => "xiaoyu";
    public string AuthorUrl => "https://mslx.mslmc.cn/plugin-dev/init/start/";
    public string PluginUrl => "https://github.com/MSLTeam/mslx-plugin-map-render";

    public void OnPluginInitialize(IServiceProvider serviceProvider)
    {
        Instance = this;
    }

    public void OnLoad()
    {
        SDK.MSLX.Logger.Info("网页地图渲染器插件 (mslx-plugin-map-render) 载入成功！");
    }

    public void OnUnload()
    {
        SDK.MSLX.Logger.Info("网页地图渲染器插件 (mslx-plugin-map-render) 卸载成功！");
    }

    public void OnRegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    public void OnRegisterServices(IServiceCollection services)
    {
        services.AddSingleton<MapRenderService>();
        services.AddSingleton<MapAuthService>();
        services.AddSingleton<InstanceMapConfigService>();
    }
}