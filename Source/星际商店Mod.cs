using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 模组主类 - 负责初始化、设置界面和存档数据
    /// </summary>
    public class 星际商店Mod : Mod
    {
        public static 星际商店Mod Instance;
        public static 商店设置 设置;

        public 星际商店Mod(ModContentPack content) : base(content)
        {
            Instance = this;
            设置 = GetSettings<商店设置>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.Label("StarStore_SettingsTitle".Translate());
            list.Gap();
            list.Label("StarStore_SettingsRows".Translate(设置.默认行数));
            设置.默认行数 = (int)list.Slider(设置.默认行数, 2, 10);
            list.Label("StarStore_SettingsCols".Translate(设置.默认列数));
            设置.默认列数 = (int)list.Slider(设置.默认列数, 2, 10);
            list.Gap();
            list.Label("StarStore_SettingsBuyMultiplier".Translate(设置.购买价格乘数.ToString("F2")));
            设置.购买价格乘数 = list.Slider(设置.购买价格乘数, 0.1f, 5.0f);
            list.Label("StarStore_SettingsSellMultiplier".Translate(设置.出售价格乘数.ToString("F2")));
            设置.出售价格乘数 = list.Slider(设置.出售价格乘数, 0.1f, 5.0f);
            list.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "星际商店";
        }
    }

    /// <summary>
    /// 商店设置数据类（继承 ModSettings 实现持久化）
    /// </summary>
    public class 商店设置 : ModSettings
    {
        public int 默认行数 = 4;
        public int 默认列数 = 4;
        public float 购买价格乘数 = 1.6f;
        public float 出售价格乘数 = 0.8f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref 默认行数, "默认行数", 4);
            Scribe_Values.Look(ref 默认列数, "默认列数", 4);
            Scribe_Values.Look(ref 购买价格乘数, "购买价格乘数", 1.6f);
            Scribe_Values.Look(ref 出售价格乘数, "出售价格乘数", 0.8f);
        }
    }

} // namespace 星际商店

    // ================================================================
    //  交易条件相关类型（必须在全局命名空间，否则 RW XML 解析器找不到）
    // ================================================================

    /// <summary>
    /// 单个物品的交易条件
    /// </summary>
    public class 物品交易条件
    {
        public string thingDef;           // 物品Def名称
        public string requiredResearch;   // 需要的研究项目Def名称（可选）
        public string requiredItem;       // 需要的殖民地物品Def名称（可选）
        public bool 隐藏 = false;         // 是否在商店中隐藏此物品（开发者模式下可见）

        /// <summary>
        /// 检查此条件是否满足
        /// </summary>
        public bool 是否满足(Map map)
        {
            // 检查研究项目
            if (!string.IsNullOrEmpty(requiredResearch))
            {
                ResearchProjectDef researchDef = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(requiredResearch);
                if (researchDef != null && !researchDef.IsFinished)
                    return false;
            }

            // 检查殖民地物品
            if (!string.IsNullOrEmpty(requiredItem))
            {
                if (map == null) return false;
                ThingDef itemDef = DefDatabase<ThingDef>.GetNamedSilentFail(requiredItem);
                if (itemDef != null)
                {
                    List<Thing> things = map.listerThings.ThingsOfDef(itemDef);
                    int count = 0;
                    for (int i = 0; i < things.Count; i++)
                        count += things[i].stackCount;
                    if (count <= 0)
                        return false;
                }
            }

            return true;
        }
    }

namespace 星际商店
{

    /// <summary>
    /// 交易物品的唯一标识（物品类型 + 品质 + 材料）
    /// </summary>
    public struct TransactionKey : IEquatable<TransactionKey>
    {
        public ThingDef def;
        public QualityCategory? quality;
        public ThingDef stuff;

        public TransactionKey(ThingDef def, QualityCategory? quality = null, ThingDef stuff = null)
        {
            this.def = def;
            this.quality = quality;
            this.stuff = stuff;
        }

        // 从实际 Thing 构造
        public TransactionKey(Thing thing)
        {
            this.def = thing.def;
            this.quality = (thing.TryGetQuality(out QualityCategory qc)) ? qc : (QualityCategory?)null;
            this.stuff = thing.Stuff;
        }

        public bool Equals(TransactionKey other)
        {
            return def == other.def && quality == other.quality && stuff == other.stuff;
        }

        public override bool Equals(object obj) => obj is TransactionKey k && Equals(k);
        public override int GetHashCode()
        {
            int hash = def.GetHashCode();
            hash = (hash * 397) ^ (quality?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (stuff?.GetHashCode() ?? 0);
            return hash;
        }

        public override string ToString()
        {
            string s = def.label;
            if (quality != null) s = quality.Value.GetLabel() + s;
            if (stuff != null) s = stuff.LabelAsStuff + "制" + s;
            return s;
        }
    }

} // namespace 星际商店

    /// <summary>
    /// 交易条件Def - 从XML加载，包含所有物品的交易条件列表
    /// </summary>
    public class StarStore_TradeConditionDef : Def
    {
        public List<物品交易条件> conditions;

