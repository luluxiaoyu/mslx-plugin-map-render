using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using MSLX.Plugin.MapRender.Models;

namespace MSLX.Plugin.MapRender.Services;

public class InstanceMapConfigService
{
    private static InstanceMapConfigService? _instance;
    public static InstanceMapConfigService Instance => _instance ??= new InstanceMapConfigService();

    private readonly object _lock = new();
    private static readonly Regex SlugRegex = new("^[a-zA-Z0-9_-]{1,32}$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidDimensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "overworld",
        "the_nether",
        "the_end"
    };

    private JObject GetInstancesContainer(JObject root)
    {
        if (root.TryGetValue("Instances", StringComparison.OrdinalIgnoreCase, out var token) && token is JObject container)
        {
            return container;
        }

        var newContainer = new JObject();
        root["Instances"] = newContainer;
        return newContainer;
    }

    public InstanceMapConfig GetConfig(uint serverId)
    {
        lock (_lock)
        {
            var server = SDK.MSLX.Config.Servers.GetServer(serverId);
            var defaultTitle = server?.Name ?? $"实例 #{serverId}";
            var defaultSlug = $"server-{serverId}";

            try
            {
                var pluginConfig = SDK.MSLX.Config.GetPluginConfig("mslx-plugin-map-render");
                var root = pluginConfig.ReadConfig();
                var container = GetInstancesContainer(root);

                if (container.TryGetValue(serverId.ToString(), out var itemToken) && itemToken is JObject itemObj)
                {
                    var config = itemObj.ToObject<InstanceMapConfig>() ?? new InstanceMapConfig();
                    config.ServerId = serverId;
                    if (string.IsNullOrWhiteSpace(config.Slug)) config.Slug = defaultSlug;
                    if (string.IsNullOrWhiteSpace(config.Title)) config.Title = defaultTitle;

                    var dimsToken = itemObj["allowedDimensions"] ?? itemObj["AllowedDimensions"];
                    if (dimsToken is JArray dimsArr && dimsArr.Count > 0)
                    {
                        config.AllowedDimensions = dimsArr.Select(x => x.ToString()).ToList();
                    }
                    else if (config.AllowedDimensions == null || config.AllowedDimensions.Count == 0)
                    {
                        config.AllowedDimensions = ["overworld", "the_nether", "the_end"];
                    }
                    return config;
                }
            }
            catch
            {
                // ignore
            }

            return new InstanceMapConfig
            {
                ServerId = serverId,
                Enabled = false,
                Slug = defaultSlug,
                Title = defaultTitle,
                AllowedDimensions = ["overworld", "the_nether", "the_end"],
                PasswordEnabled = false,
                PasswordHash = string.Empty
            };
        }
    }

    public InstanceMapConfigDto GetConfigDto(uint serverId)
    {
        var config = GetConfig(serverId);
        return new InstanceMapConfigDto
        {
            ServerId = config.ServerId,
            Enabled = config.Enabled,
            Slug = config.Slug,
            Title = config.Title,
            AllowedDimensions = config.AllowedDimensions,
            PasswordEnabled = config.PasswordEnabled,
            HasPassword = !string.IsNullOrWhiteSpace(config.PasswordHash),
            NewPasswordHash = null
        };
    }

    public void SaveConfig(uint serverId, InstanceMapConfigDto dto)
    {
        lock (_lock)
        {
            var server = SDK.MSLX.Config.Servers.GetServer(serverId) ?? throw new ArgumentException("指定的实例不存在");

            var rawSlug = string.IsNullOrWhiteSpace(dto.Slug) ? $"server-{serverId}" : dto.Slug.Trim();
            if (!SlugRegex.IsMatch(rawSlug))
            {
                throw new ArgumentException("自定义路径仅允许使用英文字母、数字、下划线(_)或短横线(-)，长度为 1-32 个字符");
            }

            var pluginConfig = SDK.MSLX.Config.GetPluginConfig("mslx-plugin-map-render");
            var root = pluginConfig.ReadConfig();
            var container = GetInstancesContainer(root);

            // 检查 slug 唯一性
            foreach (var prop in container.Properties())
            {
                if (uint.TryParse(prop.Name, out var otherId) && otherId != serverId)
                {
                    if (prop.Value is JObject otherObj)
                    {
                        var otherSlug = (otherObj["slug"] ?? otherObj["Slug"])?.ToString()?.Trim();
                        if (string.Equals(otherSlug, rawSlug, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new ArgumentException($"路径别名 '{rawSlug}' 已被其他实例 (ID: {otherId}) 占用，请更换别名");
                        }
                    }
                }
            }

            var current = GetConfig(serverId);
            current.ServerId = serverId;
            current.Enabled = dto.Enabled;
            current.Slug = rawSlug;
            current.Title = string.IsNullOrWhiteSpace(dto.Title) ? server.Name : dto.Title.Trim();

            // 校验维度
            var cleanDimensions = (dto.AllowedDimensions ?? [])
                .Where(d => ValidDimensions.Contains(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanDimensions.Count == 0)
            {
                throw new ArgumentException("请至少选择一个开放查看的地图维度");
            }
            current.AllowedDimensions = cleanDimensions;

            // 密码控制
            current.PasswordEnabled = dto.PasswordEnabled;
            if (dto.PasswordEnabled)
            {
                if (!string.IsNullOrWhiteSpace(dto.NewPasswordHash))
                {
                    current.PasswordHash = dto.NewPasswordHash.Trim();
                }
                else if (string.IsNullOrWhiteSpace(current.PasswordHash))
                {
                    throw new ArgumentException("已开启密码保护，但尚未设置访问密码");
                }
            }

            container[serverId.ToString()] = JObject.FromObject(current);
            pluginConfig.WriteConfig(root);
        }
    }

    public InstanceMapConfig? GetConfigBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        lock (_lock)
        {
            try
            {
                var pluginConfig = SDK.MSLX.Config.GetPluginConfig("mslx-plugin-map-render");
                var root = pluginConfig.ReadConfig();
                var container = GetInstancesContainer(root);

                foreach (var prop in container.Properties())
                {
                    if (prop.Value is JObject obj)
                    {
                        var itemSlug = (obj["slug"] ?? obj["Slug"])?.ToString()?.Trim();
                        if (string.Equals(itemSlug, slug.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            if (uint.TryParse(prop.Name, out var serverId))
                            {
                                return GetConfig(serverId);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }
    }

    public InstanceMapConfig? GetConfigByIdOrSlug(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return null;

        var clean = identifier.Trim();
        if (uint.TryParse(clean, out var id))
        {
            var cfg = GetConfig(id);
            if (cfg.Enabled) return cfg;
        }

        return GetConfigBySlug(clean);
    }

    public List<InstanceMapConfig> GetAllPublicConfigs()
    {
        lock (_lock)
        {
            var result = new List<InstanceMapConfig>();
            var allServers = SDK.MSLX.Config.Servers.GetServerList();

            foreach (var server in allServers)
            {
                var cfg = GetConfig((uint)server.ID);
                if (cfg.Enabled)
                {
                    result.Add(cfg);
                }
            }

            return result;
        }
    }
}
