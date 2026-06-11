using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    public partial class MainTabWindow_星际商店
    {
        // 帧级缓存：交易改善（避免每帧重复查询信标/控制台）
        private float 缓存交易改善 = float.MinValue;
        private int 缓存改善帧 = -1;

        // 帧级缓存：白银总量（避免多处独立遍历白银堆叠）
        private int 缓存白银总量 = -1;
        private int 缓存白银帧 = -1;

        // ================================================================
        //  价格计算
        // ================================================================

        // 帧级缓存字典用于购买模式的价格查询
        private Dictionary<TransactionKey, float> 价格缓存 = new Dictionary<TransactionKey, float>();
        private int 价格缓存帧 = -1;

        // 旧签名兼容（不区分品质/材料的场景）
        private float 获取购买价格(ThingDef def)
        {
            return 获取购买价格(def, null, null);
        }

        private float 获取出售价格(ThingDef def)
        {
            return 获取出售价格(def, null, null);
        }

        // 新重载：支持品质/材料
        private float 获取购买价格(ThingDef def, QualityCategory? quality = null, ThingDef stuff = null)
        {
            float 基础价 = 获取基础价格(def, quality, stuff);
            float 乘数 = 星际商店Mod.设置?.购买价格乘数 ?? 1.6f;
            float 改善 = 获取交易改善(Find.CurrentMap);
            float 价格 = 基础价 * 乘数 * Mathf.Max(0.05f, 1f - 改善); // 最低 5% 价格

            // 折扣检查（AI 辅助生成：每日随机折扣物品）
            StarStore_SidebarConfigDef 折扣cfg = 侧边栏管理器.配置;
            if (折扣cfg != null)
            {
                ThingDef 折扣物品 = 折扣cfg.获取今日折扣物品();
                if (折扣物品 != null && def.defName == 折扣物品.defName)
                    价格 *= 折扣cfg.获取折扣比例();
            }
            return 价格;
        }

        private float 获取出售价格(ThingDef def, QualityCategory? quality = null, ThingDef stuff = null)
        {
            float 基础价 = 获取基础价格(def, quality, stuff);
            float 乘数 = 星际商店Mod.设置?.出售价格乘数 ?? 0.8f;
            float 改善 = 获取交易改善(Find.CurrentMap);
            return 基础价 * 乘数 * (1f + 改善);
        }

        private float 获取基础价格(ThingDef def, QualityCategory? quality, ThingDef stuff)
        {
            int frame = Time.frameCount;
            TransactionKey cacheKey = new TransactionKey(def, quality, stuff);

            if (价格缓存帧 == frame && 价格缓存.TryGetValue(cacheKey, out float cached))
                return cached;

            float marketValue = def.BaseMarketValue;

            // 材料价值：通过 StatSystem 计算（自动处理 StatPart_Stuff 等）
            if (def.MadeFromStuff && stuff != null)
            {
                try
                {
                    StatRequest req = StatRequest.For(def, stuff);
                    marketValue = StatDefOf.MarketValue.Worker.GetValue(req);
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"星际商店: StatRequest 计算 {def.defName} 材料价格失败: {ex.Message}");
                }
            }

            // 品质系数：手工应用（StatPart_Quality 在 Def-only 请求中不会触发）
            if (quality != null)
            {
                // 与原版 StatPart_Quality 一致的品质系数（Awful=0 ... Legendary=6）
                float[] 品质系数 = { 0.50f, 0.75f, 1.00f, 1.25f, 1.50f, 2.00f, 3.00f };
                int qIndex = (int)quality.Value;
                if (qIndex >= 0 && qIndex < 品质系数.Length)
                    marketValue *= 品质系数[qIndex];
            }

            if (价格缓存帧 != frame) { 价格缓存.Clear(); 价格缓存帧 = frame; }
            价格缓存[cacheKey] = marketValue;
            return marketValue;
        }

        private float 获取交易改善(Map map)
        {
            if (map == null) return 0f;
            int currentFrame = Time.frameCount;
            if (缓存改善帧 == currentFrame && 缓存交易改善 >= 0f)
                return 缓存交易改善;

            List<Building> beacons = map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.OrbitalTradeBeacon);
            List<Building> consoles = map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.CommsConsole);
            if (beacons.NullOrEmpty() || consoles.NullOrEmpty())
            {
                缓存交易改善 = 0f;
                缓存改善帧 = currentFrame;
                return 0f;
            }
            // 取最优信标的交易改善（与原版行为一致）
            float best = 0f;
            for (int i = 0; i < beacons.Count; i++)
            {
                float imp = beacons[i].GetStatValue(StatDefOf.TradePriceImprovement);
                if (imp > best) best = imp;
            }
            缓存交易改善 = best;
            缓存改善帧 = currentFrame;
            return best;
        }

        // 白银总量缓存（避免多处独立遍历白银堆叠）
        private int 获取白银总量(Map map)
        {
            if (map == null) return 0;
            int currentFrame = Time.frameCount;
            if (缓存白银帧 == currentFrame && 缓存白银总量 >= 0)
                return 缓存白银总量;

            int total = 0;
            List<Thing> silverList = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            for (int idx = 0; idx < silverList.Count; idx++)
                total += silverList[idx].stackCount;
            缓存白银总量 = total;
            缓存白银帧 = currentFrame;
            return 缓存白银总量;
        }

        // ================================================================
        //  判断物品属于哪个预定义分类
        // ================================================================
        // ================================================================
        //  分类判断 - 使用递归检查所有父分类
        // ================================================================
        private bool 物品属于分类(ThingDef def, string 分类Key)
        {
            if (分类Key == "StarStore_All") return true;
            if (分类Key == "StarStore_Favorites") return 收藏列表.Contains(def.defName);
            if (def.thingCategories == null) return false;

            // 获取物品的所有分类（包括父分类）
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
                    c == "Animals" || c == "Plants" ||
                    c == "Seeds" || c == "Books");
            return false;
        }

        /// <summary>
        /// 递归获取物品的所有分类（包括父分类）
        /// </summary>
        private HashSet<string> 获取所有分类(ThingDef def)
        {
            HashSet<string> 结果 = new HashSet<string>();
            if (def.thingCategories == null) return 结果;

            foreach (ThingCategoryDef cat in def.thingCategories)
            {
                添加分类及父分类(cat, 结果);
            }
            return 结果;
        }

        /// <summary>
        /// 递归添加分类及其所有父分类
        /// </summary>
        private void 添加分类及父分类(ThingCategoryDef cat, HashSet<string> 结果)
        {
            if (cat == null || 结果.Contains(cat.defName)) return;

            结果.Add(cat.defName);

            // 递归添加父分类
            if (cat.parent != null)
            {
                添加分类及父分类(cat.parent, 结果);
            }
        }

        // ================================================================
        //  刷新物品列表 - 显示所有可交易物品
        // ================================================================
        private void 刷新物品列表()
        {
            // 初始化模组列表
            if (!模组列表已初始化)
            {
                可选模组列表.Clear();
                可选模组列表.Add("StarStore_AllMods"); // 全部模组
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    string modName = def.modContentPack?.Name ?? "Unknown";
                    if (!可选模组列表.Contains(modName))
                        可选模组列表.Add(modName);
                }
                可选模组列表.Sort();
                模组列表已初始化 = true;
            }

            IEnumerable<ThingDef> query = DefDatabase<ThingDef>.AllDefs
                .Where(d => !d.destroyOnDrop && d.BaseMarketValue > 0f && !d.thingCategories.NullOrEmpty());

            // 按购买/卖出模式区分可交易性
            if (是购买模式)
                query = query.Where(d => d.tradeability == Tradeability.Buyable || d.tradeability == Tradeability.All);
            else
                query = query.Where(d => d.tradeability == Tradeability.Sellable || d.tradeability == Tradeability.All);

            // 分类过滤
            if (当前分类标签 != "StarStore_All")
            {
                query = query.Where(d => 物品属于分类(d, 当前分类标签));
            }

            // 搜索过滤
            if (!string.IsNullOrEmpty(搜索文本))
            {
                query = query.Where(d => d.label.IndexOf(搜索文本, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         d.defName.IndexOf(搜索文本, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 卖出模式
            if (!是购买模式)
            {
                Map map = Find.CurrentMap;
                if (map != null && 仅显示库存)
                {
                    HashSet<ThingDef> 拥有defs = new HashSet<ThingDef>();
                    List<Thing> allThings = map.listerThings.AllThings;
                    for (int i = 0; i < allThings.Count; i++)
                    {
                        Thing t = allThings[i];
                        // 只统计殖民地拥有的物品
                        if (t.Faction == Faction.OfPlayer || (t.Faction == null && t.IsInAnyStorage()))
                        {
                            if (!拥有defs.Contains(t.def))
                                拥有defs.Add(t.def);
                        }
                    }
                    query = query.Where(d => 拥有defs.Contains(d));
                }
            }

            // 已解锁科技筛选
            if (仅已解锁科技)
                query = query.Where(d => d.researchPrerequisites == null || d.researchPrerequisites.All(r => r.IsFinished));

            // 模组筛选
            if (筛选模组 != "StarStore_AllMods")
            {
                query = query.Where(d => (d.modContentPack?.Name ?? "Unknown") == 筛选模组);
            }

            当前显示物品 = query.OrderBy(d => d.label).ToList();
        }

        // ================================================================
        //  执行交易
        // ================================================================
        private void 执行购买()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            float 总花费 = 0f;
            List<Thing> 待生成 = new List<Thing>();
            foreach (KeyValuePair<TransactionKey, int> kv in 购买交易数量)
            {
                if (kv.Value <= 0) continue;
                TransactionKey key = kv.Key;

                if (!交易条件管理器.是否可以交易(key.def.defName, map))
                {
                    Messages.Message("StarStore_ConditionNotMet".Translate(key.def.label), MessageTypeDefOf.RejectInput);
                    return;
                }

                float 单价 = 获取购买价格(key.def, key.quality, key.stuff);
                总花费 += 单价 * kv.Value;

                ThingDef stuff = key.stuff;
                if (key.def.MadeFromStuff && stuff == null)
                    stuff = key.def.defaultStuff ?? ThingDefOf.Steel;

                try
                {
                    Thing 物品 = ThingMaker.MakeThing(key.def, stuff);
                    if (key.quality != null)
                    {
                        var comp = 物品.TryGetComp<CompQuality>();
                        if (comp != null) comp.SetQuality(key.quality.Value, ArtGenerationContext.Outsider);
                    }
                    物品.stackCount = kv.Value;
                    待生成.Add(物品);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"星际商店: 无法创建物品 {key} (stuff={stuff?.defName}): {ex.Message}");
                    Messages.Message("StarStore_CreateItemFailed".Translate(key.def.label), MessageTypeDefOf.RejectInput);
                    return;
                }
            }

            if (总花费 <= 0) return;

            int 需要白银 = Mathf.RoundToInt(总花费);
            int 当前白银 = 获取白银总量(map);
            if (当前白银 < 需要白银)
            {
                Messages.Message("StarStore_InsufficientSilver".Translate(需要白银, 当前白银), MessageTypeDefOf.RejectInput);
                return;
            }

            int 剩余白银 = 需要白银;
            List<Thing> silverThings = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            int si = 0;
            while (si < silverThings.Count && 剩余白银 > 0)
            {
                Thing silver = silverThings[si];
                if (silver == null || silver.Destroyed) { si++; continue; }
                int 扣除 = Mathf.Min(剩余白银, silver.stackCount);
                剩余白银 -= 扣除;
                silver.SplitOff(扣除);
                si++;
            }
            缓存白银帧 = -1;

            // AI 辅助生成：使用运输舱生成物品（优先轨道交易信标位置）
            IntVec3 dropSpot = 获取有效降落点(map);
            DropPodUtility.DropThingsNear(dropSpot, map, 待生成);
            购买交易数量.Clear();
            // 点击消息可跳转到降落点
            Messages.Message("StarStore_PurchaseComplete".Translate(), new LookTargets(dropSpot, map), MessageTypeDefOf.TaskCompletion);
            刷新物品列表();
        }

        private void 执行卖出()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            float 总收益 = 0f;
            var 库存映射数据 = 获取库存映射(map);
            foreach (var kv in 出售交易数量)
            {
                if (kv.Value <= 0) continue;

                // 从地图上找到匹配品质/材料的物品（使用帧级缓存避免重复遍历）
                List<Thing> candidates;
                if (!库存映射数据.TryGetValue(kv.Key.def, out List<Thing> 同Def物品))
                {
                    candidates = new List<Thing>();
                }
                else
                {
                    candidates = 同Def物品
                        .Where(t => new TransactionKey(t).Equals(kv.Key))
                        .OrderBy(t => t.HitPoints)
                        .ToList();
                }

                int available = candidates.Sum(t => t.stackCount);
                if (available < kv.Value)
                {
                    Messages.Message("StarStore_InsufficientStock".Translate(kv.Key.ToString()), MessageTypeDefOf.RejectInput);
                    return;
                }

                int 剩余卖出 = kv.Value;
                for (int j = 0; j < candidates.Count && 剩余卖出 > 0; j++)
                {
                    Thing t = candidates[j];
                    if (t == null || t.Destroyed) continue;
                    int 本次卖出数 = Mathf.Min(剩余卖出, t.stackCount);
                    // 使用与 UI 一致的出售价格公式，并考虑物品耐久度
                    float 基础单价 = 获取出售价格(kv.Key.def, kv.Key.quality, kv.Key.stuff);
                    float hpFactor = Mathf.Clamp01((float)t.HitPoints / (float)t.MaxHitPoints);
                    float 单价 = 基础单价 * hpFactor;
                    总收益 += 单价 * 本次卖出数;
                    剩余卖出 -= 本次卖出数;

                    t.SplitOff(本次卖出数);
                }
            }

            if (总收益 <= 0f) return;

            int 白银数量 = Mathf.RoundToInt(总收益);
            if (白银数量 > 0)
            {
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver, null);
                silver.stackCount = 白银数量;
                IntVec3 dropSpot = 获取有效降落点(map);
                // AI 辅助生成：使用运输舱生成白银收益
                DropPodUtility.DropThingsNear(dropSpot, map, new List<Thing> { silver });
            }
            出售交易数量.Clear();
            库存映射帧 = -1; // 出售后库存已变化，使缓存失效
            缓存白银帧 = -1; // 白银收益已生成，使缓存失效
            // 点击消息可跳转到降落点
            IntVec3 saleDropSpot = 获取有效降落点(map);
            Messages.Message("StarStore_SaleComplete".Translate(白银数量), new LookTargets(saleDropSpot, map), MessageTypeDefOf.TaskCompletion);
            刷新物品列表();
        }

        private IntVec3 获取有效降落点(Map map)
        {
            if (map == null) return IntVec3.Invalid;

            // 先尝试获取交易信标位置
            IntVec3 spot = DropCellFinder.TradeDropSpot(map);

            // 检查位置是否有效（包括检查坐标是否为负）
            if (!spot.IsValid || spot.x < 0 || spot.z < 0)
            {
                // 回退到随机降落点
                spot = DropCellFinder.RandomDropSpot(map);
                if (!spot.IsValid || spot.x < 0 || spot.z < 0)
                {
                    // 最终回退到地图中心附近
                    spot = map.Center;
                    // 确保位置可通行
                    if (!spot.Walkable(map))
                    {
                        // 搜索附近可通行位置
                        spot = CellFinder.RandomClosewalkCellNear(spot, map, 10);
                    }
                }
                Log.Warning($"星际商店: 未找到有效交易信标，使用回退位置 {spot}");
            }
            return spot;
        }
    }
}
