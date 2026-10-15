using fNbt;
using Microsoft.AspNetCore.Mvc;
using MSLX.SDK;
using MSLX.SDK.Attributes;
using MSLX.SDK.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;

namespace MSLX.Plugin.MapRender.Controllers;

[ApiController]
[Route("api/plugin/mslx-plugin-map-render/map")]
public class MapRenderController : ControllerBase
{
    private static readonly Dictionary<string, Rgba32> BlockColorMap = new()
    {
        // ================= 💧 水与熔岩 =================
        { "minecraft:water", new Rgba32(35, 137, 218) },
        { "minecraft:lava", new Rgba32(230, 90, 20) },

        // ================= 🌍 基础地表与土壤 =================
        { "minecraft:grass_block", new Rgba32(121, 192, 90) },
        { "minecraft:dirt", new Rgba32(150, 108, 74) },
        { "minecraft:coarse_dirt", new Rgba32(119, 85, 59) },
        { "minecraft:podzol", new Rgba32(90, 60, 20) },
        { "minecraft:mycelium", new Rgba32(111, 99, 105) },
        { "minecraft:mud", new Rgba32(60, 57, 61) },
        { "minecraft:dirt_path", new Rgba32(150, 125, 90) },
        { "minecraft:farmland", new Rgba32(110, 70, 40) },

        // ================= 🏜️ 沙漠与恶地 =================
        { "minecraft:sand", new Rgba32(238, 214, 175) },
        { "minecraft:red_sand", new Rgba32(210, 117, 52) },
        { "minecraft:sandstone", new Rgba32(223, 201, 138) },
        { "minecraft:cut_sandstone", new Rgba32(223, 201, 138) },
        { "minecraft:smooth_sandstone", new Rgba32(223, 201, 138) },
        { "minecraft:chiseled_sandstone", new Rgba32(223, 201, 138) },
        { "minecraft:terracotta", new Rgba32(152, 94, 67) },
        { "minecraft:cactus", new Rgba32(85, 125, 45) },
        { "minecraft:dead_bush", new Rgba32(143, 100, 48) },

        // ================= 🪨 岩石与矿石 =================
        { "minecraft:stone", new Rgba32(125, 125, 125) },
        { "minecraft:smooth_stone", new Rgba32(125, 125, 125) },
        { "minecraft:cobblestone", new Rgba32(100, 100, 100) },
        { "minecraft:mossy_cobblestone", new Rgba32(90, 110, 90) },
        { "minecraft:stone_bricks", new Rgba32(110, 110, 110) },
        { "minecraft:deepslate", new Rgba32(80, 80, 85) },
        { "minecraft:cobbled_deepslate", new Rgba32(70, 70, 75) },
        { "minecraft:tuff", new Rgba32(108, 113, 112) },
        { "minecraft:granite", new Rgba32(150, 110, 100) },
        { "minecraft:diorite", new Rgba32(180, 180, 185) },
        { "minecraft:andesite", new Rgba32(130, 130, 135) },
        { "minecraft:gravel", new Rgba32(130, 130, 130) },
        { "minecraft:clay", new Rgba32(160, 166, 179) },
        { "minecraft:obsidian", new Rgba32(20, 15, 30) },

        // ================= ❄️ 冰雪地貌 =================
        { "minecraft:snow_block", new Rgba32(240, 248, 255) },
        { "minecraft:snow", new Rgba32(245, 250, 255) },
        { "minecraft:ice", new Rgba32(160, 200, 255) },
        { "minecraft:packed_ice", new Rgba32(140, 180, 245) },
        { "minecraft:blue_ice", new Rgba32(115, 165, 255) },

        // ================= 🌲 树木与植物 (各种木材) =================
        // 橡木
        { "minecraft:oak_leaves", new Rgba32(60, 120, 40) },
        { "minecraft:oak_log", new Rgba32(107, 85, 52) },
        { "minecraft:oak_wood", new Rgba32(107, 85, 52) },
        { "minecraft:oak_planks", new Rgba32(162, 130, 78) },
        // 白桦
        { "minecraft:birch_leaves", new Rgba32(95, 140, 60) },
        { "minecraft:birch_log", new Rgba32(215, 215, 210) },
        { "minecraft:birch_planks", new Rgba32(196, 178, 126) },
        // 云杉
        { "minecraft:spruce_leaves", new Rgba32(45, 85, 45) },
        { "minecraft:spruce_log", new Rgba32(59, 40, 22) },
        { "minecraft:spruce_planks", new Rgba32(114, 84, 56) },
        // 丛林木
        { "minecraft:jungle_leaves", new Rgba32(50, 135, 30) },
        { "minecraft:jungle_log", new Rgba32(86, 66, 27) },
        { "minecraft:jungle_planks", new Rgba32(160, 115, 80) },
        // 金合欢
        { "minecraft:acacia_leaves", new Rgba32(85, 130, 35) },
        { "minecraft:acacia_log", new Rgba32(103, 96, 86) },
        { "minecraft:acacia_planks", new Rgba32(168, 90, 50) },
        // 深色橡木
        { "minecraft:dark_oak_leaves", new Rgba32(40, 90, 25) },
        { "minecraft:dark_oak_log", new Rgba32(45, 30, 15) },
        { "minecraft:dark_oak_planks", new Rgba32(66, 43, 20) },
        // 红树木
        { "minecraft:mangrove_leaves", new Rgba32(75, 110, 30) },
        { "minecraft:mangrove_log", new Rgba32(84, 40, 34) },
        { "minecraft:mangrove_roots", new Rgba32(70, 50, 35) },
        { "minecraft:muddy_mangrove_roots", new Rgba32(65, 55, 45) },
        { "minecraft:mangrove_planks", new Rgba32(117, 54, 48) },
        // 樱花木
        { "minecraft:cherry_leaves", new Rgba32(230, 150, 170) },
        { "minecraft:cherry_log", new Rgba32(53, 35, 43) },
        { "minecraft:cherry_planks", new Rgba32(226, 178, 174) },

        // ================= 🌿 植被与杂项 =================
        { "minecraft:short_grass", new Rgba32(80, 140, 50) },
        { "minecraft:tall_grass", new Rgba32(70, 130, 45) },
        { "minecraft:fern", new Rgba32(75, 135, 45) },
        { "minecraft:dandelion", new Rgba32(255, 235, 60) },
        { "minecraft:poppy", new Rgba32(220, 40, 40) },
        { "minecraft:lily_pad", new Rgba32(30, 90, 40) },
        { "minecraft:seagrass", new Rgba32(40, 120, 60) },
        { "minecraft:kelp_plant", new Rgba32(45, 105, 45) },
        { "minecraft:kelp", new Rgba32(45, 105, 45) },

        // ================= 🏛️ 常见建筑与人造方块 =================
        { "minecraft:glass", new Rgba32(200, 220, 230) },
        { "minecraft:glass_pane", new Rgba32(200, 220, 230) },
        { "minecraft:white_wool", new Rgba32(235, 235, 235) },
        { "minecraft:bricks", new Rgba32(150, 70, 55) },
        { "minecraft:iron_block", new Rgba32(200, 200, 200) }
    };

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

