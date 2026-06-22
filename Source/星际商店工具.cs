using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 星际商店通用工具方法（AI 辅助生成）
    /// 供交易窗口、折扣计算等共享使用
    /// </summary>
    public static class 星际商店工具
    {
        /// <summary>
        /// 判断物品是否属于某个具体商店分类（不含 All / Favorites）
        /// </summary>
        public static bool 物品属于具体分类(ThingDef def, string 分类Key)
        {
            // AI 辅助生成：先处理不依赖 thingCategories 的分类（机械族、动物）
            if (分类Key == "StarStore_Cat_Mechanoids")
                return def.race != null && def.race.IsMechanoid;

            if (分类Key == "StarStore_Cat_Animals")
                return (def.race != null && def.race.Animal) ||
                       获取所有分类(def).Any(c => c == "Animals");

            if (def.thingCategories == null) return false;

            HashSet<string> 所有分类 = 获取所有分类(def);

            if (分类Key == "StarStore_Cat_Food")
                return 所有分类.Any(c =>
                    c == "Foods" || c == "FoodMeals" || c == "FoodRaw" ||
                    c == "FoodManufactured" || c == "PlantsFood" ||
                    c == "AnimalProducts" || c == "Eggs" ||
                    c == "MeatRaw" || c == "Milk" ||
                    c.StartsWith("Food"));
            if (分类Key == "StarStore_Cat_Medicine")
                return 所有分类.Any(c =>
                    c == "Medicine" || c == "Drugs" ||
                    c == "MedicalItems" || c.StartsWith("Drug") ||
                    c.StartsWith("Medi"));
            if (分类Key == "StarStore_Cat_Weapons")
                return 所有分类.Any(c =>
                    c == "Weapons" || c.StartsWith("Weapon") ||
                    c == "Guns" || c == "MeleeWeapons" ||
                    c == "RangedWeapons" || c == "Grenades" ||
                    c == "MortarShells");
            if (分类Key == "StarStore_Cat_Apparel")
                return 所有分类.Any(c =>
                    c == "Apparel" || c.StartsWith("Apparel") ||
                    c == "Armor" || c == "Clothing" ||
                    c == "Headgear" || c == "Shields");
            if (分类Key == "StarStore_Cat_RawMaterials")
                return 所有分类.Any(c =>
                    c == "Resources" || c == "RawMaterials" ||
                    c == "Metals" || c == "StoneBlocks" ||
                    c == "Wood" || c == "Textile" ||
                    c == "Leather" || c == "Fabrics" ||
                    c == "Chemicals" || c.StartsWith("Resource") ||
                    c.StartsWith("Raw") || c.StartsWith("Stone") ||
                    c.StartsWith("Metal"));
            if (分类Key == "StarStore_Cat_Manufactured")
                return 所有分类.Any(c =>
                    c == "Manufactured" || c == "Components" ||
                    c == "Parts" || c == "Tools" ||
                    c == "CraftingMaterials" || c.StartsWith("Manufactur"));
            if (分类Key == "StarStore_Cat_Buildings")
                return 所有分类.Any(c =>
                    c == "Buildings" || c.StartsWith("Building") ||
                    c == "Structures" || c == "Floors" ||
                    c == "Walls" || c == "Doors" ||
                    c == "Security" || c == "Power" ||
                    c == "Production" || c.StartsWith("Structure"));
            if (分类Key == "StarStore_Cat_Furniture")
                return 所有分类.Any(c =>
                    c == "BuildingsFurniture" || c == "Furniture" ||
                    c == "BuildingsJoy" || c == "Joy" ||
                    c == "BuildingsArt" || c == "Art" ||
                    c == "BuildingsTemperature" || c == "Temperature" ||
                    c == "Beds" || c == "Tables" || c == "Chairs" ||
                    c == "Lighting" || c == "Storage" ||
                    c == "Containers" || c == "Sculpture" ||
                    c == "Recreation" || c == "Furnishings");
            if (分类Key == "StarStore_Cat_Electronics")
                return 所有分类.Any(c =>
                    c == "BuildingsPower" || c == "Power" ||
                    c == "BuildingsSecurity" || c == "Security" ||
                    c == "BuildingsProduction" || c == "Production" ||
                    c == "BuildingsSpecial" || c == "Special" ||
                    c == "BuildingsMisc" || c == "Misc" ||
                    c == "Electronics" || c.StartsWith("Electron") ||
                    c == "Components" || c == "Chips" ||
                    c == "Mechanoids" || c == "MechParts" ||
                    c == "Energy" || c == "Batteries" || c == "Solar");
            if (分类Key == "StarStore_Cat_Misc")
                return 所有分类.Any(c =>
                    c == "Misc" || c == "Miscellaneous" ||
                    c == "Items" || c == "Goods" ||
                    c == "Chunks" || c == "Corpses" ||
                    c == "Plants" ||
                    c == "Seeds" || c == "Books");
            return false;
        }

        /// <summary>
        /// 递归获取物品的所有分类（包括父分类）
        /// </summary>
        public static HashSet<string> 获取所有分类(ThingDef def)
        {
            HashSet<string> 结果 = new HashSet<string>();
            if (def.thingCategories == null) return 结果;

            foreach (ThingCategoryDef cat in def.thingCategories)
            {
                添加分类及父分类(cat, 结果);
            }
            return 结果;
        }

        private static void 添加分类及父分类(ThingCategoryDef cat, HashSet<string> 结果)
        {
            if (cat == null || 结果.Contains(cat.defName)) return;

            结果.Add(cat.defName);

            if (cat.parent != null)
            {
                添加分类及父分类(cat.parent, 结果);
            }
        }
    }

    /// <summary>
    /// 机械族管理器 - 静态缓存原版与模组机械族
    /// AI 辅助生成
    /// </summary>
    public static class 机械族管理器
    {
        private static List<ThingDef> _机械族Race列表;
        private static Dictionary<ThingDef, List<PawnKindDef>> _机械族Kind映射;
        private static bool _已构建;

        private static void 构建缓存()
        {
            _机械族Race列表 = new List<ThingDef>();
            _机械族Kind映射 = new Dictionary<ThingDef, List<PawnKindDef>>();

            // AI 辅助生成：先从 ThingDef 层扫描，避免某些机械族没有 PawnKindDef 映射
            // 排除尸体类 ThingDef（Corpse_Mech_xxx 等），它们也有 race.IsMechanoid 但不可交易
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.race != null && def.race.IsMechanoid
                    && def.thingClass != typeof(Corpse)
                    && !_机械族Race列表.Contains(def))
                    _机械族Race列表.Add(def);
            }

            // 再用 PawnKindDef 补充 kind 映射
            foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefs)
            {
                if (kind.race == null || kind.race.race == null || !kind.race.race.IsMechanoid)
                    continue;
                if (!_机械族Race列表.Contains(kind.race))
                    _机械族Race列表.Add(kind.race);
                if (!_机械族Kind映射.ContainsKey(kind.race))
                    _机械族Kind映射[kind.race] = new List<PawnKindDef>();
                _机械族Kind映射[kind.race].Add(kind);
            }
            _已构建 = true;
        }

        public static bool 是机械族(ThingDef def)
        {
            if (!_已构建) 构建缓存();
            return def != null && def.race != null && def.race.IsMechanoid;
        }

        public static List<ThingDef> 获取所有机械族Race()
        {
            if (!_已构建) 构建缓存();
            return _机械族Race列表;
        }

        public static PawnKindDef 获取主要Kind(ThingDef race)
        {
            if (!_已构建) 构建缓存();
            if (_机械族Kind映射.TryGetValue(race, out List<PawnKindDef> list) && list.Count > 0)
                return list[0];
            return null;
        }

        public static List<PawnKindDef> 获取所有Kind(ThingDef race)
        {
            if (!_已构建) 构建缓存();
            return _机械族Kind映射.TryGetValue(race, out List<PawnKindDef> list) ? list : new List<PawnKindDef>();
        }

        /// <summary>获取当前地图上玩家拥有的活体机械族种族列表（卖出模式用）</summary>
        public static List<ThingDef> 获取殖民地机械族Race(Map map)
        {
            if (map == null) return new List<ThingDef>();
            return map.mapPawns.AllPawns
                .Where(p => p.Faction == Faction.OfPlayer &&
                            p.RaceProps != null &&
                            p.RaceProps.IsMechanoid &&
                            !p.Dead)
                .Select(p => p.def)
                .Distinct()
                .ToList();
        }

        /// <summary>获取指定种族的玩家机械族活体 Pawn（卖出模式用）</summary>
        public static IEnumerable<Pawn> 获取殖民地机械族(ThingDef race, Map map, int amount)
        {
            if (map == null || race == null || amount <= 0)
                return Enumerable.Empty<Pawn>();
            return map.mapPawns.AllPawns
                .Where(p => p.def == race &&
                            p.Faction == Faction.OfPlayer &&
                            p.RaceProps.IsMechanoid &&
                            !p.Dead)
                .Take(amount);
        }
    }

    /// <summary>
    /// 捆绑包管理器 - 价格计算与内容生成
    /// AI 辅助生成
    /// </summary>
    public static class 捆绑包管理器
    {
        private static List<StarStore_BundleDef> _所有有效礼包;

        public static List<StarStore_BundleDef> 获取所有有效礼包()
        {
            if (_所有有效礼包 == null)
                _所有有效礼包 = DefDatabase<StarStore_BundleDef>.AllDefs.Where(b => b.是否有效()).ToList();
            return _所有有效礼包;
        }

        public static void 刷新缓存()
        {
            _所有有效礼包 = null;
        }

        public static float 计算条目价格(StarStore_BundleDef.BundleEntry entry)
        {
            if (entry?.thingDef == null) return 0f;
            return entry.thingDef.BaseMarketValue * entry.count;
        }

        public static float 获取原价(StarStore_BundleDef bundle)
        {
            float sum = 0f;
            if (bundle.fixedItems != null)
                foreach (var e in bundle.fixedItems)
                    sum += 计算条目价格(e);
            if (bundle.randomGroups != null)
            {
                foreach (var g in bundle.randomGroups)
                {
                    if (g.thingDefPool == null || g.thingDefPool.Count == 0) continue;
                    float avg = g.thingDefPool.Average(d => d.BaseMarketValue);
                    sum += avg * g.count * g.itemCountRange.Average;
                }
            }
            return sum;
        }

        public static float 获取折扣价(StarStore_BundleDef bundle)
        {
            return 获取原价(bundle) * bundle.获取折扣率();
        }

        public static List<Thing> 生成礼包内容(StarStore_BundleDef bundle)
        {
            List<Thing> result = new List<Thing>();
            if (bundle.fixedItems != null)
            {
                foreach (var e in bundle.fixedItems)
                    result.AddRange(创建单个物品(e.thingDef, e.stuff, e.randomQuality ? null : (QualityCategory?)e.quality, e.count));
            }
            if (bundle.randomGroups != null)
            {
                foreach (var g in bundle.randomGroups)
                {
                    if (g.thingDefPool == null || g.thingDefPool.Count == 0) continue;
                    int itemCount = g.itemCountRange.RandomInRange;
                    for (int i = 0; i < itemCount; i++)
                    {
                        ThingDef def = g.thingDefPool.RandomElement();
                        QualityCategory? q = g.randomQuality ? null : (QualityCategory?)QualityCategory.Normal;
                        result.AddRange(创建单个物品(def, null, q, g.count));
                    }
                }
            }
            return result;
        }

        private static IEnumerable<Thing> 创建单个物品(ThingDef def, ThingDef stuff, QualityCategory? quality, int count)
        {
            if (def == null) yield break;

            // 动物 / 机械族：使用 PawnGenerator 完整生成管线
            if (def.race != null)
            {
                for (int i = 0; i < count; i++)
                {
                    PawnKindDef kind = 机械族管理器.获取主要Kind(def) ?? DefDatabase<PawnKindDef>.AllDefs.FirstOrDefault(k => k.race == def);
                    if (kind == null) continue;
                    Faction fac = def.race.IsMechanoid ? Faction.OfMechanoids : Faction.OfPlayer;
                    yield return PawnGenerator.GeneratePawn(kind, fac);
                }
                yield break;
            }

            ThingDef actualStuff = def.MadeFromStuff ? (stuff ?? def.defaultStuff ?? ThingDefOf.Steel) : null;
            int remaining = count;
            while (remaining > 0)
            {
                Thing t = ThingMaker.MakeThing(def, actualStuff);
                if (quality.HasValue && t.TryGetComp<CompQuality>() is CompQuality cq)
                    cq.SetQuality(quality.Value, ArtGenerationContext.Outsider);

                // AI 辅助生成：可迷你化建筑先迷你化，避免进入运输舱后被销毁
                if (def.category == ThingCategory.Building && def.Minifiable)
                    t = t.MakeMinified();

                int stack = Mathf.Min(remaining, def.stackLimit);
                t.stackCount = stack;
                yield return t;
                remaining -= stack;
            }
        }
    }
}
