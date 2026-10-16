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
        // ================= 水与熔岩 (Minecraft Wiki 基础地图色) =================
        { "minecraft:water", new Rgba32(64, 64, 255) },                 // WATER (64, 64, 255)
        { "minecraft:lava", new Rgba32(255, 68, 0) },                   // FIRE / LAVA (高亮熔岩橙红)

        // ================= 基础地表与土壤 =================
        { "minecraft:grass_block", new Rgba32(127, 178, 56) },          // GRASS (127, 178, 56)
        { "minecraft:slime_block", new Rgba32(127, 178, 56) },
        { "minecraft:dirt", new Rgba32(151, 109, 77) },                 // DIRT (151, 109, 77)
        { "minecraft:coarse_dirt", new Rgba32(151, 109, 77) },
        { "minecraft:dirt_path", new Rgba32(151, 109, 77) },
        { "minecraft:farmland", new Rgba32(151, 109, 77) },
        { "minecraft:podzol", new Rgba32(129, 86, 49) },                // PODZOL (129, 86, 49)
        { "minecraft:mycelium", new Rgba32(111, 99, 105) },
        { "minecraft:mud", new Rgba32(60, 57, 61) },
        { "minecraft:packed_mud", new Rgba32(142, 107, 78) },
        { "minecraft:mud_bricks", new Rgba32(138, 106, 79) },

        // ================= 沙漠与恶地 =================
        { "minecraft:sand", new Rgba32(247, 233, 163) },                // SAND (247, 233, 163)
        { "minecraft:suspicious_sand", new Rgba32(247, 233, 163) },
        { "minecraft:red_sand", new Rgba32(216, 127, 51) },             // COLOR_ORANGE
        { "minecraft:sandstone", new Rgba32(247, 233, 163) },
        { "minecraft:cut_sandstone", new Rgba32(247, 233, 163) },
        { "minecraft:smooth_sandstone", new Rgba32(247, 233, 163) },
        { "minecraft:chiseled_sandstone", new Rgba32(247, 233, 163) },
        { "minecraft:terracotta", new Rgba32(159, 82, 36) },            // TERRACOTTA_ORANGE
        { "minecraft:cactus", new Rgba32(0, 124, 0) },                  // PLANT (0, 124, 0)
        { "minecraft:dead_bush", new Rgba32(143, 100, 48) },

        // ================= 岩石与矿物 =================
        { "minecraft:stone", new Rgba32(112, 112, 112) },               // STONE (112, 112, 112)
        { "minecraft:smooth_stone", new Rgba32(112, 112, 112) },
        { "minecraft:cobblestone", new Rgba32(112, 112, 112) },
        { "minecraft:mossy_cobblestone", new Rgba32(112, 112, 112) },
        { "minecraft:stone_bricks", new Rgba32(112, 112, 112) },
        { "minecraft:deepslate", new Rgba32(100, 100, 100) },           // DEEPSLATE (100, 100, 100)
        { "minecraft:cobbled_deepslate", new Rgba32(100, 100, 100) },
        { "minecraft:deepslate_bricks", new Rgba32(100, 100, 100) },
        { "minecraft:tuff", new Rgba32(100, 100, 100) },
        { "minecraft:tuff_bricks", new Rgba32(100, 100, 100) },
        { "minecraft:granite", new Rgba32(151, 109, 77) },              // DIRT
        { "minecraft:diorite", new Rgba32(255, 255, 255) },             // SNOW
        { "minecraft:andesite", new Rgba32(112, 112, 112) },            // STONE
        { "minecraft:gravel", new Rgba32(112, 112, 112) },
        { "minecraft:clay", new Rgba32(164, 168, 184) },                // CLAY (164, 168, 184)
        { "minecraft:bedrock", new Rgba32(112, 112, 112) },

        // ================= 冰雪地貌 =================
        { "minecraft:snow_block", new Rgba32(255, 255, 255) },          // SNOW (255, 255, 255)
        { "minecraft:snow", new Rgba32(255, 255, 255) },
        { "minecraft:ice", new Rgba32(160, 160, 255) },                 // ICE (160, 160, 255)
        { "minecraft:packed_ice", new Rgba32(160, 160, 255) },
        { "minecraft:blue_ice", new Rgba32(160, 160, 255) },

        // ================= 树木与植物 =================
        // 橡木
        { "minecraft:oak_leaves", new Rgba32(0, 124, 0) },              // PLANT (0, 124, 0)
        { "minecraft:oak_log", new Rgba32(143, 119, 72) },              // WOOD (143, 119, 72)
        { "minecraft:oak_wood", new Rgba32(143, 119, 72) },
        { "minecraft:oak_planks", new Rgba32(143, 119, 72) },
        // 白桦
        { "minecraft:birch_leaves", new Rgba32(0, 124, 0) },
        { "minecraft:birch_log", new Rgba32(247, 233, 163) },           // SAND (247, 233, 163)
        { "minecraft:birch_wood", new Rgba32(247, 233, 163) },
        { "minecraft:birch_planks", new Rgba32(247, 233, 163) },
        // 云杉
        { "minecraft:spruce_leaves", new Rgba32(0, 124, 0) },
        { "minecraft:spruce_log", new Rgba32(129, 86, 49) },            // PODZOL (129, 86, 49)
        { "minecraft:spruce_wood", new Rgba32(129, 86, 49) },
        { "minecraft:spruce_planks", new Rgba32(129, 86, 49) },
        // 丛林木
        { "minecraft:jungle_leaves", new Rgba32(0, 124, 0) },
        { "minecraft:jungle_log", new Rgba32(151, 109, 77) },           // DIRT
        { "minecraft:jungle_wood", new Rgba32(151, 109, 77) },
        { "minecraft:jungle_planks", new Rgba32(151, 109, 77) },
        // 金合欢
        { "minecraft:acacia_leaves", new Rgba32(0, 124, 0) },
        { "minecraft:acacia_log", new Rgba32(216, 127, 51) },           // COLOR_ORANGE
        { "minecraft:acacia_wood", new Rgba32(216, 127, 51) },
        { "minecraft:acacia_planks", new Rgba32(216, 127, 51) },
        // 深色橡木
        { "minecraft:dark_oak_leaves", new Rgba32(0, 124, 0) },
        { "minecraft:dark_oak_log", new Rgba32(102, 76, 51) },          // COLOR_BROWN
        { "minecraft:dark_oak_wood", new Rgba32(102, 76, 51) },
        { "minecraft:dark_oak_planks", new Rgba32(102, 76, 51) },
        // 红树木 (真实树皮深红棕与木板粉红，拒绝地狱岩刺眼红)
        { "minecraft:mangrove_leaves", new Rgba32(0, 124, 0) },
        { "minecraft:mangrove_log", new Rgba32(84, 40, 34) },
        { "minecraft:mangrove_wood", new Rgba32(84, 40, 34) },
        { "minecraft:mangrove_roots", new Rgba32(70, 50, 35) },
        { "minecraft:muddy_mangrove_roots", new Rgba32(65, 55, 45) },
        { "minecraft:mangrove_planks", new Rgba32(117, 54, 48) },
        // 🌸 樱花木群系 (柔和樱粉色，解决不显示与错误纯白问题)
        { "minecraft:cherry_leaves", new Rgba32(230, 150, 170) },       // 樱花树冠粉色
        { "minecraft:pink_petals", new Rgba32(230, 150, 170) },         // 地表落花粉色
        { "minecraft:cherry_sapling", new Rgba32(230, 150, 170) },
        { "minecraft:cherry_log", new Rgba32(53, 35, 43) },             // 樱花深棕紫树皮
        { "minecraft:cherry_wood", new Rgba32(53, 35, 43) },
        { "minecraft:stripped_cherry_log", new Rgba32(226, 178, 174) },
        { "minecraft:stripped_cherry_wood", new Rgba32(226, 178, 174) },
        { "minecraft:cherry_planks", new Rgba32(226, 178, 174) },       // 樱花淡粉木板
        // 26.3 斑驳森林与白杨木
        { "minecraft:poplar_leaves", new Rgba32(210, 130, 40) },        // 秋季金橙树冠
        { "minecraft:poplar_log", new Rgba32(215, 215, 210) },          // 浅灰白树皮
        { "minecraft:poplar_wood", new Rgba32(215, 215, 210) },
        { "minecraft:poplar_planks", new Rgba32(220, 190, 140) },
        { "minecraft:shelf_mushroom", new Rgba32(143, 100, 48) },
        { "minecraft:straw_bed", new Rgba32(220, 190, 100) },
        { "minecraft:leaf_litter", new Rgba32(180, 100, 40) },
        // 苍白之园 (Pale Garden)
        { "minecraft:pale_oak_leaves", new Rgba32(140, 150, 140) },
        { "minecraft:pale_oak_log", new Rgba32(180, 180, 180) },
        { "minecraft:pale_oak_wood", new Rgba32(180, 180, 180) },
        { "minecraft:pale_oak_planks", new Rgba32(230, 230, 230) },
        { "minecraft:pale_moss_block", new Rgba32(140, 150, 140) },
        { "minecraft:pale_moss_carpet", new Rgba32(140, 150, 140) },
        { "minecraft:pale_hanging_moss", new Rgba32(140, 150, 140) },

        // ================= 植被、地被与苔藓 =================
        { "minecraft:short_grass", new Rgba32(0, 124, 0) },             // PLANT (0, 124, 0)
        { "minecraft:tall_grass", new Rgba32(0, 124, 0) },
        { "minecraft:fern", new Rgba32(0, 124, 0) },
        { "minecraft:large_fern", new Rgba32(0, 124, 0) },
        { "minecraft:moss_block", new Rgba32(100, 130, 50) },
        { "minecraft:moss_carpet", new Rgba32(100, 130, 50) },
        { "minecraft:dandelion", new Rgba32(255, 235, 60) },
        { "minecraft:poppy", new Rgba32(220, 40, 40) },
        { "minecraft:blue_orchid", new Rgba32(43, 178, 242) },
        { "minecraft:allium", new Rgba32(180, 100, 200) },
        { "minecraft:azure_bluet", new Rgba32(230, 230, 230) },
        { "minecraft:red_tulip", new Rgba32(220, 40, 40) },
        { "minecraft:orange_tulip", new Rgba32(230, 120, 40) },
        { "minecraft:white_tulip", new Rgba32(230, 230, 230) },
        { "minecraft:pink_tulip", new Rgba32(230, 150, 170) },
        { "minecraft:oxeye_daisy", new Rgba32(230, 230, 230) },
        { "minecraft:cornflower", new Rgba32(65, 105, 225) },
        { "minecraft:lily_of_the_valley", new Rgba32(255, 255, 255) },
        { "minecraft:sunflower", new Rgba32(255, 215, 0) },
        { "minecraft:lilac", new Rgba32(180, 100, 200) },
        { "minecraft:rose_bush", new Rgba32(200, 30, 30) },
        { "minecraft:peony", new Rgba32(230, 150, 170) },
        { "minecraft:lily_pad", new Rgba32(0, 124, 0) },
        { "minecraft:seagrass", new Rgba32(64, 64, 255) },
        { "minecraft:kelp_plant", new Rgba32(64, 64, 255) },
        { "minecraft:kelp", new Rgba32(64, 64, 255) },

        // ================= 常见建筑与人造方块 =================
        { "minecraft:glass", new Rgba32(200, 220, 230) },
        { "minecraft:glass_pane", new Rgba32(200, 220, 230) },
        { "minecraft:white_wool", new Rgba32(255, 255, 255) },
        { "minecraft:bricks", new Rgba32(150, 70, 55) },                // 真实红砖陶土色
        { "minecraft:iron_block", new Rgba32(167, 167, 167) },
        { "minecraft:copper_block", new Rgba32(195, 106, 75) },
        { "minecraft:exposed_copper", new Rgba32(161, 125, 103) },
        { "minecraft:weathered_copper", new Rgba32(109, 153, 129) },
        { "minecraft:oxidized_copper", new Rgba32(82, 163, 134) },

        // ================= 下界 (Nether) 官方地图色 =================
        { "minecraft:netherrack", new Rgba32(112, 2, 0) },              // NETHER (112, 2, 0)
        { "minecraft:nether_bricks", new Rgba32(112, 2, 0) },          // NETHER
        { "minecraft:nether_brick_fence", new Rgba32(112, 2, 0) },
        { "minecraft:nether_brick_stairs", new Rgba32(112, 2, 0) },
        { "minecraft:nether_brick_slab", new Rgba32(112, 2, 0) },
        { "minecraft:nether_brick_wall", new Rgba32(112, 2, 0) },
        { "minecraft:cracked_nether_bricks", new Rgba32(112, 2, 0) },
        { "minecraft:chiseled_nether_bricks", new Rgba32(112, 2, 0) },
        { "minecraft:red_nether_bricks", new Rgba32(112, 2, 0) },
        { "minecraft:red_nether_brick_stairs", new Rgba32(112, 2, 0) },
        { "minecraft:red_nether_brick_slab", new Rgba32(112, 2, 0) },
        { "minecraft:red_nether_brick_wall", new Rgba32(112, 2, 0) },
        { "minecraft:nether_gold_ore", new Rgba32(112, 2, 0) },
        { "minecraft:nether_quartz_ore", new Rgba32(112, 2, 0) },
        { "minecraft:magma_block", new Rgba32(112, 2, 0) },
        { "minecraft:soul_sand", new Rgba32(102, 76, 51) },             // COLOR_BROWN (102, 76, 51)
        { "minecraft:soul_soil", new Rgba32(102, 76, 51) },
        { "minecraft:basalt", new Rgba32(112, 112, 112) },              // COLOR_STONE / COLOR_GRAY
        { "minecraft:polished_basalt", new Rgba32(112, 112, 112) },
        { "minecraft:smooth_basalt", new Rgba32(112, 112, 112) },
        { "minecraft:blackstone", new Rgba32(25, 25, 25) },             // COLOR_BLACK
        { "minecraft:polished_blackstone", new Rgba32(25, 25, 25) },
        { "minecraft:polished_blackstone_bricks", new Rgba32(25, 25, 25) },
        { "minecraft:gilded_blackstone", new Rgba32(25, 25, 25) },
        { "minecraft:chiseled_polished_blackstone", new Rgba32(25, 25, 25) },
        { "minecraft:ancient_debris", new Rgba32(25, 25, 25) },
        { "minecraft:crying_obsidian", new Rgba32(25, 25, 25) },
        { "minecraft:respawn_anchor", new Rgba32(25, 25, 25) },
        { "minecraft:glowstone", new Rgba32(247, 233, 163) },           // SAND (247, 233, 163)
        { "minecraft:shroomlight", new Rgba32(159, 82, 36) },           // TERRACOTTA_ORANGE (159, 82, 36)
        { "minecraft:bone_block", new Rgba32(247, 233, 163) },          // SAND (247, 233, 163)
        { "minecraft:crimson_nylium", new Rgba32(189, 48, 49) },        // CRIMSON_NYLIUM (189, 48, 49)
        { "minecraft:crimson_stem", new Rgba32(148, 63, 97) },          // CRIMSON_STEM (148, 63, 97)
        { "minecraft:stripped_crimson_stem", new Rgba32(148, 63, 97) },
        { "minecraft:crimson_hyphae", new Rgba32(92, 25, 29) },         // CRIMSON_HYPHAE (92, 25, 29)
        { "minecraft:stripped_crimson_hyphae", new Rgba32(92, 25, 29) },
        { "minecraft:crimson_planks", new Rgba32(148, 63, 97) },
        { "minecraft:crimson_fungus", new Rgba32(112, 2, 0) },
        { "minecraft:crimson_roots", new Rgba32(112, 2, 0) },
        { "minecraft:weeping_vines", new Rgba32(112, 2, 0) },
        { "minecraft:weeping_vines_plant", new Rgba32(112, 2, 0) },
        { "minecraft:nether_wart_block", new Rgba32(255, 0, 0) },       // COLOR_RED
        { "minecraft:nether_wart", new Rgba32(255, 0, 0) },
        { "minecraft:warped_nylium", new Rgba32(22, 126, 134) },        // WARPED_NYLIUM (22, 126, 134)
        { "minecraft:warped_stem", new Rgba32(58, 142, 140) },          // WARPED_STEM (58, 142, 140)
        { "minecraft:stripped_warped_stem", new Rgba32(58, 142, 140) },
        { "minecraft:warped_hyphae", new Rgba32(86, 44, 62) },          // WARPED_HYPHAE (86, 44, 62)
        { "minecraft:stripped_warped_hyphae", new Rgba32(86, 44, 62) },
        { "minecraft:warped_planks", new Rgba32(58, 142, 140) },
        { "minecraft:warped_fungus", new Rgba32(22, 126, 134) },
        { "minecraft:warped_roots", new Rgba32(22, 126, 134) },
        { "minecraft:twisting_vines", new Rgba32(22, 126, 134) },
        { "minecraft:twisting_vines_plant", new Rgba32(22, 126, 134) },
        { "minecraft:warped_wart_block", new Rgba32(20, 180, 133) },    // WARPED_WART_BLOCK (20, 180, 133)

        // ================= 末地 (The End) 官方地图色 =================
        { "minecraft:end_stone", new Rgba32(247, 233, 163) },           // SAND (247, 233, 163)
        { "minecraft:end_stone_bricks", new Rgba32(247, 233, 163) },    // SAND
        { "minecraft:end_stone_brick_stairs", new Rgba32(247, 233, 163) },
        { "minecraft:end_stone_brick_slab", new Rgba32(247, 233, 163) },
        { "minecraft:end_stone_brick_wall", new Rgba32(247, 233, 163) },
        { "minecraft:purpur_block", new Rgba32(178, 76, 216) },         // COLOR_MAGENTA (178, 76, 216)
        { "minecraft:purpur_pillar", new Rgba32(178, 76, 216) },
        { "minecraft:purpur_stairs", new Rgba32(178, 76, 216) },
        { "minecraft:purpur_slab", new Rgba32(178, 76, 216) },
        { "minecraft:chorus_plant", new Rgba32(127, 63, 178) },         // COLOR_PURPLE (127, 63, 178)
        { "minecraft:chorus_flower", new Rgba32(127, 63, 178) },
        { "minecraft:obsidian", new Rgba32(25, 25, 25) },               // COLOR_BLACK
        { "minecraft:end_rod", new Rgba32(255, 255, 255) },
        { "minecraft:dragon_egg", new Rgba32(25, 25, 25) },
        { "minecraft:end_portal_frame", new Rgba32(0, 124, 0) }         // PLANT
    };

    /// <summary>
    /// 未显式映射方块的安全回退色彩计算，绝不使用跨像素污染的全局变量，绝不异常偏红
    /// </summary>
    private static Rgba32 GetFallbackColor(string blockName)
    {
        if (string.IsNullOrEmpty(blockName)) return new Rgba32(127, 178, 56);
        if (blockName.Contains("leaves")) return new Rgba32(60, 120, 40);
        if (blockName.Contains("log") || blockName.Contains("wood") || blockName.Contains("stem")) return new Rgba32(107, 85, 52);
        if (blockName.Contains("planks")) return new Rgba32(162, 130, 78);
        if (blockName.Contains("water")) return new Rgba32(64, 64, 255);
        if (blockName.Contains("sand")) return new Rgba32(247, 233, 163);
        if (blockName.Contains("stone") || blockName.Contains("deepslate") || blockName.Contains("tuff") || blockName.Contains("andesite") || blockName.Contains("granite") || blockName.Contains("diorite")) return new Rgba32(112, 112, 112);
        if (blockName.Contains("copper")) return new Rgba32(195, 106, 75);
        if (blockName.Contains("cherry") || blockName.Contains("pink")) return new Rgba32(230, 150, 170);
        if (blockName.Contains("ice")) return new Rgba32(160, 200, 255);
        if (blockName.Contains("snow")) return new Rgba32(255, 255, 255);
        if (blockName.Contains("wool") || blockName.Contains("concrete") || blockName.Contains("terracotta")) return new Rgba32(180, 180, 180);
        return new Rgba32(127, 178, 56); // 默认自然草地绿
    }

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
        for (int cx = 0; cx < 32; cx++)
        {
            for (int cz = 0; cz < 32; cz++)
            {
                var chunkTag = parser.GetChunkNbt(cx, cz);
                if (chunkTag == null) continue;

                var level = chunkTag.Get<NbtCompound>("Level") ?? chunkTag;
                var heightmaps = level.Get<NbtCompound>("Heightmaps") ?? level.Get<NbtCompound>("heightmaps");
                if (heightmaps == null) continue;

                var worldSurfData = (heightmaps.Get<NbtLongArray>("WORLD_SURFACE") ?? heightmaps.Get<NbtLongArray>("world_surface"))?.Value;
                var motionData = (heightmaps.Get<NbtLongArray>("MOTION_BLOCKING") ?? heightmaps.Get<NbtLongArray>("motion_blocking"))?.Value;
                var surfaceData = worldSurfData ?? motionData;
                if (surfaceData == null) continue;

                int[] topHeights = UnpackHeightmap(surfaceData);

                var floorData = (heightmaps.Get<NbtLongArray>("OCEAN_FLOOR") ?? heightmaps.Get<NbtLongArray>("ocean_floor"))?.Value;
                int[]? floorHeights = floorData != null ? UnpackHeightmap(floorData) : null;

                var sections = level.Get<NbtList>("sections") ?? level.Get<NbtList>("Sections");
                if (sections == null || sections.Count == 0) continue;

                // 计算当前版本的 Y 轴基础偏移 (1.18+ / 26.x 为 -64..319, 旧版为 0..256)
                int dataVersion = chunkTag.Get<NbtInt>("DataVersion")?.Value ?? level.Get<NbtInt>("DataVersion")?.Value ?? 0;
                bool is118Plus = dataVersion >= 2860;
                if (!is118Plus)
                {
                    var yPos = level.Get<NbtInt>("yPos")?.Value ?? chunkTag.Get<NbtInt>("yPos")?.Value;
                    if (yPos != null && yPos.Value < 0) is118Plus = true;
                    else if (sections.OfType<NbtCompound>().Any(s => (GetSectionY(s) ?? 0) < 0) || sections.Count > 16) is118Plus = true;
                }

                int minY = is118Plus ? -64 : 0;
                int heightOffset = -minY + 1; // 1.18+ 为 65，1.17 及以下为 1

                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int idx = z * 16 + x;
                        int topY = topHeights[idx] - heightOffset;
                        int floorY = floorHeights != null ? (floorHeights[idx] - heightOffset) : (topY - 1);

                        if (topHeights[idx] <= 0 || topY < minY) continue;

                        string blockName = GetBlockNameAtY(sections, x, topY, z);

                        // 若高度图所指方块恰好为空气（高度图延迟或表面植被），向下探查寻找真实地表方块
                        if (blockName == "minecraft:air" || blockName == "minecraft:cave_air")
                        {
                            for (int dy = 1; dy <= 20 && topY - dy >= minY; dy++)
                            {
                                var probe = GetBlockNameAtY(sections, x, topY - dy, z);
                                if (probe != "minecraft:air" && probe != "minecraft:cave_air")
                                {
                                    blockName = probe;
                                    topY -= dy;
                                    break;
                                }
                            }
                        }

                        // 如果仍然是空气，保持透明底色，坚决不进行错误染色
                        if (blockName == "minecraft:air" || blockName == "minecraft:cave_air") continue;

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
                        }
                        else
                        {
                            baseColor = GetFallbackColor(blockName);
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
        int[,] floorHeights = new int[16, 16];
        string[,] floorBlocks = new string[16, 16];

        for (int cx = 0; cx < 32; cx++)
        {
            for (int cz = 0; cz < 32; cz++)
            {
                var chunkTag = parser.GetChunkNbt(cx, cz);
                if (chunkTag == null) continue;

                var level = chunkTag.Get<NbtCompound>("Level") ?? chunkTag;
                var sections = level.Get<NbtList>("sections") ?? level.Get<NbtList>("Sections");
                if (sections == null || sections.Count == 0) continue;

                // 阶段 1：向下扫描穿透天花板寻找开阔洞穴地面与岩浆湖
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        string topBlock = "minecraft:netherrack";
                        int topY = 31; // 下界标准岩浆海平面
                        bool foundAir = false;

                        for (int y = 122; y >= 10; y--)
                        {
                            var b = GetBlockNameAtY(sections, x, y, z);
                            bool isAir = b == "minecraft:air" || b == "minecraft:cave_air";

                            if (!foundAir)
                            {
                                if (isAir) foundAir = true;
                            }
                            else
                            {
                                if (!isAir)
                                {
                                    topBlock = b;
                                    topY = y;
                                    break;
                                }
                            }
                        }

                        floorHeights[x, z] = topY;
                        floorBlocks[x, z] = topBlock;
                    }
                }

                // 阶段 2：着色与山体浮雕光影
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int topY = floorHeights[x, z];
                        string topBlock = floorBlocks[x, z];
                        bool isLava = topBlock == "minecraft:lava";

                        Rgba32 baseColor;
                        if (isLava)
                        {
                            baseColor = new Rgba32(255, 68, 0); // 亮岩浆橙红
                        }
                        else if (BlockColorMap.TryGetValue(topBlock, out var mappedColor))
                        {
                            baseColor = mappedColor;
                        }
                        else
                        {
                            baseColor = GetFallbackColor(topBlock);
                        }

                        int northY = (z > 0) ? floorHeights[x, z - 1] : topY;
                        int diff = topY - northY;
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
        for (int cx = 0; cx < 32; cx++)
        {
            for (int cz = 0; cz < 32; cz++)
            {
                var chunkTag = parser.GetChunkNbt(cx, cz);
                if (chunkTag == null) continue;

                var level = chunkTag.Get<NbtCompound>("Level") ?? chunkTag;
                var heightmaps = level.Get<NbtCompound>("Heightmaps") ?? level.Get<NbtCompound>("heightmaps");
                var sections = level.Get<NbtList>("sections") ?? level.Get<NbtList>("Sections");
                if (sections == null || sections.Count == 0) continue;

                var worldSurfData = (heightmaps?.Get<NbtLongArray>("WORLD_SURFACE") ?? heightmaps?.Get<NbtLongArray>("world_surface"))?.Value;
                var motionData = (heightmaps?.Get<NbtLongArray>("MOTION_BLOCKING") ?? heightmaps?.Get<NbtLongArray>("motion_blocking"))?.Value;
                var surfaceData = worldSurfData ?? motionData;
                int[]? topHeights = surfaceData != null ? UnpackHeightmap(surfaceData) : null;

                int minY = 0;
                int heightOffset = 1;

                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int idx = z * 16 + x;
                        int topY = topHeights != null ? topHeights[idx] - heightOffset : 60;

                        // 虚空区域保持透明
                        if (topY <= minY) continue;

                        string blockName = GetBlockNameAtY(sections, x, topY, z);

                        // 空气向下探测真实岛屿表面
                        if (blockName == "minecraft:air" || blockName == "minecraft:cave_air")
                        {
                            for (int dy = 1; dy <= 20 && topY - dy >= minY; dy++)
                            {
                                var probe = GetBlockNameAtY(sections, x, topY - dy, z);
                                if (probe != "minecraft:air" && probe != "minecraft:cave_air")
                                {
                                    blockName = probe;
                                    topY -= dy;
                                    break;
                                }
                            }
                        }

                        if (blockName == "minecraft:air" || blockName == "minecraft:cave_air") continue;

                        Rgba32 baseColor;
                        if (BlockColorMap.TryGetValue(blockName, out var mappedColor))
                        {
                            baseColor = mappedColor;
                        }
                        else
                        {
                            baseColor = GetFallbackColor(blockName);
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
        if (data == null || data.Length == 0) return heights;

        const int bitsPerEntry = 9;
        const long mask = (1L << bitsPerEntry) - 1;

        if (data.Length >= 37)
        {
            // 现代 1.16+ 格式：每 64 位 long 存放 7 个 9-bit 整数，边界不跨 long
            const int entriesPerLong = 7;
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
        }
        else
        {
            // 早期 1.15 格式：36 个 long 紧凑连续位流
            for (int index = 0; index < 256; index++)
            {
                int startBit = index * bitsPerEntry;
                int longIndex = startBit / 64;
                int bitOffset = startBit % 64;
                if (longIndex >= data.Length) break;

                ulong low = (ulong)data[longIndex] >> bitOffset;
                ulong val = low;
                if (bitOffset + bitsPerEntry > 64 && longIndex + 1 < data.Length)
                {
                    ulong high = (ulong)data[longIndex + 1] << (64 - bitOffset);
                    val = low | high;
                }
                heights[index] = (int)(val & (ulong)mask);
            }
        }

        return heights;
    }

    private static int? GetSectionY(NbtCompound sec)
    {
        var rawY = sec.Get<NbtByte>("Y")?.Value;
        if (rawY != null) return (int)(sbyte)rawY.Value;
        return sec.Get<NbtInt>("Y")?.Value ?? (int?)sec.Get<NbtShort>("Y")?.Value;
    }

    private static string ExtractBlockName(NbtTag? tag)
    {
        if (tag is NbtString strTag) return strTag.Value;
        if (tag is NbtCompound cmpTag)
        {
            return cmpTag.Get<NbtString>("Name")?.Value
                ?? cmpTag.Get<NbtString>("id")?.Value
                ?? cmpTag.Get<NbtString>("name")?.Value
                ?? "minecraft:air";
        }
        return "minecraft:air";
    }

    private static string GetBlockNameAtY(NbtList sections, int x, int y, int z)
    {
        int sectionY = y >> 4;

        foreach (NbtCompound sec in sections)
        {
            var secY = GetSectionY(sec);
            if (secY == null || secY.Value != sectionY) continue;

            NbtList? palette = null;
            long[]? data = null;

            var blockStates = sec.Get<NbtCompound>("block_states") ?? sec.Get<NbtCompound>("BlockStates");
            if (blockStates != null)
            {
                palette = blockStates.Get<NbtList>("palette") ?? blockStates.Get<NbtList>("Palette");
                data = (blockStates.Get<NbtLongArray>("data") ?? blockStates.Get<NbtLongArray>("Data"))?.Value;
            }
            else
            {
                palette = sec.Get<NbtList>("Palette") ?? sec.Get<NbtList>("palette");
                data = (sec.Get<NbtLongArray>("BlockStates") ?? sec.Get<NbtLongArray>("block_states") ?? sec.Get<NbtLongArray>("data"))?.Value;
            }

            if (palette == null || palette.Count == 0) return "minecraft:air";

            if (data == null || data.Length == 0)
            {
                return ExtractBlockName(palette[0]);
            }

            int localY = y & 15;
            int blockIndex = (localY * 256) + (z * 16) + x;

            int paletteSize = palette.Count;
            int bitsPerEntry = Math.Max(4, (int)Math.Ceiling(Math.Log2(paletteSize)));

            // 根据 data.Length 校准 bitsPerEntry，避免调色板变更但数据数组未收缩导致的错位
            for (int b = 4; b <= 12; b++)
            {
                int expectedLongs = (int)Math.Ceiling(4096.0 / (64 / b));
                if (data.Length == expectedLongs)
                {
                    bitsPerEntry = b;
                    break;
                }
            }

            int entriesPerLong = 64 / bitsPerEntry;
            int paletteIndex;

            if (entriesPerLong > 0 && Math.Ceiling(4096.0 / entriesPerLong) <= data.Length)
            {
                // 现代 1.16+ 对齐打包 (不跨 64 位 long 边界)
                int longIndex = blockIndex / entriesPerLong;
                int startBit = (blockIndex % entriesPerLong) * bitsPerEntry;

                if (longIndex >= data.Length) return "minecraft:air";

                long val = data[longIndex];
                long mask = (1L << bitsPerEntry) - 1;
                paletteIndex = (int)((val >> startBit) & mask);
            }
            else
            {
                // 旧版 1.15 及以前连续跨 long 打包
                int startBit = blockIndex * bitsPerEntry;
                int longIndex = startBit / 64;
                int bitOffset = startBit % 64;
                if (longIndex >= data.Length) return "minecraft:air";

                ulong low = (ulong)data[longIndex] >> bitOffset;
                ulong val = low;
                if (bitOffset + bitsPerEntry > 64 && longIndex + 1 < data.Length)
                {
                    ulong high = (ulong)data[longIndex + 1] << (64 - bitOffset);
                    val = low | high;
                }
                long mask = (1L << bitsPerEntry) - 1;
                paletteIndex = (int)(val & (ulong)mask);
            }

            if (paletteIndex >= paletteSize) return "minecraft:air";

            return ExtractBlockName(palette[paletteIndex]);
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