        /// <summary>
        /// 获取某个物品的交易条件
        /// </summary>
        public 物品交易条件 获取条件(string thingDefName)
        {
            if (conditions == null) return null;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].thingDef == thingDefName)
                    return conditions[i];
            }
            return null;
        }

        /// <summary>
        /// 设置或更新某个物品的交易条件
        /// </summary>
        public void 设置条件(string thingDefName, string requiredResearch, string requiredItem)
        {
            if (conditions == null)
                conditions = new List<物品交易条件>();

            物品交易条件 现有 = 获取条件(thingDefName);
            if (现有 != null)
            {
                现有.requiredResearch = requiredResearch;
                现有.requiredItem = requiredItem;
            }
            else
            {
                conditions.Add(new 物品交易条件
                {
                    thingDef = thingDefName,
                    requiredResearch = requiredResearch,
                    requiredItem = requiredItem
                });
            }
        }

        /// <summary>
        /// 删除某个物品的交易条件
        /// </summary>
        public void 删除条件(string thingDefName)
        {
            if (conditions == null) return;
            conditions.RemoveAll(c => c.thingDef == thingDefName);
        }

        /// <summary>
        /// 检查某个物品是否可以交易（满足所有前置条件）
        /// </summary>
        public bool 是否可以交易(string thingDefName, Map map)
        {
            物品交易条件 条件 = 获取条件(thingDefName);
            if (条件 == null) return true; // 没有条件则允许交易
            return 条件.是否满足(map);
        }

        /// <summary>
        /// 检查某个物品是否被隐藏
        /// </summary>
        public bool 是否隐藏(string thingDefName)
        {
            物品交易条件 条件 = 获取条件(thingDefName);
            if (条件 == null) return false;
            return 条件.隐藏;
        }

        /// <summary>
        /// 设置隐藏某个物品
        /// </summary>
        public void 设置隐藏(string thingDefName)
        {
            if (conditions == null)
                conditions = new List<物品交易条件>();

            物品交易条件 现有 = 获取条件(thingDefName);
            if (现有 != null)
            {
                现有.隐藏 = true;
            }
            else
            {
                conditions.Add(new 物品交易条件
                {
                    thingDef = thingDefName,
                    隐藏 = true
                });
            }
        }

        /// <summary>
        /// 取消隐藏某个物品
        /// </summary>
        public void 取消隐藏(string thingDefName)
        {
            物品交易条件 条件 = 获取条件(thingDefName);
            if (条件 != null)
            {
                条件.隐藏 = false;
            }
        }
    }

namespace 星际商店
{

    /// <summary>
    /// 游戏组件 - 持久化收藏列表等跨存档数据
    /// </summary>
    public class 星际商店GameComponent : GameComponent
    {
        public HashSet<string> 收藏列表 = new HashSet<string>();

        public 星际商店GameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref 收藏列表, "收藏列表", LookMode.Value);
            if (收藏列表 == null) 收藏列表 = new HashSet<string>();
        }
    }

    /// <summary>
    /// 交易条件管理器 - 管理所有交易条件
    /// </summary>
    public static class 交易条件管理器
    {
        private static StarStore_TradeConditionDef _条件Def;

        /// <summary>
        /// 获取交易条件Def（从XML加载）
        /// </summary>
        public static StarStore_TradeConditionDef 条件Def
        {
            get
            {
                if (_条件Def == null)
                {
                    _条件Def = DefDatabase<StarStore_TradeConditionDef>.GetNamedSilentFail("StarStore_DefaultConditions");
                    if (_条件Def == null)
                    {
                        // 如果没有XML配置，创建一个默认的空配置
                        _条件Def = new StarStore_TradeConditionDef();
                        _条件Def.defName = "StarStore_DefaultConditions";
                        _条件Def.conditions = new List<物品交易条件>();
                    }
                }
                return _条件Def;
            }
        }

        /// <summary>
        /// 检查物品是否可以交易
        /// </summary>
        public static bool 是否可以交易(string thingDefName, Map map)
        {
            return 条件Def.是否可以交易(thingDefName, map);
        }

        /// <summary>
        /// 获取物品的交易条件描述
        /// </summary>
        public static string 获取条件描述(string thingDefName)
        {
            物品交易条件 条件 = 条件Def.获取条件(thingDefName);
            if (条件 == null) return "";

            string desc = "";
            if (!string.IsNullOrEmpty(条件.requiredResearch))
            {
                ResearchProjectDef rp = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(条件.requiredResearch);
                string 名称 = rp != null ? rp.label : 条件.requiredResearch;
                desc = "StarStore_RequireResearch".Translate(名称);
            }
            if (!string.IsNullOrEmpty(条件.requiredItem))
            {
                if (desc != "") desc += "\n";
                ThingDef td = DefDatabase<ThingDef>.GetNamedSilentFail(条件.requiredItem);
                string 名称 = td != null ? td.label : 条件.requiredItem;
                desc += "StarStore_RequireItem".Translate(名称);
            }
            return desc;
        }
    }
}