    private string GetRegionDirectoryPath(McServerInfo.ServerInfo server)
    {
        var worldPath = GetWorldPath(server);
        var relativePath = NormalizeRelativePath(
            server.RegionPath,
            "region",
            "Region 目录路径必须是地图目录内的相对路径"
        );
        var check = GetSafePath(worldPath, relativePath);
        if (!check.IsSafe) throw new ArgumentException(check.Message);
        return check.FullPath;
    }

    [HttpGet("spawn/{id}")]
    [AllowTokenScope(TokenScopes.Media)]
    public IActionResult GetWorldSpawn(uint id)
    {
        var userId = User?.FindFirst("UserId")?.Value ?? "";
        if (!SDK.MSLX.Config.Users.HasResourcePermission(userId, "server", (int)id))
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到指定实例或无权访问" });

        try
        {
            var server = GetServer(id);
            var worldPath = GetWorldPath(server);
            var levelDatPath = Path.Combine(worldPath, "level.dat");

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
    [AllowTokenScope(TokenScopes.Media)]
    public IActionResult GetRegionMap(uint id, int regionX = 0, int regionZ = 0)
    {
        var userId = User?.FindFirst("UserId")?.Value ?? "";
        if (!SDK.MSLX.Config.Users.HasResourcePermission(userId, "server", (int)id))
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到指定实例或无权访问" });

        try
        {
            var server = GetServer(id);
            var regionDirectoryPath = GetRegionDirectoryPath(server);
            var mcaFilePath = Path.Combine(regionDirectoryPath, $"r.{regionX}.{regionZ}.mca");

            using var image = new Image<Rgba32>(512, 512);
            if (!System.IO.File.Exists(mcaFilePath)) return OutputImage(image);

            var parser = new McaParser(mcaFilePath);

            // 初始化环境记忆色：默认给个草地绿，防止第一格就不认识
            Rgba32 lastKnownColor = new Rgba32(121, 192, 90);

            for (int cx = 0; cx < 32; cx++)
            {
                for (int cz = 0; cz < 32; cz++)
                {
                    var chunkTag = parser.GetChunkNbt(cx, cz);
                    if (chunkTag == null) continue;

                    var level = chunkTag.Get<NbtCompound>("Level") ?? chunkTag;

                    var heightmaps = level.Get<NbtCompound>("Heightmaps");
                    if (heightmaps == null) continue;

                    var motionData = heightmaps.Get<NbtLongArray>("MOTION_BLOCKING")?.Value;
                    var floorData = heightmaps.Get<NbtLongArray>("OCEAN_FLOOR")?.Value;
                    if (motionData == null || floorData == null) continue;

                    int[] topHeights = UnpackHeightmap(motionData);
                    int[] floorHeights = UnpackHeightmap(floorData);

                    var sections = level.Get<NbtList>("sections") ?? level.Get<NbtList>("Sections");
                    if (sections == null) continue;

                    for (int x = 0; x < 16; x++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            int idx = z * 16 + x;
                            int topY = topHeights[idx] - 65;
                            int floorY = floorHeights[idx] - 65;

                            string blockName = GetBlockNameAtY(sections, x, topY, z);

                            Rgba32 baseColor;
                            bool isWater = blockName == "minecraft:water";

                            if (isWater)
                            {
                                int waterDepth = Math.Max(1, topY - floorY);
                                baseColor = new Rgba32(
                                    (byte)Math.Max(15, 35 - waterDepth * 2),
                                    (byte)Math.Max(60, 137 - waterDepth * 3),
                                    (byte)Math.Max(120, 218 - waterDepth),
                                    255
                                );
                            }
                            else if (BlockColorMap.TryGetValue(blockName, out var mappedColor))
                            {
                                baseColor = mappedColor;
                                lastKnownColor = mappedColor;
                            }
                            else
                            {
                                baseColor = new Rgba32(
                                    (byte)Math.Clamp(lastKnownColor.R + 5, 0, 255),
                                    (byte)Math.Clamp(lastKnownColor.G + 5, 0, 255),
                                    (byte)Math.Clamp(lastKnownColor.B + 5, 0, 255),
                                    lastKnownColor.A
                                );
                            }

                            // 伪 3D 山体阴影
                            int northY = (z > 0) ? (topHeights[idx - 16] - 65) : topY;
                            int diff = topY - northY;
                            Rgba32 finalColor = isWater ? baseColor : ApplyHillshading(baseColor, diff);

                            int pixelX = cx * 16 + x;
                            int pixelZ = cz * 16 + z;
                            image[pixelX, pixelZ] = finalColor;
                        }
                    }
                }
            }
            return OutputImage(image);
        }
        catch (Exception e)
        {
            return BadRequest(new { Code = 400, Message = e.Message });
        }
    }

    private IActionResult OutputImage(Image image)
    {
        var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return File(ms.ToArray(), "image/png");
    }

    private static Rgba32 ApplyHillshading(Rgba32 color, int diff)
    {
        if (diff == 0) return color;
        float factor = 1.0f + Math.Clamp(diff * 0.05f, -0.2f, 0.2f);

        byte r = (byte)Math.Clamp(color.R * factor, 0, 255);
        byte g = (byte)Math.Clamp(color.G * factor, 0, 255);
        byte b = (byte)Math.Clamp(color.B * factor, 0, 255);
        return new Rgba32(r, g, b, color.A);
    }

    private static int[] UnpackHeightmap(long[] data)
    {
        int[] heights = new int[256];
        int bitsPerEntry = 9;
        int entriesPerLong = 64 / bitsPerEntry;
        long mask = (1L << bitsPerEntry) - 1;

        int index = 0;
        foreach (long value in data)
        {
            long temp = value;
            for (int j = 0; j < entriesPerLong && index < 256; j++)
            {
                heights[index++] = (int)(temp & mask);
                temp >>= bitsPerEntry;
            }
        }
        return heights;
    }

    private static string GetBlockNameAtY(NbtList sections, int x, int y, int z)
    {
        int sectionY = y >> 4;

        foreach (NbtCompound sec in sections)
        {
            var yTag = sec.Get<NbtByte>("Y")?.Value;
            if (yTag != sectionY) continue;

            var blockStates = sec.Get<NbtCompound>("block_states");
            if (blockStates == null) return "minecraft:air";

            var palette = blockStates.Get<NbtList>("palette");
            if (palette == null) return "minecraft:air";

            var data = blockStates.Get<NbtLongArray>("data")?.Value;

            if (data == null || data.Length == 0)
            {
                var singleBlock = palette[0] as NbtCompound;
                return singleBlock?.Get<NbtString>("Name")?.Value ?? "minecraft:air";
            }

            int localY = y & 15;
            int blockIndex = (localY * 256) + (z * 16) + x;

            int paletteSize = palette.Count;
            int bitsPerEntry = Math.Max(4, (int)Math.Ceiling(Math.Log2(paletteSize)));
            int entriesPerLong = 64 / bitsPerEntry;

            int longIndex = blockIndex / entriesPerLong;
            int startBit = (blockIndex % entriesPerLong) * bitsPerEntry;

            if (longIndex >= data.Length) return "minecraft:air";

            long val = data[longIndex];
            long mask = (1L << bitsPerEntry) - 1;
            int paletteIndex = (int)((val >> startBit) & mask);

            if (paletteIndex >= paletteSize) return "minecraft:air";

            var targetBlock = palette[paletteIndex] as NbtCompound;
            return targetBlock?.Get<NbtString>("Name")?.Value ?? "minecraft:air";
        }
        return "minecraft:air";
    }

    private class McaParser
    {
        private readonly byte[]? _fileData;

        public McaParser(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            _fileData = ms.ToArray();
        }

        public NbtCompound? GetChunkNbt(int localX, int localZ)
        {
            if (_fileData == null || _fileData.Length < 8192) return null;

            int offsetLocation = 4 * ((localX & 31) + (localZ & 31) * 32);
            if (offsetLocation + 3 >= _fileData.Length) return null;

            int offset = (_fileData[offsetLocation] << 16) | (_fileData[offsetLocation + 1] << 8) | _fileData[offsetLocation + 2];
            int sectors = _fileData[offsetLocation + 3];

            if (offset == 0 && sectors == 0) return null;

            int dataOffset = offset * 4096;
            if (dataOffset + 4 >= _fileData.Length) return null;

            int length = (_fileData[dataOffset] << 24) | (_fileData[dataOffset + 1] << 16) | (_fileData[dataOffset + 2] << 8) | _fileData[dataOffset + 3];
            byte compressionType = _fileData[dataOffset + 4];

            if (dataOffset + 5 + length - 1 > _fileData.Length) return null;

            using var ms = new MemoryStream(_fileData, dataOffset + 5, length - 1);
            Stream decompressedStream;

            if (compressionType == 1) decompressedStream = new GZipStream(ms, CompressionMode.Decompress);
            else if (compressionType == 2) decompressedStream = new ZLibStream(ms, CompressionMode.Decompress);
            else decompressedStream = ms;

            try
            {
                var nbtFile = new NbtFile();
                nbtFile.LoadFromStream(decompressedStream, NbtCompression.None);
                return nbtFile.RootTag;
            }
            catch
            {
                return null;
            }
        }
    }
}
