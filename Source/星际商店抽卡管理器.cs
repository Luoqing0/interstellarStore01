using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

/// <summary>
/// 星际商店 - 抽卡管理器
/// AI 辅助生成
/// 核心逻辑：加权稀有度随机、单抽、十连、保底
/// </summary>
public static class 星际商店抽卡管理器
{
    private static List<StarStore_GachaRarityDef> _稀有度列表;
    private static List<StarStore_GachaDef> _有效卡池列表;
    private static bool _已构建;

    /// <summary>
    /// 构建或刷新缓存
    /// </summary>
    public static void 构建缓存()
    {
        _稀有度列表 = DefDatabase<StarStore_GachaRarityDef>.AllDefs
            .OrderByDescending(r => r.weight)
            .ToList();

        _有效卡池列表 = DefDatabase<StarStore_GachaDef>.AllDefs
            .OrderBy(g => g.sortOrder)
            .ThenBy(g => g.defName)
            .ToList();

        _已构建 = true;
    }

    /// <summary>
    /// 获取所有有效卡池
    /// </summary>
    public static List<StarStore_GachaDef> 获取所有卡池()
    {
        if (!_已构建) 构建缓存();
        return _有效卡池列表 ?? new List<StarStore_GachaDef>();
    }

    /// <summary>
    /// 按权重随机抽取稀有度
    /// AI 辅助生成：使用加权随机算法
    /// </summary>
    public static StarStore_GachaRarityDef 随机抽取稀有度(bool 十连保底激活)
    {
        if (!_已构建) 构建缓存();
        if (_稀有度列表.NullOrEmpty()) return null;

        float 总权重 = _稀有度列表.Sum(r => r.weight);
        float roll = Rand.Range(0f, 总权重);

        foreach (var rarity in _稀有度列表)
        {
            if (十连保底激活 && rarity.tenPullGuaranteeTier < 1) continue;
            roll -= rarity.weight;
            if (roll <= 0f) return rarity;
        }

        return _稀有度列表.Last();
    }

    /// <summary>
    /// 从指定稀有度的物品池中随机选择一个物品
    /// </summary>
    public static ThingDef 随机抽取物品(StarStore_GachaDef 卡池, StarStore_GachaRarityDef 稀有度)
    {
        if (卡池?.poolEntries == null) return null;

        var entry = 卡池.poolEntries.FirstOrDefault(e => e.rarityDefName == 稀有度.defName);
        if (entry?.thingDefs.NullOrEmpty() != false) return null;

        return entry.thingDefs.RandomElement();
    }

    /// <summary>
    /// 执行单抽
    /// </summary>
    public static Thing 执行单抽(StarStore_GachaDef 卡池, Map map)
    {
        var 稀有度 = 随机抽取稀有度(false);
        var def = 随机抽取物品(卡池, 稀有度);
        if (def == null) return null;

        Thing thing = ThingMaker.MakeThing(def);
        thing.stackCount = 1;

        if (thing is Pawn pawn)
        {
            IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
            GenSpawn.Spawn(pawn, spawnPos, map, WipeMode.Vanish);
        }

        return thing;
    }

    /// <summary>
    /// 执行十连
    /// </summary>
    public static List<Thing> 执行十连(StarStore_GachaDef 卡池, Map map)
    {
        List<Thing> results = new List<Thing>();
        bool 已有保底 = false;

        for (int i = 0; i < 10; i++)
        {
            bool 保底激活 = (i == 9 && !已有保底 && 卡池.tenPullGuaranteeRare);
            var 稀有度 = 随机抽取稀有度(保底激活);
            if (稀有度.tenPullGuaranteeTier >= 1) 已有保底 = true;

            var def = 随机抽取物品(卡池, 稀有度);
            if (def == null) continue;

            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = 1;

            if (thing is Pawn pawn)
            {
                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
                GenSpawn.Spawn(pawn, spawnPos, map, WipeMode.Vanish);
            }

            results.Add(thing);
        }

        return results;
    }

    /// <summary>
    /// 获取稀有度定义（按 DefName）
    /// </summary>
    public static StarStore_GachaRarityDef 获取稀有度(string defName)
    {
        if (!_已构建) 构建缓存();
        return _稀有度列表?.FirstOrDefault(r => r.defName == defName);
    }

    /// <summary>
    /// 获取所有稀有度
    /// </summary>
    public static List<StarStore_GachaRarityDef> 获取所有稀有度()
    {
        if (!_已构建) 构建缓存();
        return _稀有度列表 ?? new List<StarStore_GachaRarityDef>();
    }
}