using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 主窗口 - 星际商店的购买/卖出面板
    /// </summary>
    public partial class MainTabWindow_星际商店 : MainTabWindow
    {
        public static MainTabWindow_星际商店 Instance;

        // ===== 模式 =====
        private bool 是购买模式 = true;
        private bool 仅显示库存 = false;

        // ===== 滚动位置 =====
        private Vector2 网格滚动位置 = Vector2.zero;

        // ===== 数据 =====
        private List<ThingDef> 当前显示物品 = new List<ThingDef>();
        private Dictionary<TransactionKey, int> 购买交易数量 = new Dictionary<TransactionKey, int>();
        private Dictionary<TransactionKey, int> 出售交易数量 = new Dictionary<TransactionKey, int>();
        private Dictionary<TransactionKey, int> 当前交易数量 => 是购买模式 ? 购买交易数量 : 出售交易数量;
        private string 当前分类标签 = "StarStore_All";

        // ===== 库存映射帧级缓存（出售模式性能优化） =====
        private Dictionary<ThingDef, List<Thing>> 库存映射 = new Dictionary<ThingDef, List<Thing>>();
        private int 库存映射帧 = -1;

        // ===== 收藏（通过 GameComponent 持久化） =====
        private HashSet<string> 收藏列表
        {
            get
            {
                if (Current.Game == null) return _fallbackFavorites;
                var comp = Current.Game.GetComponent<星际商店GameComponent>();
                return comp?.收藏列表 ?? _fallbackFavorites;
            }
        }
        private static HashSet<string> _fallbackFavorites = new HashSet<string>();

        // ===== 筛选 =====
        private bool 显示筛选面板 = false;
        private bool 仅已解锁科技 = false;
        private string 搜索文本 = "";
        private string 筛选模组 = "StarStore_AllMods"; // "StarStore_AllMods" 表示全部模组
        private List<string> 可选模组列表 = new List<string>();
        private bool 模组列表已初始化 = false;

        // ===== 翻页 =====
        private int 当前页码 = 1;
        private string 跳转页码输入 = "";
        private const int 每页物品数 = 50;

        // ===== 购买品质/材料选择 =====
        private Dictionary<ThingDef, QualityCategory?> 购买品质选择 = new Dictionary<ThingDef, QualityCategory?>();
        private Dictionary<ThingDef, ThingDef> 购买材料选择 = new Dictionary<ThingDef, ThingDef>();

        // ===== 购物车 =====
        private bool 显示购物车 = true;
        private Vector2 购物车滚动位置 = Vector2.zero;
        private Vector2 购物车购买滚动 = Vector2.zero;
        private Vector2 购物车出售滚动 = Vector2.zero;

        // ===== 开发者模式 - 右键配置 =====
        private ThingDef 右键配置物品 = null;
        private Vector2 配置窗口滚动位置 = Vector2.zero;
        private string 配置研究输入 = "";
        private string 配置物品输入 = "";
        private string 配置提示信息 = "";

        // ===== 行/列输入持久化字符串 =====
        private string 行数字符串 = "4";
        private string 列数字符串 = "4";

        // ===== 布局参数 =====
        public static int 行数 = 4;
        public static int 列数 = 4;
        private const float 间距 = 6f;
        private const float 左侧分类栏宽 = 140f;

        // 格子尺寸限制（像素）
        private const float 格子最小尺寸 = 80f;
        private const float 格子最大尺寸 = 200f;

        // ===== 集中化布局常量 =====
        private const float 标题栏高 = 38f;
        private const float 标题栏Y偏移 = 3f;
        private const float 水平内边距 = 5f;
        private const float 区域间距 = 2f;
        private const float 分类标签栏高 = 28f;
        private const float 按钮标准宽 = 60f;
        private const float 按钮标准高 = 26f;
        private const float 搜索框宽 = 200f;
        private const float 搜索框高 = 22f;
        private const float 行列输入宽 = 24f;
        private const float 行列输入高 = 18f;
        private const float 购物车宽 = 200f;
        private const float 购物车间距 = 4f;
        private const float 底部栏Y偏移 = 38f;
        private const float 底部栏高 = 35f;
        private const float 网格底部留白 = 64f;
        private const float 网格内边距 = 16f;
        private const float 卡片内边距 = 4f;
        private const float 图标最大尺寸 = 38f;
        private const float 图标尺寸比例 = 0.40f;

        // ===== 淘宝×科幻电商风格颜色 =====
        // 基底 - 深邃太空背景
        private static readonly Color 背景色 = new Color(0.06f, 0.06f, 0.12f);
        private static readonly Color 渐变顶色 = new Color(0.08f, 0.08f, 0.16f);
        private static readonly Color 渐变底色 = new Color(0.04f, 0.04f, 0.08f);

        // 卡片 - 毛玻璃深色卡片
        private static readonly Color 格子背景色 = new Color(0.10f, 0.10f, 0.20f);
        private static readonly Color 格子hover色 = new Color(0.15f, 0.12f, 0.25f);
        private static readonly Color 边框色 = new Color(0.18f, 0.16f, 0.28f);

        // 主色调 - 暖橙色（淘宝品牌色调）
        private static readonly Color 主色调 = new Color(1.0f, 0.55f, 0.15f);      // 淘宝橙
        private static readonly Color 主色调hover = new Color(1.0f, 0.65f, 0.25f);
        private static readonly Color 主色调暗 = new Color(0.8f, 0.40f, 0.10f);

        // 高亮/科技点缀 - 青色霓虹
        private static readonly Color 高亮边框色 = new Color(0.2f, 0.85f, 0.85f);  // 青色
        private static readonly Color 高亮色暗 = new Color(0.1f, 0.55f, 0.55f);

        // 文字
        private static readonly Color 文字色 = new Color(0.90f, 0.92f, 0.95f);     // 亮白
        private static readonly Color 次要文字色 = new Color(0.55f, 0.58f, 0.65f); // 灰

        // 价格 - 金色（醒目）
        private static readonly Color 价格色 = new Color(1.0f, 0.75f, 0.2f);       // 暖金
        private static readonly Color 原价色 = new Color(0.5f, 0.45f, 0.4f);       // 灰色原价

        // 按钮
        private static readonly Color 按钮色 = new Color(0.12f, 0.10f, 0.22f);
        private static readonly Color 按钮hover色 = new Color(0.20f, 0.15f, 0.35f);
        private static readonly Color 按钮选中色 = new Color(主色调.r * 0.7f, 主色调.g * 0.7f, 主色调.b * 0.7f);

        // 分类栏
        private static readonly Color 分类栏背景 = new Color(0.08f, 0.07f, 0.15f);

        // 语义色
        private static readonly Color 条件未满足色 = new Color(0.25f, 0.10f, 0.12f); // 暗红
        private static readonly Color 购买按钮色 = new Color(1.0f, 0.5f, 0.15f);     // 淘宝橙（购买主按钮）
        private static readonly Color 出售按钮色 = new Color(0.15f, 0.7f, 0.4f);     // 绿色（出售主按钮）
        private static readonly Color 按钮不可用色 = new Color(0.12f, 0.12f, 0.15f);

        // 购物车 - 半透明玻璃
        private static readonly Color 购物车背景 = new Color(0.06f, 0.06f, 0.16f, 0.95f);
        private static readonly Color 购物车分隔 = new Color(0.15f, 0.13f, 0.25f);

        // 推荐/标签
        private static readonly Color 推荐标签色 = new Color(1.0f, 0.35f, 0.1f);    // 红色推荐标签
        private static readonly Color 新品标签色 = new Color(0.2f, 0.8f, 0.5f);     // 绿色新品标签

        // ===== 预定义分类列表 =====
        private static List<string> 预定义分类 = new List<string>
        {
            "StarStore_All", "StarStore_Favorites",
            "StarStore_Cat_Food", "StarStore_Cat_Medicine", "StarStore_Cat_Weapons",
            "StarStore_Cat_Apparel", "StarStore_Cat_RawMaterials",
            "StarStore_Cat_Manufactured", "StarStore_Cat_Buildings", "StarStore_Cat_Furniture",
            "StarStore_Cat_Electronics", "StarStore_Cat_Misc"
        };

        // ===== 窗口大小 =====
        public override Vector2 RequestedTabSize
        {
            get { return new Vector2(1000f, 760f); }
        }

        // 窗口位置居中
        public override void PostOpen()
        {
            base.PostOpen();
            windowRect.x = (UI.screenWidth - windowRect.width) / 2f;
            windowRect.y = (UI.screenHeight - windowRect.height) / 2f;
        }

        public override void OnAcceptKeyPressed()
        {
            // 阻止 Enter 键关闭商店界面
            // 不调用 base.OnAcceptKeyPressed()
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Instance = this;
            if (星际商店Mod.设置 != null)
            {
                行数 = 星际商店Mod.设置.默认行数;
                列数 = 星际商店Mod.设置.默认列数;
            }
            // 确保 GameComponent 存在（收藏列表持久化用）
            if (Current.Game != null && Current.Game.GetComponent<星际商店GameComponent>() == null)
            {
                Current.Game.components.Add(new 星际商店GameComponent(Current.Game));
            }
            刷新物品列表();
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 绘制渐变背景（顶部亮→底部暗）
            GUI.color = 渐变顶色;
            Widgets.DrawRectFast(new Rect(inRect.x, inRect.y, inRect.width, inRect.height / 2f), 渐变顶色);
            GUI.color = 渐变底色;
            Widgets.DrawRectFast(new Rect(inRect.x, inRect.y + inRect.height / 2f, inRect.width, inRect.height / 2f), 渐变底色);
            GUI.color = Color.white;

            // ===== 顶部标题栏 =====
            Rect 标题区域 = new Rect(inRect.x + 水平内边距, inRect.y + 标题栏Y偏移, inRect.width - 水平内边距 * 2f, 标题栏高);
            绘制标题栏(标题区域);

            // ===== 分类标签页（顶部水平） =====
            Rect 分类区域 = new Rect(inRect.x + 水平内边距, 标题区域.yMax + 区域间距, inRect.width - 水平内边距 * 2f, 分类标签栏高);
            绘制分类标签页(分类区域);

            // ===== 物品网格区域 =====
            float 网格X = inRect.x + 水平内边距;
            float 网格Y = 分类区域.yMax + 4f;
            float 当前购物车宽 = 显示购物车 ? 购物车宽 : 0f;
            float 网格宽 = inRect.width - 水平内边距 * 2f - 当前购物车宽 - (显示购物车 ? 购物车间距 : 0f);
            float 网格高 = inRect.yMax - 4f - 网格Y - 网格底部留白;

            Rect 网格区域 = new Rect(网格X, 网格Y, 网格宽, 网格高);
            绘制物品网格(网格区域);

            // ===== 购物车面板（右侧固定） =====
            if (显示购物车)
            {
                Rect 购物车区域 = new Rect(网格区域.xMax + 购物车间距, 网格Y, 当前购物车宽, 网格高);
                绘制购物车(购物车区域);
            }

            // ===== 底部按钮栏 =====
            Rect 底部区域 = new Rect(inRect.x + 水平内边距, inRect.yMax - 底部栏Y偏移, inRect.width - 水平内边距 * 2f, 底部栏高);
            绘制底部栏(底部区域);

            // ===== 筛选面板（浮层） =====
            if (显示筛选面板)
            {
                Rect 筛选Rect = new Rect(inRect.xMax - 320f, 分类区域.yMax + 5f, 300f, 400f);
                绘制筛选面板(筛选Rect);
            }

            // ===== 开发者模式 - 右键配置窗口（浮层） =====
            if (右键配置物品 != null && Prefs.DevMode)
            {
                Rect 配置Rect = new Rect(inRect.x + 100f, 分类区域.yMax + 20f, 400f, 500f);
                绘制配置窗口(配置Rect);
            }
        }

        // ================================================================
        //  公开接口 - 供 Dialog_SellVariant 等外部类调用
        // ================================================================

        /// <summary>公开的出售价格查询（供弹窗使用）</summary>
        public float 获取出售价格_公开(ThingDef def, QualityCategory? quality, ThingDef stuff)
        {
            return 获取出售价格(def, quality, stuff);
        }

        /// <summary>公开的购买价格查询</summary>
        public float 获取购买价格_公开(ThingDef def, QualityCategory? quality, ThingDef stuff)
        {
            return 获取购买价格(def, quality, stuff);
        }

        /// <summary>添加到交易数量（供弹窗累加）</summary>
        public void 添加到交易数量(TransactionKey key, int amount)
        {
            var dict = 是购买模式 ? 购买交易数量 : 出售交易数量;
            if (dict.ContainsKey(key))
                dict[key] += amount;
            else
                dict[key] = amount;
        }

        /// <summary>公开刷新物品列表</summary>
        public void 公开刷新()
        {
            刷新物品列表();
        }

        /// <summary>公开的主色调（供外部Window使用）</summary>
        public static Color 主色调_公开 => 主色调;

        /// <summary>检查物品是否有品质或材料的变体（需要弹窗选择）</summary>
        public bool 物品有变体(ThingDef def)
        {
            return def.HasComp(typeof(CompQuality)) || def.MadeFromStuff;
        }

        /// <summary>获取指定ThingDef的库存物品列表（用于弹窗的数据源，使用帧级缓存）</summary>
        public List<Thing> 获取库存物品(ThingDef def, Map map)
        {
            if (map == null) return new List<Thing>();
            var 映射 = 获取库存映射(map);
            if (映射.TryGetValue(def, out List<Thing> list))
                return list.ToList();
            return new List<Thing>();
        }

        /// <summary>帧级库存映射缓存 - 同一帧内共享所有出售模式的ThingDef查询</summary>
        private Dictionary<ThingDef, List<Thing>> 获取库存映射(Map map)
        {
            if (map == null) return new Dictionary<ThingDef, List<Thing>>();

            int 当前帧 = Time.frameCount;
            if (库存映射帧 == 当前帧)
                return 库存映射;

            库存映射.Clear();
            库存映射帧 = 当前帧;

            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing t = allThings[i];
                if (t.Faction == Faction.OfPlayer || (t.Faction == null && t.IsInAnyStorage()))
                {
                    ThingDef tDef = t.def;
                    if (!库存映射.TryGetValue(tDef, out List<Thing> list))
                    {
                        list = new List<Thing>();
                        库存映射[tDef] = list;
                    }
                    list.Add(t);
                }
            }
            return 库存映射;
        }
    }
}
