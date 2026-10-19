using fNbt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSLX.Plugin.MapRender.Services;
using MSLX.SDK;
using MSLX.SDK.Attributes;
using MSLX.SDK.Models;

namespace MSLX.Plugin.MapRender.Controllers;

[ApiController]
[Route("api/plugin/mslx-plugin-map-render/map")]
public class MapRenderController : ControllerBase
{
    private readonly MapRenderService _renderService;

    public MapRenderController(MapRenderService? renderService = null)
    {
        _renderService = renderService ?? MapRenderService.Instance;
    }

    private static string NormalizeRelativePath(string? input, string defaultPath, string errorMessage)
    {
        var path = string.IsNullOrWhiteSpace(input) ? defaultPath : input.Trim();
        if (Path.IsPathRooted(path))
            throw new ArgumentException(errorMessage);

        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(p => p == ".."))
            throw new ArgumentException(errorMessage);

        return Path.Combine(parts.Length > 0 ? parts : [defaultPath]);
    }

    private static (bool IsSafe, string FullPath, string Message) GetSafePath(string basePath, string relativePath)
    {
        try
        {
            var fullBasePath = Path.GetFullPath(basePath);
            var combined = Path.GetFullPath(Path.Combine(fullBasePath, relativePath));
            if (!combined.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
                return (false, string.Empty, "路径越界，禁止访问实例目录之外的内容");

            return (true, combined, "路径安全");
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"非法路径: {ex.Message}");
        }
    }

    private McServerInfo.ServerInfo GetServer(uint id)
    {
        return SDK.MSLX.Config.Servers.GetServer(id) ?? throw new Exception("实例不存在");
    }

    private string GetWorldPath(McServerInfo.ServerInfo server)
    {
        var relativePath = NormalizeRelativePath(
            server.WorldPath,
            "world",
            "地图目录路径必须是实例目录内的相对路径"
        );
        var check = GetSafePath(server.Base, relativePath);
        if (!check.IsSafe) throw new ArgumentException(check.Message);
        return check.FullPath;
    }

    private string GetRegionDirectoryPath(McServerInfo.ServerInfo server, string dimension = "overworld")
    {
        var worldPath = GetWorldPath(server);

        // 1. 若配置了非默认的自定义 RegionPath，优先尝试该配置
        if (!string.IsNullOrWhiteSpace(server.RegionPath) &&
            !server.RegionPath.Equals("region", StringComparison.OrdinalIgnoreCase))
        {
            var customRelative = NormalizeRelativePath(
                server.RegionPath,
                "region",
                "Region 目录路径必须是地图目录内的相对路径"
            );
            var customCheck = GetSafePath(worldPath, customRelative);
            if (customCheck.IsSafe && Directory.Exists(customCheck.FullPath))
            {
                return customCheck.FullPath;
            }
        }

        var dimName = dimension.Contains(':') ? dimension.Split(':')[1] : dimension;
        if (string.IsNullOrWhiteSpace(dimName)) dimName = "overworld";

        var candidatePaths = new List<string>();

        if (dimName.Equals("overworld", StringComparison.OrdinalIgnoreCase))
        {
            // 新版结构: world/dimensions/minecraft/overworld/region
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", "overworld", "region"));
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", "overworld"));
            candidatePaths.Add(Path.Combine(server.Base, "dimensions", "minecraft", "overworld", "region"));
            candidatePaths.Add(Path.Combine(server.Base, "dimensions", "minecraft", "overworld"));
            // 旧版原版结构: world/region
            candidatePaths.Add(Path.Combine(worldPath, "region"));
        }
        else if (dimName.Equals("the_nether", StringComparison.OrdinalIgnoreCase) || dimName.Equals("nether", StringComparison.OrdinalIgnoreCase))
        {
            // 新版结构: world/dimensions/minecraft/the_nether/region
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", "the_nether", "region"));
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", "the_nether"));
            candidatePaths.Add(Path.Combine(server.Base, "dimensions", "minecraft", "the_nether", "region"));
            // 旧版原版结构: world/DIM-1/region
            candidatePaths.Add(Path.Combine(worldPath, "DIM-1", "region"));
            candidatePaths.Add(Path.Combine(worldPath, "DIM-1"));
            // Spigot/Paper 结构: world_nether/DIM-1/region
            candidatePaths.Add(Path.Combine(server.Base, "world_nether", "DIM-1", "region"));
        }
        else if (dimName.Equals("the_end", StringComparison.OrdinalIgnoreCase) || dimName.Equals("end", StringComparison.OrdinalIgnoreCase))
        {
            // 新版结构: world/dimensions/minecraft/the_end/region
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", "the_end", "region"));
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", "the_end"));
            candidatePaths.Add(Path.Combine(server.Base, "dimensions", "minecraft", "the_end", "region"));
            // 旧版原版结构: world/DIM1/region
            candidatePaths.Add(Path.Combine(worldPath, "DIM1", "region"));
            candidatePaths.Add(Path.Combine(worldPath, "DIM1"));
            // Spigot/Paper 结构: world_the_end/DIM1/region
            candidatePaths.Add(Path.Combine(server.Base, "world_the_end", "DIM1", "region"));
        }
        else
        {
            // 自定义/模组维度: dimensions/<namespace>/<dimension>/region
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", dimName, "region"));
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", "minecraft", dimName));
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", dimName, "region"));
            candidatePaths.Add(Path.Combine(worldPath, "dimensions", dimName));
        }

        // 优先检查哪个目录实际存在且含有 .mca 文件
        string? firstExistingDir = null;
        foreach (var path in candidatePaths)
        {
            try
            {
                var rel = Path.GetRelativePath(server.Base, path);
                var check = GetSafePath(server.Base, rel);
                if (!check.IsSafe) continue;

                if (Directory.Exists(check.FullPath))
                {
                    firstExistingDir ??= check.FullPath;
                    if (Directory.EnumerateFiles(check.FullPath, "*.mca").Any())
                    {
                        return check.FullPath;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        if (firstExistingDir != null) return firstExistingDir;

        // 若目录尚不存在，根据特征返回默认路径
        if (Directory.Exists(Path.Combine(worldPath, "dimensions", "minecraft")))
        {
            return Path.Combine(worldPath, "dimensions", "minecraft", "overworld", "region");
        }

        return Path.Combine(worldPath, "region");
    }

    private bool IsPublicAccessEnabled()
    {
        var val = SDK.MSLX.Config.Main.ReadConfigKey("Plugins:PublicAccess:mslx-plugin-map-render");
        if (val != null && bool.TryParse(val.ToString(), out var parsed)) return parsed;
        return false;
    }

    private string? GetUserIdFromRequest()
    {
        var uid = User?.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(uid)) return uid;

        string? token = Request.Headers["x-user-token"].FirstOrDefault()
                     ?? Request.Query["x-user-token"].FirstOrDefault()
                     ?? Request.Query["token"].FirstOrDefault()
                     ?? Request.Query["media_token"].FirstOrDefault();

        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            var parts = token.Split('.');
            if (parts.Length >= 2)
            {
                string payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var bytes = Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("UserId", out var prop))
                {
                    return prop.GetString();
                }
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private bool CheckAccess(uint id)
    {
        // 1. 若管理员在宿主面板开启了“公开页面访问”，直接允许免登录访问
        if (IsPublicAccessEnabled()) return true;

        // 2. 否则校验登录用户资源权限
        var userId = GetUserIdFromRequest();
        if (!string.IsNullOrEmpty(userId) && SDK.MSLX.Config.Users.HasResourcePermission(userId, "server", (int)id))
        {
            return true;
        }

        return false;
    }

    [HttpGet("public/servers")]
    [AllowAnonymous]
    public IActionResult GetPublicServers()
    {
        if (!IsPublicAccessEnabled())
        {
            return StatusCode(403, new ApiResponse<object>
            {
                Code = 403,
                Message = "管理员未开启本插件的公开访问权限"
            });
        }

        var serverList = SDK.MSLX.Config.Servers.GetServerList();
        var data = serverList.Select(s => new
        {
            id = s.ID,
            name = s.Name
        }).ToList();

        return Ok(new ApiResponse<object>
        {
            Code = 200,
            Data = data
        });
    }

    [HttpGet("spawn/{id}")]
    [AllowAnonymous]
    [AllowTokenScope(TokenScopes.Media)]
    public IActionResult GetWorldSpawn(uint id, [FromQuery] string dimension = "overworld")
    {
        if (!CheckAccess(id))
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到指定实例或无权访问" });

        try
        {
            var server = GetServer(id);
            var worldPath = GetWorldPath(server);

            var levelDatPath = Path.Combine(worldPath, "level.dat");
            if (!System.IO.File.Exists(levelDatPath))
            {
                var altLevelDat = Path.Combine(worldPath, "dimensions", "minecraft", "overworld", "level.dat");
                if (System.IO.File.Exists(altLevelDat)) levelDatPath = altLevelDat;
            }

            if (!System.IO.File.Exists(levelDatPath))
                return Ok(new ApiResponse<object> { Code = 200, Data = new { x = 0, z = 0 } });

            using var fs = new FileStream(levelDatPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var nbtFile = new NbtFile();
            nbtFile.LoadFromStream(fs, NbtCompression.AutoDetect);

            var dataTag = nbtFile.RootTag.Get<NbtCompound>("Data");
            int spawnX = dataTag?.Get<NbtInt>("SpawnX")?.Value ?? 0;
            int spawnZ = dataTag?.Get<NbtInt>("SpawnZ")?.Value ?? 0;

            return Ok(new ApiResponse<object> { Code = 200, Data = new { x = spawnX, z = spawnZ } });
        }
        catch (Exception e)
        {
            return BadRequest(new ApiResponse<object> { Code = 400, Message = e.Message });
        }
    }

    [HttpGet("{id}/{regionX}/{regionZ}")]
    [AllowAnonymous]
    [AllowTokenScope(TokenScopes.Media)]
    public IActionResult GetRegionMap(uint id, int regionX = 0, int regionZ = 0, [FromQuery] string dimension = "overworld")
    {
        if (!CheckAccess(id))
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到指定实例或无权访问" });

        try
        {
            var server = GetServer(id);
            var regionDirectoryPath = GetRegionDirectoryPath(server, dimension);
            var mcaFilePath = Path.Combine(regionDirectoryPath, $"r.{regionX}.{regionZ}.mca");

            var pngBytes = _renderService.RenderRegionToPng(mcaFilePath, dimension);
            return File(pngBytes, "image/png");
        }
        catch (Exception e)
        {
            return BadRequest(new { Code = 400, Message = e.Message });
        }
    }
}
