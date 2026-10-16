using System.IO.Compression;
using fNbt;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MSLX.Plugin.MapRender.Services;

public class MapRenderService
{
    public static MapRenderService Instance { get; } = new();

    public static readonly Dictionary<string, Rgba32> BlockColorMap = new()
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
        { "minecraft:crying_obsidian", new Rgba32(35, 10, 50) },

        // ================= ❄️ 冰雪地貌 =================
        { "minecraft:snow_block", new Rgba32(240, 248, 255) },
        { "minecraft:snow", new Rgba32(245, 250, 255) },
        { "minecraft:ice", new Rgba32(160, 200, 255) },
        { "minecraft:packed_ice", new Rgba32(140, 180, 245) },
        { "minecraft:blue_ice", new Rgba32(115, 165, 255) },

        // ================= 🌲 树木与植物 =================
        { "minecraft:oak_leaves", new Rgba32(60, 120, 40) },
        { "minecraft:oak_log", new Rgba32(107, 85, 52) },
        { "minecraft:oak_wood", new Rgba32(107, 85, 52) },
        { "minecraft:oak_planks", new Rgba32(162, 130, 78) },
        { "minecraft:birch_leaves", new Rgba32(95, 140, 60) },
        { "minecraft:birch_log", new Rgba32(215, 215, 210) },
        { "minecraft:birch_planks", new Rgba32(196, 178, 126) },
        { "minecraft:spruce_leaves", new Rgba32(45, 85, 45) },
        { "minecraft:spruce_log", new Rgba32(59, 40, 22) },
        { "minecraft:spruce_planks", new Rgba32(114, 84, 56) },
        { "minecraft:jungle_leaves", new Rgba32(50, 135, 30) },
        { "minecraft:jungle_log", new Rgba32(86, 66, 27) },
        { "minecraft:jungle_planks", new Rgba32(160, 115, 80) },
        { "minecraft:acacia_leaves", new Rgba32(85, 130, 35) },
        { "minecraft:acacia_log", new Rgba32(103, 96, 86) },
        { "minecraft:acacia_planks", new Rgba32(168, 90, 50) },
        { "minecraft:dark_oak_leaves", new Rgba32(40, 90, 25) },
        { "minecraft:dark_oak_log", new Rgba32(45, 30, 15) },
        { "minecraft:dark_oak_planks", new Rgba32(66, 43, 20) },
        { "minecraft:mangrove_leaves", new Rgba32(75, 110, 30) },
        { "minecraft:mangrove_log", new Rgba32(84, 40, 34) },
        { "minecraft:mangrove_roots", new Rgba32(70, 50, 35) },
        { "minecraft:muddy_mangrove_roots", new Rgba32(65, 55, 45) },
        { "minecraft:mangrove_planks", new Rgba32(117, 54, 48) },
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
        { "minecraft:iron_block", new Rgba32(200, 200, 200) },

        // ================= 🔥 下界 (Nether) =================
        { "minecraft:netherrack", new Rgba32(111, 27, 27) },
        { "minecraft:soul_sand", new Rgba32(84, 64, 51) },
        { "minecraft:soul_soil", new Rgba32(75, 57, 46) },
        { "minecraft:crimson_nylium", new Rgba32(189, 48, 49) },
        { "minecraft:warped_nylium", new Rgba32(43, 114, 101) },
        { "minecraft:basalt", new Rgba32(80, 81, 86) },
        { "minecraft:polished_basalt", new Rgba32(100, 101, 106) },
        { "minecraft:blackstone", new Rgba32(42, 38, 46) },
        { "minecraft:glowstone", new Rgba32(238, 187, 85) },
        { "minecraft:magma_block", new Rgba32(143, 62, 27) },
        { "minecraft:nether_bricks", new Rgba32(44, 21, 26) },
        { "minecraft:red_nether_bricks", new Rgba32(69, 7, 9) },
        { "minecraft:warped_wart_block", new Rgba32(20, 180, 133) },
        { "minecraft:nether_wart_block", new Rgba32(114, 2, 2) },
        { "minecraft:shroomlight", new Rgba32(245, 145, 67) },
        { "minecraft:crimson_stem", new Rgba32(101, 28, 48) },
        { "minecraft:warped_stem", new Rgba32(58, 142, 140) },
        { "minecraft:crimson_planks", new Rgba32(101, 48, 70) },
        { "minecraft:warped_planks", new Rgba32(43, 104, 99) },
        { "minecraft:bone_block", new Rgba32(227, 220, 196) },
        { "minecraft:ancient_debris", new Rgba32(94, 66, 58) },

