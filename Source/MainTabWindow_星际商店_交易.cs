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

            // 折扣检查（AI 辅助生成：使用共享折扣物品，与看板刷新同步）
            if (当前折扣物品 != null && def.defName == 当前折扣物品.defName)
            {
                StarStore_SidebarConfigDef 折扣cfg2 = 侧边栏管理器.配置;
                if (折扣cfg2 != null)
                    价格 *= 折扣cfg2.获取折扣比例();
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
        // AI 辅助生成：只统计当前可用白银，排除被小人预留或持有的堆叠
        private int 获取白银总量(Map map)
        {
            if (map == null) return 0;
            int currentFrame = Time.frameCount;
            if (缓存白银帧 == currentFrame && 缓存白银总量 >= 0)
                return 缓存白银总量;

            int total = 0;
            List<Thing> silverList = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            for (int idx = 0; idx < silverList.Count; idx++)
            {
                Thing silver = silverList[idx];
                if (白银可用(silver, map))
                    total += silver.stackCount;
            }
            缓存白银总量 = total;
            缓存白银帧 = currentFrame;
            return 缓存白银总量;
        }

        /// <summary>
        /// 判断一堆白银是否可以被商店交易使用
        /// </summary>
        private bool 白银可用(Thing silver, Map map)
        {
            if (silver == null || silver.Destroyed || !silver.Spawned || silver.Map != map)
                return false;
            if (map.reservationManager.IsReserved(silver))
                return false;
            if (ThingOwnerUtility.AnyParentIs<Pawn>(silver))
                return false;
            return true;
        }

        // ================================================================
        //  分类判断 - 使用递归检查所有父分类
        // ================================================================
        private bool 物品属于分类(ThingDef def, string 分类Key)
        {
            if (分类Key == "StarStore_All") return true;
            if (分类Key == "StarStore_Favorites") return 收藏列表.Contains(def.defName);
            return 星际商店工具.物品属于具体分类(def, 分类Key);
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

            IEnumerable<ThingDef> query;

            // AI 辅助生成：机械族使用单独缓存列表（tradeability 通常为 None）
            if (当前显示机械族)
            {
                query = 机械族管理器.获取所有机械族Race();
            }
            else
            {
                query = DefDatabase<ThingDef>.AllDefs
                    .Where(d => !d.destroyOnDrop && d.BaseMarketValue > 0f && !d.thingCategories.NullOrEmpty());

                // 按购买/卖出模式区分可交易性
                if (是购买模式)
                    query = query.Where(d => d.tradeability == Tradeability.Buyable || d.tradeability == Tradeability.All);
                else
                    query = query.Where(d => d.tradeability == Tradeability.Sellable || d.tradeability == Tradeability.All);
            }

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
            if (!是购买模式 && !当前显示机械族)
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
            // AI 辅助生成：同时检查 ThingDef.researchPrerequisites 与 recipeMaker 中的研究前提
            if (仅已解锁科技 && !当前显示机械族)
                query = query.Where(d => 已解锁所有研究(d));

            // 模组筛选
            if (筛选模组 != "StarStore_AllMods")
            {
                query = query.Where(d => (d.modContentPack?.Name ?? "Unknown") == 筛选模组);
            }

            // AI 辅助生成：排序支持（名称/价格/科技，正序/倒序）
            IOrderedEnumerable<ThingDef> ordered;
            switch (当前排序方式)
            {
                case 排序方式.价格:
                    ordered = query.OrderBy(d => d.BaseMarketValue);
                    break;
                case 排序方式.科技:
                    ordered = query.OrderBy(d => d.techLevel);
                    break;
                case 排序方式.名称:
                default:
                    ordered = query.OrderBy(d => d.label);
                    break;
            }
            当前显示物品 = (排序正序 ? ordered : ordered.Reverse()).ToList();
        }

        /// <summary>
        /// 完整检查 ThingDef 本身与 recipeMaker 中的研究前提是否全部完成
        /// </summary>
        private bool 已解锁所有研究(ThingDef def)
        {
            if (def.researchPrerequisites != null)
                foreach (var r in def.researchPrerequisites)
                    if (!r.IsFinished) return false;

            if (def.recipeMaker != null)
            {
                if (def.recipeMaker.researchPrerequisite != null && !def.recipeMaker.researchPrerequisite.IsFinished)
                    return false;
                if (def.recipeMaker.researchPrerequisites != null)
                    foreach (var r in def.recipeMaker.researchPrerequisites)
                        if (!r.IsFinished) return false;
            }
            return true;
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

                // AI 辅助生成：机械族购买分支
                if (key.def.race != null && key.def.race.IsMechanoid)
                {
                    PawnKindDef kindDef = 机械族管理器.获取主要Kind(key.def);
                    if (kindDef == null)
                    {
                        Log.Error($"星际商店: 无法找到 {key.def.defName} 的机械族 PawnKindDef，跳过");
                        Messages.Message("StarStore_CreateItemFailed".Translate(key.def.label), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    try
                    {
                        for (int i = 0; i < kv.Value; i++)
                        {
                            Pawn pawn = PawnGenerator.GeneratePawn(kindDef, Faction.OfMechanoids);
                            待生成.Add(pawn);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"星际商店: 无法生成机械族 {key.def.defName}: {ex.Message}");
                        Messages.Message("StarStore_CreateItemFailed".Translate(key.def.label), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    continue;
                }

                // 动物（Pawn）特殊处理：ThingMaker.MakeThing 会创建畸形 Pawn，
                // 缺少 faction/kindDef/年龄等初始化，生成到地图后会污染 MapPawns 内部列表，
                // 导致 MapPawns.get_AllPawnsUnspawned() NRE → 游戏卡死
                if (key.def.race != null)
                {
                    PawnKindDef kindDef = DefDatabase<PawnKindDef>.AllDefs
                        .FirstOrDefault(k => k.race == key.def);
                    if (kindDef == null)
                    {
                        Log.Error($"星际商店: 无法找到 {key.def.defName} 的 PawnKindDef，跳过");
                        Messages.Message("StarStore_CreateItemFailed".Translate(key.def.label), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    try
                    {
                        for (int i = 0; i < kv.Value; i++)
                        {
                            Pawn pawn = PawnGenerator.GeneratePawn(kindDef, Faction.OfPlayer);
                            待生成.Add(pawn);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"星际商店: 无法生成动物 {key.def.defName}: {ex.Message}");
                        Messages.Message("StarStore_CreateItemFailed".Translate(key.def.label), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    continue;
                }

                ThingDef stuff = key.stuff;
                if (key.def.MadeFromStuff && stuff == null)
                    stuff = key.def.defaultStuff ?? ThingDefOf.Steel;

                try
                {
                    int 剩余数量 = kv.Value;
                    while (剩余数量 > 0)
                    {
                        Thing 物品 = ThingMaker.MakeThing(key.def, stuff);
                        if (key.quality != null)
                        {
                            var comp = 物品.TryGetComp<CompQuality>();
                            if (comp != null) comp.SetQuality(key.quality.Value, ArtGenerationContext.Outsider);
                        }
                        int 本次数量 = Mathf.Min(剩余数量, key.def.stackLimit);
                        物品.stackCount = 本次数量;
                        待生成.Add(物品);
                        剩余数量 -= 本次数量;
                    }
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
                if (silver == null || silver.Destroyed || !白银可用(silver, map)) { si++; continue; }
                int 扣除 = Mathf.Min(剩余白银, silver.stackCount);
                剩余白银 -= 扣除;
                silver.SplitOff(扣除);
                si++;
            }
            缓存白银帧 = -1;

            if (剩余白银 > 0)
            {
                Messages.Message("StarStore_InsufficientSilver".Translate(需要白银, 获取白银总量(map)), MessageTypeDefOf.RejectInput);
                return;
            }

            // AI 辅助生成：使用运输舱生成物品（优先轨道交易信标位置）
            // 统一处理普通物品与动物：DropPodUtility.DropThingsNear 内部会把每个 Thing（含 Pawn）
            // 装入 ActiveTransporterInfo.innerContainer，落地后自动生成，因此动物无需单独 GenSpawn。
            IntVec3 dropSpot = 获取有效降落点(map);
            if (dropSpot.IsValid && dropSpot.InBounds(map) && !dropSpot.Roofed(map))
            {
                // 方案A：运输舱投放（室外/无屋顶）
                DropPodUtility.DropThingsNear(dropSpot, map, 待生成, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: false, allowFogged: false);
                Messages.Message("StarStore_PurchaseComplete".Translate(), new LookTargets(dropSpot, map), MessageTypeDefOf.TaskCompletion);
            }
            else
            {
                // 方案B：直接生成在地面（室内/屋顶下，运输舱无法通过）
                IntVec3 center = 获取室内生成点(map);
                for (int i = 0; i < 待生成.Count; i++)
                {
                    if (待生成[i] is Pawn pawn)
                        GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(center, map, 10), map, WipeMode.Vanish);
                    else
                        GenPlace.TryPlaceThing(待生成[i], center, map, ThingPlaceMode.Near);
                }
                Messages.Message("StarStore_PurchaseIndoor".Translate(), new LookTargets(center, map), MessageTypeDefOf.TaskCompletion);
            }
            购买交易数量.Clear();
            刷新物品列表();
        }

        // 出售执行计划：避免先扣后失败导致物品被吞
        private class 出售计划项
        {
            public TransactionKey key;
            public List<Thing> candidates;
            public int amount;
        }

        private void 执行卖出()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            float 总收益 = 0f;
            var 库存映射数据 = 获取库存映射(map);

            // AI 辅助生成：两阶段交易
            // 阶段1：预检查所有购物车项的库存是否都充足，任一不足则整单取消，不会扣除任何物品
            List<出售计划项> 执行计划 = new List<出售计划项>();
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

                执行计划.Add(new 出售计划项 { key = kv.Key, candidates = candidates, amount = kv.Value });
            }

            // 阶段2：全部预检查通过后，统一扣除库存并累加收益
            foreach (var plan in 执行计划)
            {
                int 剩余卖出 = plan.amount;
                for (int j = 0; j < plan.candidates.Count && 剩余卖出 > 0; j++)
                {
                    Thing t = plan.candidates[j];
                    if (t == null || t.Destroyed) continue;
                    int 本次卖出数 = Mathf.Min(剩余卖出, t.stackCount);
                    // 使用与 UI 一致的出售价格公式，并考虑物品耐久度
                    float 基础单价 = 获取出售价格(plan.key.def, plan.key.quality, plan.key.stuff);
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

                IntVec3 saleDropSpot = 获取有效降落点(map);
                if (saleDropSpot.IsValid && saleDropSpot.InBounds(map) && !saleDropSpot.Roofed(map))
                {
                    // 方案A：运输舱投放（室外/无屋顶）
                    DropPodUtility.DropThingsNear(saleDropSpot, map, new List<Thing> { silver });
                    Messages.Message("StarStore_SaleComplete".Translate(白银数量), new LookTargets(saleDropSpot, map), MessageTypeDefOf.TaskCompletion);
                }
                else
                {
                    // 方案B：直接生成在地面（室内/屋顶下）
                    IntVec3 center = 获取室内生成点(map);
                    GenPlace.TryPlaceThing(silver, center, map, ThingPlaceMode.Near);
                    Messages.Message("StarStore_SaleIndoor".Translate(白银数量), new LookTargets(center, map), MessageTypeDefOf.TaskCompletion);
                }
            }
            出售交易数量.Clear();
            库存映射帧 = -1; // 出售后库存已变化，使缓存失效
            缓存白银帧 = -1; // 白银收益已生成，使缓存失效
            刷新物品列表();
        }

        // AI 辅助生成：室内生成点（用于全室内地图无运输舱投放时）
        private IntVec3 获取室内生成点(Map map)
        {
            // 优先使用选中的殖民者位置
            Pawn 选中殖民者 = Find.Selector.SingleSelectedThing as Pawn;
            if (选中殖民者 != null)
                return 选中殖民者.Position;

            // 回退到地图中心附近的可通行格
            IntVec3 center = map.Center;
            if (center.Walkable(map))
                return center;

            return CellFinder.RandomClosewalkCellNear(center, map, 10);
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
