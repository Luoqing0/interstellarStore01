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

        // 默认值常量（用于显示和重置）
        public const int 默认行数_默认 = 4;
        public const int 默认列数_默认 = 4;
        public const float 购买价格乘数_默认 = 1.6f;
        public const float 出售价格乘数_默认 = 0.8f;

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

            // 每行：标签(含当前值+默认值) | 滑块 | 重置按钮
            float 标签宽 = 200f;
            float 按钮宽 = 70f;
            float 间隙 = 8f;

            // 默认行数
            {
                Rect 行 = list.GetRect(30f);
                Rect 标签Rect = new Rect(行.x, 行.y, 标签宽, 行.height);
                Rect 按钮Rect = new Rect(行.xMax - 按钮宽, 行.y, 按钮宽, 行.height);
                Rect 滑块Rect = new Rect(标签Rect.xMax + 间隙, 行.y, 按钮Rect.xMin - 标签Rect.xMax - 间隙 * 2, 行.height);
                Widgets.Label(标签Rect, "StarStore_SettingsRows".Translate(设置.默认行数) + " " + "StarStore_Default".Translate(默认行数_默认));
                设置.默认行数 = (int)Widgets.HorizontalSlider(滑块Rect, 设置.默认行数, 2, 10, true);
                if (Widgets.ButtonText(按钮Rect, "StarStore_Reset".Translate()))
                    设置.默认行数 = 默认行数_默认;
            }
            // 默认列数
            {
                Rect 行 = list.GetRect(30f);
                Rect 标签Rect = new Rect(行.x, 行.y, 标签宽, 行.height);
                Rect 按钮Rect = new Rect(行.xMax - 按钮宽, 行.y, 按钮宽, 行.height);
                Rect 滑块Rect = new Rect(标签Rect.xMax + 间隙, 行.y, 按钮Rect.xMin - 标签Rect.xMax - 间隙 * 2, 行.height);
                Widgets.Label(标签Rect, "StarStore_SettingsCols".Translate(设置.默认列数) + " " + "StarStore_Default".Translate(默认列数_默认));
                设置.默认列数 = (int)Widgets.HorizontalSlider(滑块Rect, 设置.默认列数, 2, 10, true);
                if (Widgets.ButtonText(按钮Rect, "StarStore_Reset".Translate()))
                    设置.默认列数 = 默认列数_默认;
            }
            list.Gap();
            // 购买价格乘数
            {
                Rect 行 = list.GetRect(30f);
                Rect 标签Rect = new Rect(行.x, 行.y, 标签宽, 行.height);
                Rect 按钮Rect = new Rect(行.xMax - 按钮宽, 行.y, 按钮宽, 行.height);
                Rect 滑块Rect = new Rect(标签Rect.xMax + 间隙, 行.y, 按钮Rect.xMin - 标签Rect.xMax - 间隙 * 2, 行.height);
                Widgets.Label(标签Rect, "StarStore_SettingsBuyMultiplier".Translate(设置.购买价格乘数.ToString("F2")) + " " + "StarStore_Default".Translate(购买价格乘数_默认.ToString("F2")));
                设置.购买价格乘数 = Widgets.HorizontalSlider(滑块Rect, 设置.购买价格乘数, 0.1f, 5.0f, true);
                if (Widgets.ButtonText(按钮Rect, "StarStore_Reset".Translate()))
                    设置.购买价格乘数 = 购买价格乘数_默认;
            }
            // 出售价格乘数
            {
                Rect 行 = list.GetRect(30f);
                Rect 标签Rect = new Rect(行.x, 行.y, 标签宽, 行.height);
                Rect 按钮Rect = new Rect(行.xMax - 按钮宽, 行.y, 按钮宽, 行.height);
                Rect 滑块Rect = new Rect(标签Rect.xMax + 间隙, 行.y, 按钮Rect.xMin - 标签Rect.xMax - 间隙 * 2, 行.height);
                Widgets.Label(标签Rect, "StarStore_SettingsSellMultiplier".Translate(设置.出售价格乘数.ToString("F2")) + " " + "StarStore_Default".Translate(出售价格乘数_默认.ToString("F2")));
                设置.出售价格乘数 = Widgets.HorizontalSlider(滑块Rect, 设置.出售价格乘数, 0.1f, 5.0f, true);
                if (Widgets.ButtonText(按钮Rect, "StarStore_Reset".Translate()))
                    设置.出售价格乘数 = 出售价格乘数_默认;
            }
            list.Gap();
            // 重置窗口位置按钮
            if (list.ButtonText("StarStore_ResetWindowPos".Translate()))
            {
                设置.窗口X = -1f;
                设置.窗口Y = -1f;
                设置.Write();
            }
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
        // 窗口位置记忆（-1 表示未设置，使用默认居中）
        public float 窗口X = -1f;
        public float 窗口Y = -1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref 默认行数, "默认行数", 4);
            Scribe_Values.Look(ref 默认列数, "默认列数", 4);
            Scribe_Values.Look(ref 购买价格乘数, "购买价格乘数", 1.6f);
            Scribe_Values.Look(ref 出售价格乘数, "出售价格乘数", 0.8f);
            Scribe_Values.Look(ref 窗口X, "窗口X", -1f);
            Scribe_Values.Look(ref 窗口Y, "窗口Y", -1f);
        }
    }

} // namespace 星际商店

    /// <summary>
    /// 侧边栏配置Def - 看板娘/折扣/新闻/背景故事（XML可配置）
    /// AI 辅助生成
    /// </summary>
    public class StarStore_SidebarConfigDef : Def
    {
        /// <summary>看板娘图片路径（旧单一看板娘兼容字段）</summary>
        public string mascotTexturePath = "看板娘/starstore_mascot";

        /// <summary>多个看板娘 defName 列表（每次打开商店随机选一个）</summary>
        public List<string> mascotDefNames = new List<string>();

        /// <summary>随机问候语列表（旧单一看板娘兼容字段）</summary>
        public List<string> greetings = new List<string>();

        /// <summary>每日折扣物品 defName 列表（旧配置，useRandomCategoryDiscount=false 时生效）</summary>
        public List<string> dailyDiscountThingDefs = new List<string>();

        /// <summary>是否使用“随机类别+随机物品”的每日折扣（默认开启）</summary>
        public bool useRandomCategoryDiscount = true;

        /// <summary>折扣比例（0-1 之间，默认0.8）</summary>
        public float discountPercent = 0.8f;

        /// <summary>折扣黑名单：不会成为每日折扣物品的 ThingDef defName</summary>
        public List<string> discountBlacklist = new List<string>();

        /// <summary>折扣分类黑名单：不会进入每日折扣候选的分类 Key</summary>
        public List<string> discountCategoryBlacklist = new List<string>();

        /// <summary>是否将礼包内固定物品排除在每日折扣候选池之外</summary>
        public bool excludeBundleItemsFromDiscount = true;

        /// <summary>新闻公告列表</summary>
        public List<string> newsList = new List<string>();

        /// <summary>背景故事文本</summary>
        public string backstory = "星际商店，连接银河系各个角落的贸易枢纽。";

        /// <summary>侧边栏标题</summary>
        public string sidebarTitle = "星际商报";

        // 缓存
        private Texture2D _mascotTex;
        private string _lastTexPath = "";

        // AI 辅助生成：每日折扣缓存与手动刷新缓存分离，避免互相污染
        private int _lastDiscountDay = -1;
        private ThingDef _cachedDiscountThing;
        private int _devSeed = -1;
        private ThingDef _devDiscountThing;

        /// <summary>获取看板娘贴图（旧单一看板娘兼容）</summary>
        public Texture2D 获取看板娘贴图()
        {
            if (_mascotTex == null || _lastTexPath != mascotTexturePath)
            {
                _lastTexPath = mascotTexturePath;
                _mascotTex = ContentFinder<Texture2D>.Get(mascotTexturePath, false);
            }
            return _mascotTex;
        }

        /// <summary>从配置的 mascotDefNames 中随机选择一位看板娘</summary>
        public StarStore_MascotDef 随机看板娘()
        {
            if (mascotDefNames.NullOrEmpty()) return null;
            string name = mascotDefNames[Rand.Range(0, mascotDefNames.Count)];
            return DefDatabase<StarStore_MascotDef>.GetNamedSilentFail(name);
        }


        /// <summary>
        /// 根据游戏内天数获取每日折扣物品（每日自动刷新使用）
        /// 使用静态缓存、世界种子、价值加权、黑名单与历史去重
        /// </summary>
        public ThingDef 获取今日折扣物品(int 游戏天数)
        {
            // 缓存：同一天返回同一物品
            if (_lastDiscountDay == 游戏天数 && _cachedDiscountThing != null)
                return _cachedDiscountThing;

            // 旧配置兼容：关闭随机类别且提供了固定列表时，使用旧逻辑
            if (!useRandomCategoryDiscount && dailyDiscountThingDefs != null && dailyDiscountThingDefs.Count > 0)
            {
                int idx = (游戏天数 * 47) % dailyDiscountThingDefs.Count;
                _cachedDiscountThing = DefDatabase<ThingDef>.GetNamedSilentFail(dailyDiscountThingDefs[idx]);
                _lastDiscountDay = 游戏天数;
                return _cachedDiscountThing;
            }

            // 世界种子 + 游戏天数 确定性随机：同一世界同一天结果稳定
            int seed = (Find.World?.info?.Seed ?? 0) + 游戏天数 * 73856093;
            _cachedDiscountThing = 计算折扣物品BySeed(seed);
            _lastDiscountDay = 游戏天数;
            return _cachedDiscountThing;
        }

        /// <summary>
        /// 根据自定义种子获取手动刷新折扣物品（开发者手动刷新使用）
        /// 拥有独立缓存，不污染每日折扣缓存
        /// </summary>
        public ThingDef 获取手动折扣物品(int seed)
        {
            if (_devSeed == seed && _devDiscountThing != null)
                return _devDiscountThing;

            _devDiscountThing = 计算折扣物品BySeed(seed);
            _devSeed = seed;
            return _devDiscountThing;
        }

        /// <summary>
        /// 按种子计算折扣物品的公共逻辑
        /// </summary>
        private ThingDef 计算折扣物品BySeed(int seed)
        {
            var 分类物品表 = 星际商店.星际商店折扣缓存.获取分类物品表();
            if (分类物品表 == null || 分类物品表.Count == 0)
                return null;

            var 候选分类 = 分类物品表.Keys.ToList();
            if (discountCategoryBlacklist != null)
                候选分类.RemoveAll(discountCategoryBlacklist.Contains);
            候选分类.RemoveAll(c => !分类物品表.ContainsKey(c) || 分类物品表[c].Count == 0);
            if (候选分类.Count == 0)
                return null;

            Rand.PushState(seed);

            string 选中分类 = 候选分类[Rand.Range(0, 候选分类.Count)];
            var 物品列表 = 分类物品表[选中分类]
                .Where(d => d.BaseMarketValue >= 5f)
                .Where(d => discountBlacklist == null || !discountBlacklist.Contains(d.defName))
                .Where(d => !excludeBundleItemsFromDiscount || !星际商店.星际商店折扣缓存.获取礼包物品集合().Contains(d.defName))
                .ToList();

            ThingDef 选中物品 = null;
            if (物品列表.Count > 0)
            {
                // 价值加权随机：价值越高权重越大
                float total = 物品列表.Sum(d => d.BaseMarketValue);
                float roll = Rand.Range(0f, total);
                foreach (var d in 物品列表)
                {
                    roll -= d.BaseMarketValue;
                    if (roll <= 0f)
                    {
                        选中物品 = d;
                        break;
                    }
                }
                if (选中物品 == null) 选中物品 = 物品列表.Last();

                // 历史去重：命中最近 7 天历史则重抽一次
                var comp = Current.Game?.GetComponent<星际商店.星际商店GameComponent>();
                if (comp != null && comp.历史折扣物品 != null && comp.历史折扣物品.Contains(选中物品.defName))
                {
                    物品列表.Remove(选中物品);
                    if (物品列表.Count > 0)
                        选中物品 = 物品列表[Rand.Range(0, 物品列表.Count)];
                }
            }

            Rand.PopState();

            return 选中物品;
        }

        /// <summary>根据游戏内天数获取新闻（返回 null 表示无新闻）</summary>
        public string 获取今日新闻(int 游戏天数)
        {
            if (newsList == null || newsList.Count == 0) return null;
            int idx = (游戏天数 * 31) % newsList.Count;
            return newsList[idx];
        }

        /// <summary>获取折扣比例（百分比 0-1）</summary>
        public float 获取折扣比例()
        {
            return discountPercent <= 0 || discountPercent >= 1 ? 0.8f : discountPercent;
        }
    }

    /// <summary>
    /// 侧边栏管理器
    /// </summary>
    public static class 侧边栏管理器
    {
        private static StarStore_SidebarConfigDef _config;
        public static StarStore_SidebarConfigDef 配置
        {
            get
            {
                if (_config == null)
                    _config = DefDatabase<StarStore_SidebarConfigDef>.GetNamedSilentFail("StarStore_DefaultSidebar");
                return _config;
            }
        }
    }

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

        // AI 辅助生成：宽松匹配——当任一方的 quality/stuff 为 null 时视为通配
        // 用于卖出模式：网格上选的物品 key 为 (def, null, null)，但库存物品有实际品质/材料
        // 严格 Equals 会返回 false 导致 >> 按钮返回 0、卖出执行提示库存不足
        public bool 宽松匹配(TransactionKey other)
        {
            if (def != other.def) return false;
            // 任一方 quality 为 null 时视为匹配
            if (quality.HasValue && other.quality.HasValue && quality.Value != other.quality.Value) return false;
            // 任一方 stuff 为 null 时视为匹配
            if (stuff != null && other.stuff != null && stuff != other.stuff) return false;
            return true;
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
        /// <summary>玩家收藏的物品 defName 集合（跨存档）</summary>
        public HashSet<string> 收藏列表 = new HashSet<string>();

        /// <summary>最近几天的每日折扣物品，用于去重</summary>
        public List<string> 历史折扣物品 = new List<string>();

        /// <summary>历史折扣保留长度（最近 N 天）</summary>
        public const int 历史折扣保留天数 = 7;

        // AI 辅助生成：RimWorld 1.6 通过 Activator.CreateInstance(type, game) 调用 GameComponent(Game)
        public 星际商店GameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref 收藏列表, "收藏列表", LookMode.Value);
            Scribe_Collections.Look(ref 历史折扣物品, "历史折扣物品", LookMode.Value);
            if (收藏列表 == null) 收藏列表 = new HashSet<string>();
            if (历史折扣物品 == null) 历史折扣物品 = new List<string>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit) 清理失效收藏();
        }

        /// <summary>清理已不存在的 Def 引用，避免移除模组后报错</summary>
        public void 清理失效收藏()
        {
            收藏列表.RemoveWhere(name =>
                string.IsNullOrEmpty(name) || DefDatabase<ThingDef>.GetNamedSilentFail(name) == null);
            历史折扣物品.RemoveAll(name =>
                string.IsNullOrEmpty(name) || DefDatabase<ThingDef>.GetNamedSilentFail(name) == null);
        }

        public bool 是否收藏(string defName)
        {
            return 收藏列表 != null && 收藏列表.Contains(defName);
        }

        public void 切换收藏(string defName)
        {
            if (收藏列表 == null) 收藏列表 = new HashSet<string>();
            if (收藏列表.Contains(defName))
                收藏列表.Remove(defName);
            else
                收藏列表.Add(defName);
        }

        /// <summary>记录新的历史折扣物品，并只保留最近 N 条</summary>
        public void 记录历史折扣(string defName)
        {
            if (历史折扣物品 == null) 历史折扣物品 = new List<string>();
            if (!历史折扣物品.Contains(defName))
                历史折扣物品.Add(defName);
            while (历史折扣物品.Count > 历史折扣保留天数)
                历史折扣物品.RemoveAt(0);
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