        // ================= 🌌 末地 (The End) =================
        { "minecraft:end_stone", new Rgba32(223, 230, 160) },
        { "minecraft:end_stone_bricks", new Rgba32(221, 228, 163) },
        { "minecraft:purpur_block", new Rgba32(169, 125, 169) },
        { "minecraft:purpur_pillar", new Rgba32(171, 129, 171) },
        { "minecraft:chorus_plant", new Rgba32(88, 58, 88) },
        { "minecraft:chorus_flower", new Rgba32(160, 130, 160) },
        { "minecraft:end_rod", new Rgba32(255, 255, 255) }
    };

    public Image<Rgba32> RenderRegion(string mcaFilePath, string dimension = "overworld")
    {
        var image = new Image<Rgba32>(512, 512);
        if (!File.Exists(mcaFilePath)) return image;

        var parser = new McaParser(mcaFilePath);
        var dimName = dimension.Contains(':') ? dimension.Split(':')[1] : dimension;
        if (string.IsNullOrWhiteSpace(dimName)) dimName = "overworld";

        if (dimName.Equals("the_nether", StringComparison.OrdinalIgnoreCase) || dimName.Equals("nether", StringComparison.OrdinalIgnoreCase))
        {
            RenderNether(parser, image);
        }
        else if (dimName.Equals("the_end", StringComparison.OrdinalIgnoreCase) || dimName.Equals("end", StringComparison.OrdinalIgnoreCase))
        {
            RenderTheEnd(parser, image);
        }
        else
        {
            RenderOverworld(parser, image);
        }

        return image;
    }

    public byte[] RenderRegionToPng(string mcaFilePath, string dimension = "overworld")
    {
        using var image = RenderRegion(mcaFilePath, dimension);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private void RenderOverworld(McaParser parser, Image<Rgba32> image)
    {
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

                // 计算当前版本的 Y 轴基础偏移 (1.18+ 为 -64..319, 旧版为 0..256)
                int minSectionY = 0;
                var yPosTag = level.Get<NbtInt>("yPos");
                if (yPosTag != null)
                {
                    minSectionY = yPosTag.Value;
                }
                else if (sections.Count > 0)
                {
                    minSectionY = sections.OfType<NbtCompound>()
                        .Select(s => (int)(sbyte)(s.Get<NbtByte>("Y")?.Value ?? 0))
                        .DefaultIfEmpty(0)
                        .Min();
                }
                int minY = minSectionY < 0 ? minSectionY * 16 : 0;
                int heightOffset = -minY + 1;

                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int idx = z * 16 + x;
                        int topY = topHeights[idx] - heightOffset;
                        int floorY = floorHeights[idx] - heightOffset;

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
                        int northY = (z > 0) ? (topHeights[idx - 16] - heightOffset) : topY;
                        int diff = topY - northY;
                        Rgba32 finalColor = isWater ? baseColor : ApplyHillshading(baseColor, diff);

                        int pixelX = cx * 16 + x;
                        int pixelZ = cz * 16 + z;
                        image[pixelX, pixelZ] = finalColor;
                    }
                }
            }
        }
    }

    private void RenderNether(McaParser parser, Image<Rgba32> image)
    {
        Rgba32 lastKnownColor = new Rgba32(111, 27, 27); // 默认地狱岩红

        for (int cx = 0; cx < 32; cx++)
        {
            for (int cz = 0; cz < 32; cz++)
            {
                var chunkTag = parser.GetChunkNbt(cx, cz);
                if (chunkTag == null) continue;

                var level = chunkTag.Get<NbtCompound>("Level") ?? chunkTag;
                var sections = level.Get<NbtList>("sections") ?? level.Get<NbtList>("Sections");
                if (sections == null || sections.Count == 0) continue;

                // 下界顶部有基岩天花板，从基岩下方(Y=120)向下扫描至第一个非空气方块
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        string topBlock = "minecraft:air";
                        int topY = 0;

                        for (int y = 120; y >= 10; y--)
                        {
                            var b = GetBlockNameAtY(sections, x, y, z);
                            if (b != "minecraft:air" && b != "minecraft:cave_air")
                            {
                                topBlock = b;
                                topY = y;
                                break;
                            }
                        }

                        Rgba32 baseColor;
                        bool isLava = topBlock == "minecraft:lava";

                        if (isLava)
                        {
                            baseColor = new Rgba32(230, 90, 20);
                        }
                        else if (BlockColorMap.TryGetValue(topBlock, out var mappedColor))
                        {
                            baseColor = mappedColor;
                            lastKnownColor = mappedColor;
                        }
                        else
                        {
                            baseColor = lastKnownColor;
                        }

                        int diff = (topY % 4) - 2;
                        Rgba32 finalColor = isLava ? baseColor : ApplyHillshading(baseColor, diff);

                        int pixelX = cx * 16 + x;
                        int pixelZ = cz * 16 + z;
                        image[pixelX, pixelZ] = finalColor;
                    }
                }
            }
        }
    }

    private void RenderTheEnd(McaParser parser, Image<Rgba32> image)
    {
        Rgba32 lastKnownColor = new Rgba32(223, 230, 160); // 末地石黄白

        for (int cx = 0; cx < 32; cx++)
        {
            for (int cz = 0; cz < 32; cz++)
            {
                var chunkTag = parser.GetChunkNbt(cx, cz);
                if (chunkTag == null) continue;

                var level = chunkTag.Get<NbtCompound>("Level") ?? chunkTag;
                var heightmaps = level.Get<NbtCompound>("Heightmaps");
                var sections = level.Get<NbtList>("sections") ?? level.Get<NbtList>("Sections");
                if (sections == null) continue;

                var motionData = heightmaps?.Get<NbtLongArray>("MOTION_BLOCKING")?.Value;
                int[]? topHeights = motionData != null ? UnpackHeightmap(motionData) : null;

                int minSectionY = 0;
                var yPosTag = level.Get<NbtInt>("yPos");
                if (yPosTag != null) minSectionY = yPosTag.Value;
                int minY = minSectionY < 0 ? minSectionY * 16 : 0;
                int heightOffset = -minY + 1;

                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int idx = z * 16 + x;
                        int topY = topHeights != null ? topHeights[idx] - heightOffset : 60;
                        string blockName = GetBlockNameAtY(sections, x, topY, z);

                        if (blockName == "minecraft:air" || blockName == "minecraft:cave_air")
                        {
                            continue;
                        }

                        Rgba32 baseColor;
                        if (BlockColorMap.TryGetValue(blockName, out var mappedColor))
                        {
                            baseColor = mappedColor;
                            lastKnownColor = mappedColor;
                        }
                        else
                        {
                            baseColor = lastKnownColor;
                        }

                        int northY = (z > 0 && topHeights != null) ? (topHeights[idx - 16] - heightOffset) : topY;
                        int diff = topY - northY;
                        Rgba32 finalColor = ApplyHillshading(baseColor, diff);

                        int pixelX = cx * 16 + x;
                        int pixelZ = cz * 16 + z;
                        image[pixelX, pixelZ] = finalColor;
                    }
                }
            }
        }
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
            var rawY = sec.Get<NbtByte>("Y")?.Value;
            if (rawY == null) continue;
            if ((sbyte)rawY.Value != sectionY) continue;

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
