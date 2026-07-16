using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
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
        public bool 仅储存区 = false;

        // ===== 滚动位置 =====
        private Vector2 网格滚动位置 = Vector2.zero;

        // ===== 数据 =====
        private List<ThingDef> 当前显示物品 = new List<ThingDef>();
        private Dictionary<TransactionKey, int> 购买交易数量 = new Dictionary<TransactionKey, int>();
        private Dictionary<TransactionKey, int> 出售交易数量 = new Dictionary<TransactionKey, int>();
        private Dictionary<TransactionKey, int> 当前交易数量 => 是购买模式 ? 购买交易数量 : 出售交易数量;
        // 输入框文本缓冲持久化：避免每帧重建 buffer 导致光标乱跳
        private Dictionary<TransactionKey, string> 数量输入缓冲 = new Dictionary<TransactionKey, string>();
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
        // AI 辅助生成：每页物品数随布局变化
        private int 每页物品数 => 行数 * 列数;

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
        private const float 间距 = 10f;
        private const float 左侧分类栏宽 = 140f;

        // 格子尺寸限制（像素）
        private const float 格子最小尺寸 = 80f;
        private const float 格子最大尺寸 = 200f;

        // ===== 三种预设布局 =====
        public enum 布局类型 { 大, 中, 小 }
        public static 布局类型 当前布局 = 布局类型.大;  // 默认大布局

        // ===== 排序方式 =====
        public enum 排序方式 { 名称, 价格, 科技 }
        private 排序方式 当前排序方式 = 排序方式.名称;
        private bool 排序正序 = true;

        // ===== 看板娘（每次打开商店随机） =====
        private StarStore_MascotDef 当前看板娘 = null;
        private string 当前问候语 = "";

        // ===== 当前分类快捷判断 =====
        private bool 当前显示捆绑包 => 当前分类标签 == "StarStore_Cat_Bundles";
        private bool 当前显示机械族 => 当前分类标签 == "StarStore_Cat_Mechanoids";
        private bool 当前显示抽卡 => 当前分类标签 == "StarStore_Cat_Gacha";

        // 布局相关参数（根据布局类型动态调整）
        // 大布局3x4：图标更大，品质材料半行，价格+白银图标，数量+单位
        // 中布局4x4，小布局6x6
        private float 当前图标尺寸比例 => 当前布局 == 布局类型.大 ? 0.42f : (当前布局 == 布局类型.中 ? 0.40f : 0.35f);
        private float 当前图标最大尺寸 => 当前布局 == 布局类型.大 ? 72f : (当前布局 == 布局类型.中 ? 40f : 32f);
        private float 当前数量控制高度 => 当前布局 == 布局类型.大 ? 32f : (当前布局 == 布局类型.中 ? 46f : 30f);
        private bool 显示完整数量控制 => 当前布局 == 布局类型.大 || 当前布局 == 布局类型.中;  // 大/中布局显示
        private bool 显示品质材料半行 => 当前布局 == 布局类型.大;  // 大布局品质材料并排占半行

        private void 应用布局(布局类型 布局)
        {
            当前布局 = 布局;
            switch (布局)
            {
                case 布局类型.大:
                    行数 = 3;
                    列数 = 4;
                    break;
                case 布局类型.中:
                    行数 = 4;
                    列数 = 4;
                    break;
                case 布局类型.小:
                    行数 = 6;
                    列数 = 6;
                    break;
            }
            行数字符串 = 行数.ToString();
            列数字符串 = 列数.ToString();
            缓存格子尺寸 = 0f;
            上次物品数量 = -1;
        }

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
        private const float 底部栏Y偏移 = 52f;
        private const float 底部栏高 = 48f;
        private const float 网格底部留白 = 64f;
        private const float 网格内边距 = 16f;
        private const float 卡片内边距 = 4f;
        private const float 图标最大尺寸 = 38f;
        private const float 图标尺寸比例 = 0.40f;
        private const float 看板宽 = 220f;  // 独立看板宽度（不占用网格空间）
        private const float 分类列宽 = 80f;   // 左侧分类列宽度
        private const float 分页栏高 = 26f;     // 分页控件高度
        private string 缓存问候文字 = "";     // 缓存问候避免每帧变化
        public ThingDef 当前折扣物品 = null;   // AI：看板/交易/物品网格共享的折扣物品
        private int 上次折扣天数 = -1;         // 检测跨天时刷新折扣物品

        // ===== 淘宝×科幻电商风格颜色 =====
        // 基底 - 深邃太空背景
        private static readonly Color 背景色 = new Color(0.06f, 0.06f, 0.12f);
        private static readonly Color 渐变顶色 = new Color(0.08f, 0.08f, 0.16f);
        private static readonly Color 渐变底色 = new Color(0.04f, 0.04f, 0.08f);

        // 卡片 - 毛玻璃深色卡片
        private static readonly Color 格子背景色 = new Color(0.10f, 0.10f, 0.20f);
        private static readonly Color 格子hover色 = new Color(0.15f, 0.12f, 0.25f);
        private static readonly Color 边框色 = new Color(0.18f, 0.16f, 0.28f);

        // 主色调 - 科幻蓝紫
        private static readonly Color 主色调 = new Color(0.4f, 0.5f, 0.9f);       // 科技蓝
        private static readonly Color 主色调暗 = new Color(0.25f, 0.28f, 0.45f);   // 暗蓝

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
        // AI 辅助生成：捆绑包放在收藏下方，方便查看
        private static List<string> 预定义分类 = new List<string>
        {
            "StarStore_All",
            "StarStore_Favorites",
            "StarStore_Cat_Bundles",
            "StarStore_Cat_Gacha",
            "StarStore_Cat_Food", "StarStore_Cat_Medicine", "StarStore_Cat_Weapons",
            "StarStore_Cat_Apparel", "StarStore_Cat_Animals", "StarStore_Cat_Mechanoids",
            "StarStore_Cat_RawMaterials", "StarStore_Cat_Manufactured", "StarStore_Cat_Buildings",
            "StarStore_Cat_Furniture", "StarStore_Cat_Electronics",
            "StarStore_Cat_Misc"
        };

        // ===== 窗口大小 =====
        public override Vector2 RequestedTabSize
        {
            get { return new Vector2(1350f, 880f); }  // AI：加高80px容纳大布局数量控件
        }

        // 窗口位置居中（考虑看板窗口宽度，整体居中避免看板被推出屏幕）
        // 修复：高分屏+UI缩放下，主窗口单独居中会导致看板窗口 x 为负，左侧被裁剪
        // 支持：拖拽窗口后记忆位置，下次打开恢复
        private Vector2 上次窗口位置 = Vector2.zero;  // 用于检测拖拽
        public override void PostOpen()
        {
            base.PostOpen();
            draggable = true;  // 允许玩家拖拽商店窗口

            // 恢复保存的窗口位置，否则整体居中
            商店设置 设置 = 星际商店Mod.设置;
            if (设置 != null && 设置.窗口X >= 0f && 设置.窗口Y >= 0f)
            {
                windowRect.x = 设置.窗口X;
                windowRect.y = 设置.窗口Y;
                // 保底：确保窗口不会完全超出屏幕
                if (windowRect.x + windowRect.width < 100f)
                    windowRect.x = UI.screenWidth - windowRect.width;
                if (windowRect.x > UI.screenWidth - 100f)
                    windowRect.x = 100f;
                if (windowRect.y < 0f) windowRect.y = 0f;
                if (windowRect.y > UI.screenHeight - 100f)
                    windowRect.y = UI.screenHeight - 100f;
            }
            else
            {
                // 主窗口+看板作为一个整体居中
                float 整体宽 = windowRect.width + 看板宽 + 4f;
                windowRect.x = (UI.screenWidth - 整体宽) / 2f;
                // 保底：确保看板不会超出屏幕左边
                if (windowRect.x < 看板宽 + 4f)
                    windowRect.x = 看板宽 + 4f;
                // 保底：确保主窗口不会超出屏幕右边
                if (windowRect.x + windowRect.width > UI.screenWidth)
                    windowRect.x = UI.screenWidth - windowRect.width;
                windowRect.y = (UI.screenHeight - windowRect.height) / 2f;
            }
            上次窗口位置 = new Vector2(windowRect.x, windowRect.y);

            // AI 辅助生成：看板独立窗口，紧贴主窗口左侧
            if (看板窗口 == null)
            {
                看板窗口 = new Window_看板(this);
                Find.WindowStack.Add(看板窗口);
            }
            更新看板窗口位置();
        }

        /// <summary>
        /// 同步看板窗口位置到主窗口左侧
        /// </summary>
        private void 更新看板窗口位置()
        {
            if (看板窗口 == null) return;
            // 看板窗口紧贴主窗口左侧，顶部对齐，高度略短（留出底部栏空间）
            float kbX = windowRect.x - 看板宽 - 4f;
            float kbY = windowRect.y + 标题栏Y偏移;
            float kbH = windowRect.height - 标题栏Y偏移 - 底部栏Y偏移 - 4f;
            看板窗口.更新位置(kbX, kbY, kbH);
        }

        public override void PreClose()
        {
            base.PreClose();
            // 清理输入缓冲避免内存泄漏（L6）
            数量输入缓冲.Clear();
            // 保存窗口位置到设置（拖拽后记忆位置）
            商店设置 设置 = 星际商店Mod.设置;
            if (设置 != null)
            {
                设置.窗口X = windowRect.x;
                设置.窗口Y = windowRect.y;
                设置.Write();
            }
            // 关闭看板窗口
            if (看板窗口 != null)
            {
                看板窗口.Close(false);
                看板窗口 = null;
            }
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
            // 强制大布局（忽略设置中的行/列数）
            应用布局(布局类型.大);
            // AI 辅助生成：确保 GameComponent 存在并清理失效收藏
            if (Current.Game != null)
            {
                if (Current.Game.GetComponent<星际商店GameComponent>() == null)
                {
                    Current.Game.components.Add(new 星际商店GameComponent(Current.Game));
                }
                Current.Game.GetComponent<星际商店GameComponent>()?.清理失效收藏();
            }
            刷新物品列表();
            刷新礼包列表();
            // AI 辅助生成：缓存问候文字（避免每帧抖动）
            StarStore_SidebarConfigDef cfg = 侧边栏管理器.配置;
            当前看板娘 = cfg?.随机看板娘();
            if (当前看板娘 != null && !当前看板娘.greetings.NullOrEmpty())
                当前问候语 = 当前看板娘.greetings[Rand.Range(0, 当前看板娘.greetings.Count)];
            else if (cfg != null && cfg.greetings != null && cfg.greetings.Count > 0)
                当前问候语 = cfg.greetings[Rand.Range(0, cfg.greetings.Count)];
            else
                当前问候语 = "";
            // 保留旧字段兼容：看板窗口仍可能读取 缓存问候文字
            缓存问候文字 = 当前问候语;
            // AI 辅助生成：初始化折扣物品
            上次折扣天数 = GenDate.DayOfYear(Find.TickManager.TicksAbs, 0f);
            当前折扣物品 = cfg?.获取今日折扣物品(上次折扣天数);
            上次折扣物品 = 当前折扣物品;
            记录折扣历史();
            // AI 辅助生成：商店开门提示音（防 CurrentMap 为 null）
            Map openMap = Find.CurrentMap;
            if (openMap != null)
                SoundDef.Named("UI_ButtonPrompt").PlayOneShot(new TargetInfo(UI.MouseCell(), openMap));
        }

        /// <summary>
        /// 开发者手动刷新折扣：统一数据源，避免看板与物品格不一致
        /// AI 辅助生成
        /// </summary>
        public void 手动刷新折扣()
        {
            StarStore_SidebarConfigDef cfg = 侧边栏管理器.配置;
            if (cfg == null) return;

            int seed = Rand.Range(0, 99999);
            ThingDef item = cfg.获取手动折扣物品(seed);
            if (item != null)
            {
                当前折扣物品 = item;
                上次折扣物品 = item;
                记录折扣历史();
                刷新物品列表();
                Messages.Message("StarStore_DevRefreshDiscount".Translate(), MessageTypeDefOf.TaskCompletion);
            }
            else
            {
                Messages.Message("StarStore_DevRefreshDiscountFailed".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        /// <summary>刷新有效礼包缓存（随机礼包内容会重新抽取）</summary>
        private void 刷新礼包列表()
        {
            捆绑包管理器.刷新缓存();
        }

        /// <summary>把当前折扣物品记录到历史去重列表</summary>
        private void 记录折扣历史()
        {
            if (当前折扣物品 == null) return;
            var comp = Current.Game?.GetComponent<星际商店GameComponent>();
            comp?.记录历史折扣(当前折扣物品.defName);
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 检测窗口拖拽：位置变化时同步看板窗口
            if (windowRect.x != 上次窗口位置.x || windowRect.y != 上次窗口位置.y)
            {
                上次窗口位置 = new Vector2(windowRect.x, windowRect.y);
                更新看板窗口位置();
            }

            // 检测跨天：刷新折扣物品、礼包并更新物品列表
            int 当前天数 = GenDate.DayOfYear(Find.TickManager.TicksAbs, 0f);
            if (当前天数 != 上次折扣天数)
            {
                上次折扣天数 = 当前天数;
                StarStore_SidebarConfigDef cfg = 侧边栏管理器.配置;
                当前折扣物品 = cfg?.获取今日折扣物品(当前天数);
                上次折扣物品 = 当前折扣物品;
                记录折扣历史();
                刷新物品列表();
                刷新礼包列表();
                Messages.Message("StarStore_DailyRefresh".Translate(), MessageTypeDefOf.TaskCompletion);
            }

            // 同步看板窗口位置（窗口可能被拖动）
            更新看板窗口位置();

            // 绘制渐变背景（顶部亮→底部暗）
            GUI.color = 渐变顶色;
            Widgets.DrawRectFast(new Rect(inRect.x, inRect.y, inRect.width, inRect.height / 2f), 渐变顶色);
            GUI.color = 渐变底色;
            Widgets.DrawRectFast(new Rect(inRect.x, inRect.y + inRect.height / 2f, inRect.width, inRect.height / 2f), 渐变底色);
            GUI.color = Color.white;

            // ===== 看板已移至独立窗口，商店区从 inRect 左侧开始 =====
            float 商店区X = inRect.x + 水平内边距;
            float 商店区宽 = inRect.width - 水平内边距 * 2f;

            // ===== 顶部标题栏 =====
            Rect 标题区域 = new Rect(商店区X, inRect.y + 标题栏Y偏移, 商店区宽, 标题栏高);
            绘制标题栏(标题区域);

            // ===== 布局区域计算 =====
            // AI 辅助生成：内容高减去分页栏空间，避免与底部栏重叠
            float 内容Y = 标题区域.yMax + 区域间距;
            float 内容高 = inRect.yMax - 4f - 内容Y - 底部栏Y偏移 - 分页栏高 - 区域间距 * 2;

            // 分类列、网格、购物车都在商店区内
            float 分类列X = 商店区X;
            float 网格X = 分类列X + 分类列宽 + 区域间距;
            float 当前购物车宽 = 显示购物车 ? 购物车宽 : 0f;
            float 网格宽 = 商店区宽 - 分类列宽 - 区域间距 - 当前购物车宽 - (显示购物车 ? 购物车间距 : 0f);

            // AI 辅助生成：预估算网格实际所需宽度，收紧右侧空白
            // 使用与绘制物品网格相同的格子尺寸公式
            float 预估可用宽 = 网格宽 - 网格内边距;
            float 预估可用高 = 内容高 - 网格内边距;
            float 预估格 = Mathf.Min(预估可用宽 / 列数, 预估可用高 / 行数);
            预估格 -= 间距;
            预估格 = Mathf.Clamp(预估格, 格子最小尺寸, 格子最大尺寸);
            float 预估行高 = 预估格 + 间距;
            if (当前布局 == 布局类型.大) 预估行高 += 20f;
            float 实际网格宽 = 列数 * 预估行高 + 网格内边距;
            网格宽 = Mathf.Min(网格宽, 实际网格宽);

            // 先绘制网格（底层），再绘制分类列（上层，z-order正确）
            Rect 网格区域 = new Rect(网格X, 内容Y, 网格宽, 内容高);
            绘制物品网格(网格区域);

            Rect 分类列区域 = new Rect(分类列X, 内容Y, 分类列宽, 内容高);
            绘制分类列(分类列区域);

            // ===== 分页控件（在网格下方）=====
            Rect 分页区域 = new Rect(网格X, 网格区域.yMax + 区域间距, 网格宽, 分页栏高);
            绘制分页控件(分页区域);

            // ===== 购物车面板（右侧固定） =====
            if (显示购物车)
            {
                Rect 购物车区域 = new Rect(网格区域.xMax + 购物车间距, 内容Y, 当前购物车宽, 内容高);
                绘制购物车(购物车区域);
            }

            // ===== 底部按钮栏（商店区内）=====
            Rect 底部区域 = new Rect(商店区X, inRect.yMax - 底部栏Y偏移, 商店区宽, 底部栏高);
            绘制底部栏(底部区域);

            // ===== 筛选面板（浮层） =====
            if (显示筛选面板)
            {
                Rect 筛选Rect = new Rect(inRect.xMax - 320f, 标题区域.yMax + 5f, 300f, 400f);
                绘制筛选面板(筛选Rect);
            }

            // ===== 开发者模式 - 右键配置窗口（浮层） =====
            if (右键配置物品 != null && Prefs.DevMode)
            {
                Rect 配置Rect = new Rect(inRect.x + 100f, 标题区域.yMax + 20f, 400f, 500f);
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
            // AI 辅助生成：Pawn（动物/机械族）不走变体弹窗，因为获取库存映射只收集 Item
            if (def.race != null) return false;
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
            int currentFrame = Time.frameCount;
            if (库存映射帧 != currentFrame)
            {
                库存映射.Clear();
                库存映射帧 = currentFrame;

                // 性能优化：用 category == Item 快速跳过建筑/植物/投射物等非物品 Thing
                // 原代码遍历 AllThings 并对每个 Thing 执行 LINQ .Any() 检查，大型存档下 TPS 降幅显著
                // AI 辅助生成
                List<Thing> 所有物品 = map.listerThings.AllThings;
                for (int i = 0; i < 所有物品.Count; i++)
                {
                    Thing thing = 所有物品[i];
                    if (thing == null || thing.def == null) continue;
                    // 第一道过滤：只处理 Item 类别（跳过 Building/Plant/Filth 等）
                    if (thing.def.category != ThingCategory.Item) continue;
                    // 排除尸体（Corpse 的 category 也是 Item）
                    if (thing.def.IsCorpse) continue;
                    // 只统计可交易物品：出售模式需包含 Sellable（如鱼/食物），TraderCanSell 只含 All/Buyable
                    // 修复：原代码用 TraderCanSell() 排除了 Sellable 物品，导致鱼/食物类有库存却提示"库存不足"
                    Tradeability 交易性 = thing.def.tradeability;
                    if (交易性 != Tradeability.All && 交易性 != Tradeability.Buyable && 交易性 != Tradeability.Sellable) continue;
                    // 排除碎片（Chunk）—— 用 for 循环替代 LINQ .Any() 避免闭包开销
                    if (thing.def.thingCategories != null)
                    {
                        bool 是碎片 = false;
                        for (int c = 0; c < thing.def.thingCategories.Count; c++)
                        {
                            if (thing.def.thingCategories[c].defName == "Chunks")
                            {
                                是碎片 = true;
                                break;
                            }
                        }
                        if (是碎片) continue;
                    }
                    // 仅储存区模式下，跳过不在储存区内的物品
                    if (仅储存区 && !thing.IsInAnyStorage()) continue;

                    if (!库存映射.TryGetValue(thing.def, out List<Thing> 列表))
                    {
                        列表 = new List<Thing>();
                        库存映射[thing.def] = 列表;
                    }
                    列表.Add(thing);
                }
            }
            return 库存映射;
        }

        // ================================================================
        //  分页控件（独立绘制，避免与物品网格重叠）
        // ================================================================
        private void 绘制分页控件(Rect rect)
        {
            int 总页数 = Mathf.Max(1, Mathf.CeilToInt((float)当前显示物品.Count / 每页物品数));
            当前页码 = Mathf.Clamp(当前页码, 1, 总页数);

            // 显示物品数量和格子尺寸信息
            GUI.color = new Color(0.5f, 0.5f, 0.7f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 2f, rect.y + 2f, 180f, 20f),
                "StarStore_CellInfo".Translate(当前显示物品.Count, (int)缓存格子尺寸));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            if (总页数 <= 1) return;

            float 按钮宽 = 24f;
            float 分页Y = rect.y + 2f;
            float 分页起始X = rect.x + 190f;

            // 首页按钮
            Rect 首页Rect = new Rect(分页起始X, 分页Y, 按钮宽, 20f);
            if (Widgets.ButtonText(首页Rect, "|<")) { 当前页码 = 1; }

            // 上一页按钮
            Rect 上一页Rect = new Rect(分页起始X + 26f, 分页Y, 按钮宽, 20f);
            if (Widgets.ButtonText(上一页Rect, "<")) { if (当前页码 > 1) 当前页码--; }

            // 页码显示
            GUI.color = 文字色;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(分页起始X + 56f, 分页Y + 2f, 70f, 18f),
                "StarStore_PageInfo".Translate(当前页码, 总页数));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 下一页按钮
            Rect 下一页Rect = new Rect(分页起始X + 126f, 分页Y, 按钮宽, 20f);
            if (Widgets.ButtonText(下一页Rect, ">")) { if (当前页码 < 总页数) 当前页码++; }

            // 末页按钮
            Rect 末页Rect = new Rect(分页起始X + 152f, 分页Y, 按钮宽, 20f);
            if (Widgets.ButtonText(末页Rect, ">|")) { 当前页码 = 总页数; }

            // 跳转输入框
            Rect 跳转Rect = new Rect(分页起始X + 186f, 分页Y, 40f, 20f);
            Widgets.TextFieldNumeric(跳转Rect, ref 当前页码, ref 跳转页码输入, 1, 总页数);
        }
    }
}