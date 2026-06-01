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
            return 基础价 * 乘数 * Mathf.Max(0.05f, 1f - 改善); // 最低 5% 价格
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
        private bool 物品属于分类(ThingDef def, string 分类Key)
        {
            if (分类Key == "StarStore_All") return true;
            if (分类Key == "StarStore_Favorites") return 收藏列表.Contains(def.defName);
            if (def.thingCategories == null) return false;

            if (分类Key == "StarStore_Cat_Food")
                return def.thingCategories.Any(c =>
                    c.defName == "Foods" || c.defName == "FoodMeals" || c.defName == "FoodRaw" ||
                    c.defName == "FoodManufactured" || c.defName == "PlantsFood" ||
                    c.defName == "AnimalProducts" || c.defName == "Eggs" ||
                    c.defName == "MeatRaw" || c.defName == "Milk" ||
                    c.defName.StartsWith("Food"));
            if (分类Key == "StarStore_Cat_Medicine")
                return def.thingCategories.Any(c =>
                    c.defName == "Medicine" || c.defName == "Drugs" ||
                    c.defName == "MedicalItems" || c.defName.StartsWith("Drug") ||
                    c.defName.StartsWith("Medi"));
            if (分类Key == "StarStore_Cat_Weapons")
                return def.thingCategories.Any(c =>
                    c.defName == "Weapons" || c.defName.StartsWith("Weapon") ||
                    c.defName == "Guns" || c.defName == "MeleeWeapons" ||
                    c.defName == "RangedWeapons" || c.defName == "Grenades" ||
                    c.defName == "MortarShells");
            if (分类Key == "StarStore_Cat_Apparel")
                return def.thingCategories.Any(c =>
                    c.defName == "Apparel" || c.defName.StartsWith("Apparel") ||
                    c.defName == "Armor" || c.defName == "Clothing" ||
                    c.defName == "Headgear" || c.defName == "Shields");
            if (分类Key == "StarStore_Cat_RawMaterials")
                return def.thingCategories.Any(c =>
                    c.defName == "Resources" || c.defName == "RawMaterials" ||
                    c.defName == "Metals" || c.defName == "StoneBlocks" ||
                    c.defName == "Wood" || c.defName == "Textile" ||
                    c.defName == "Leather" || c.defName == "Fabrics" ||
                    c.defName == "Chemicals" || c.defName.StartsWith("Resource") ||
                    c.defName.StartsWith("Raw") || c.defName.StartsWith("Stone") ||
                    c.defName.StartsWith("Metal"));
            if (分类Key == "StarStore_Cat_Manufactured")
                return def.thingCategories.Any(c =>
                    c.defName == "Manufactured" || c.defName == "Components" ||
                    c.defName == "Parts" || c.defName == "Tools" ||
                    c.defName == "CraftingMaterials" || c.defName.StartsWith("Manufactur"));
            if (分类Key == "StarStore_Cat_Buildings")
                return def.thingCategories.Any(c =>
                    c.defName == "Buildings" || c.defName.StartsWith("Building") ||
                    c.defName == "Structures" || c.defName == "Floors" ||
                    c.defName == "Walls" || c.defName == "Doors" ||
                    c.defName == "Security" || c.defName == "Power" ||
                    c.defName == "Production" || c.defName.StartsWith("Structure"));
            if (分类Key == "StarStore_Cat_Furniture")
                return def.thingCategories.Any(c =>
                    c.defName == "Furniture" || c.defName.StartsWith("Furniture") ||
                    c.defName == "Furnishings" || c.defName == "Beds" ||
                    c.defName == "Tables" || c.defName == "Chairs" ||
                    c.defName == "Lighting" || c.defName == "Storage" ||
                    c.defName == "Containers" || c.defName == "Sculpture" ||
                    c.defName == "Art" || c.defName == "Joy" ||
                    c.defName == "Recreation");
            if (分类Key == "StarStore_Cat_Electronics")
                return def.thingCategories.Any(c =>
                    c.defName == "Electronics" || c.defName.StartsWith("Electron") ||
                    c.defName == "Components" || c.defName == "Chips" ||
                    c.defName == "Mechanoids" || c.defName == "MechParts" ||
                    c.defName == "Power" || c.defName == "Energy" ||
                    c.defName == "Batteries" || c.defName == "Solar");
            if (分类Key == "StarStore_Cat_Misc")
                return def.thingCategories.Any(c =>
                    c.defName == "Misc" || c.defName == "Miscellaneous" ||
                    c.defName == "Items" || c.defName == "Goods" ||
                    c.defName == "Chunks" || c.defName == "Corpses" ||
                    c.defName == "Animals" || c.defName == "Plants" ||
                    c.defName == "Seeds" || c.defName == "Books");
            return false;
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
            foreach (KeyValuePair<TransactionKey, int> kv in 交易数量)
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

            // 使用 GenPlace 直接生成物品到交易降落点
            int 生成失败数 = 0;
            for (int i = 0; i < 待生成.Count; i++)
            {
                IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
                Thing result = GenPlace.TryPlaceThing(待生成[i], dropSpot, map, ThingPlaceMode.Near);
                if (result == null)
                {
                    生成失败数++;
                    Log.Warning($"星际商店: 购买物品 {待生成[i].def.defName} 放置失败 (位置 {dropSpot})");
                }
            }
            交易数量.Clear();
            if (生成失败数 > 0)
            {
                Messages.Message("StarStore_PurchasePartialDropFail".Translate(生成失败数), MessageTypeDefOf.RejectInput);
            }
            else
            {
                Messages.Message("StarStore_PurchaseComplete".Translate(), MessageTypeDefOf.TaskCompletion);
            }
            刷新物品列表();
        }

        private void 执行卖出()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            float 总收益 = 0f;
            var 库存映射数据 = 获取库存映射(map);
            foreach (var kv in 交易数量)
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
                IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
                Thing result = GenPlace.TryPlaceThing(silver, dropSpot, map, ThingPlaceMode.Near);
                if (result == null)
                {
                    Log.Error($"星际商店: 出售收益白银放置失败 (数量 {白银数量}, 位置 {dropSpot})");
                    Messages.Message("StarStore_SaleSilverDropFail".Translate(白银数量), MessageTypeDefOf.RejectInput);
                }
            }
            交易数量.Clear();
            库存映射帧 = -1; // 出售后库存已变化，使缓存失效
            缓存白银帧 = -1; // 白银收益已生成，使缓存失效
            Messages.Message("StarStore_SaleComplete".Translate(白银数量), MessageTypeDefOf.TaskCompletion);
            刷新物品列表();
        }
    }
}
