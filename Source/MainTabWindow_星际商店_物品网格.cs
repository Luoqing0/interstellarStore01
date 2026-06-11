using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace 星际商店
{
    public partial class MainTabWindow_星际商店
    {
        // ================================================================
        //  物品网格 - 虚拟滚动优化，只渲染可见区域的格子
        // ================================================================
        // 缓存上一次的格子尺寸和列数，避免频繁重算
        private float 缓存格子尺寸 = 0f;
        private int 缓存列数 = 0;
        private int 上次物品数量 = -1;
        private float 上次可用宽 = 0f;
        private float 上次可用高 = 0f;

        private const int 最大名称字符数 = 14;

        private void 绘制物品网格(Rect rect)
        {
            // 分页计算
            int 总页数 = Mathf.Max(1, Mathf.CeilToInt((float)当前显示物品.Count / 每页物品数));
            当前页码 = Mathf.Clamp(当前页码, 1, 总页数);

            int 页起始索引 = (当前页码 - 1) * 每页物品数;
            int 页结束索引 = Mathf.Min(当前显示物品.Count - 1, 页起始索引 + 每页物品数 - 1);
            List<ThingDef> 当前页物品 = new List<ThingDef>();
            for (int i = 页起始索引; i <= 页结束索引; i++)
                当前页物品.Add(当前显示物品[i]);

            if (当前页物品.Count == 0)
            {
                GUI.color = 文字色;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "StarStore_NoItems".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                缓存格子尺寸 = 0f;
                return;
            }

            float 可用宽 = rect.width - 网格内边距;
            float 可用高 = rect.height - 网格内边距;

            // 物品数量或窗口尺寸变化时重新计算格子尺寸和列数
            if (当前页物品.Count != 上次物品数量 ||
                缓存格子尺寸 <= 0f ||
                Mathf.Abs(可用宽 - 上次可用宽) > 1f ||
                Mathf.Abs(可用高 - 上次可用高) > 1f)
            {
                上次物品数量 = 当前页物品.Count;
                上次可用宽 = 可用宽;
                上次可用高 = 可用高;
                float 格子尺寸 = Mathf.Min(可用宽 / 列数, 可用高 / 行数);
                格子尺寸 -= 间距;
                格子尺寸 = Mathf.Clamp(格子尺寸, 格子最小尺寸, 格子最大尺寸);
                缓存格子尺寸 = 格子尺寸;
                缓存列数 = Mathf.Max(1, Mathf.FloorToInt((可用宽 + 间距) / (格子尺寸 + 间距)));
            }

            float 格 = 缓存格子尺寸;
            int 实际列数 = 缓存列数;
            float 行高 = 格 + 间距;

            // 总内容区域（虚拟高度，用于正确显示滚动条）
            int 总行数 = Mathf.CeilToInt((float)当前页物品.Count / 实际列数);
            float 内容宽 = 实际列数 * 行高;
            float 内容高 = 总行数 * 行高;

            Rect 视图区域 = new Rect(0f, 0f, 内容宽, 内容高);
            Widgets.BeginScrollView(rect, ref 网格滚动位置, 视图区域);

            // 虚拟滚动：只渲染可见行（±1 行缓冲）
            int 起始行 = Mathf.Max(0, Mathf.FloorToInt(网格滚动位置.y / 行高) - 1);
            int 可见行数 = Mathf.CeilToInt(rect.height / 行高) + 2;
            int 结束行 = Mathf.Min(总行数 - 1, 起始行 + 可见行数);

            int 起始索引 = 起始行 * 实际列数;
            int 结束索引 = Mathf.Min(当前页物品.Count - 1, (结束行 + 1) * 实际列数 - 1);

            for (int i = 起始索引; i <= 结束索引; i++)
            {
                ThingDef def = 当前页物品[i];
                int 行 = i / 实际列数;
                int 列 = i % 实际列数;
                float x = 列 * 行高;
                float y = 行 * 行高;
                Rect 格子 = new Rect(x, y, 格, 格);
                绘制单个物品格子(格子, def, 格);
            }

            Widgets.EndScrollView();

            // 分页控件和物品信息已移至独立方法 绘制分页控件()
        }

        // ================================================================
        //  单个物品格子（卡片式设计 - 轻科幻浅蓝渐变）
        // ================================================================
        private void 绘制单个物品格子(Rect rect, ThingDef def, float 格子尺寸)
        {
            // 检查交易条件
            bool 条件满足 = 交易条件管理器.是否可以交易(def.defName, Find.CurrentMap);
            bool 已收藏 = 收藏列表.Contains(def.defName);
            bool 已隐藏 = 交易条件管理器.条件Def.是否隐藏(def.defName);
            if (已隐藏 && !Prefs.DevMode) return;

            // 卡片背景（hover 高亮）
            bool hover = Mouse.IsOver(rect);

            // 微阴影
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            Widgets.DrawBox(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height));
            GUI.color = Color.white;

            // 轻科幻浅蓝透明渐变背景（多条细带模拟真渐变）
            Color top = new Color(0.06f, 0.10f, 0.22f, 0.85f);
            Color bottom = new Color(0.03f, 0.05f, 0.12f, 0.85f);
            int strips = 8;
            for (int s = 0; s < strips; s++)
            {
                float t = (float)s / (strips - 1);
                Color c = Color.Lerp(top, bottom, t);
                if (hover && 条件满足) c = Color.Lerp(c, new Color(0.15f, 0.20f, 0.35f, 0.9f), 0.5f);
                Rect strip = new Rect(rect.x, rect.y + (rect.height * s / strips), rect.width, rect.height / strips + 1f);
                Widgets.DrawRectFast(strip, c);
            }

            // 边框
            Color 边框 = hover && 条件满足 ? new Color(0.45f, 0.55f, 0.85f, 0.8f) : new Color(0.18f, 0.22f, 0.35f, 0.6f);
            GUI.color = 边框;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            float 内边距 = 卡片内边距;
            float 可用宽 = rect.width - 内边距 * 2;

            // 收藏星（左上角）- 悬停高亮 + 点击音效
            Rect 收藏Rect = new Rect(rect.x + 2f, rect.y + 2f, 18f, 18f);
            bool 收藏hover = Mouse.IsOver(收藏Rect);
            GUI.color = 已收藏 ? new Color(1f, 0.75f, 0f) : (收藏hover ? new Color(0.6f, 0.6f, 0.7f) : new Color(0.3f, 0.3f, 0.35f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(收藏Rect, "★");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(收藏Rect))
            {
                // 点击音效（与研究页面解锁一致）
                SoundDefOf.Tick_Tiny.PlayOneShot(new TargetInfo(UI.MouseCell(), Find.CurrentMap));
                if (已收藏) 收藏列表.Remove(def.defName);
                else 收藏列表.Add(def.defName);
                刷新物品列表();
            }

            // 信息按钮（右上角）- 悬停高亮 + 点击音效
            Rect 信息按钮Rect = new Rect(rect.xMax - 20f, rect.y + 2f, 18f, 18f);
            bool 信息hover = Mouse.IsOver(信息按钮Rect);
            GUI.color = 信息hover ? 主色调 : new Color(0.4f, 0.5f, 0.7f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(信息按钮Rect, "i");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(信息按钮Rect))
            {
                SoundDefOf.Tick_Tiny.PlayOneShot(new TargetInfo(UI.MouseCell(), Find.CurrentMap));
                Find.WindowStack.Add(new Dialog_InfoCard(def));
            }

            // 物品图标（居中偏上）- 根据布局类型动态调整尺寸，大布局图标更大
            float 图标尺寸 = Mathf.Min(格子尺寸 * 当前图标尺寸比例, 当前图标最大尺寸);
            float 图标X = rect.x + (rect.width - 图标尺寸) / 2f;
            float 图标Y = rect.y + 内边距 + 20f;  // 为收藏和信息按钮留空间
            Rect 图标Rect = new Rect(图标X, 图标Y, 图标尺寸, 图标尺寸);

            // 图标背景光晕
            if (hover && 条件满足)
            {
                GUI.color = new Color(主色调.r, 主色调.g, 主色调.b, 0.12f);
                Widgets.DrawRectFast(图标Rect.ExpandedBy(4f), new Color(主色调.r, 主色调.g, 主色调.b, 0.12f));
                GUI.color = Color.white;
            }
            Widgets.ThingIcon(图标Rect, def);

            // 物品名称 - 增加高度避免截断
            float 名称Y = 图标Rect.yMax + 2f;
            GUI.color = 条件满足 ? 文字色 : new Color(0.6f, 0.3f, 0.3f);
            Text.Font = GameFont.Tiny;
            string 显示名 = def.LabelCap;
            if (Text.CalcHeight(显示名, 可用宽) > 22f)
            {
                显示名 = def.label;
                if (Text.CalcHeight(显示名, 可用宽) > 22f)
                    显示名 = def.label.Truncate(最大名称字符数);
            }
            Rect 名称Rect = new Rect(rect.x + 内边距, 名称Y, 可用宽, 22f);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(名称Rect, 显示名);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 大布局：品质和材料并排占半行
            if (显示品质材料半行 && 是购买模式)
            {
                bool 有品质 = def.HasComp(typeof(CompQuality));
                bool 有材料 = def.MadeFromStuff;
                
                if (有品质 || 有材料)
                {
                    float 半行Y = 名称Rect.yMax + 1f;
                    float 半行宽 = 可用宽 / 2f - 2f;
                    
                    // 品质选择（左半边）- 显示"品质:xx"
                    if (有品质)
                    {
                        Rect qRect = new Rect(rect.x + 内边距, 半行Y, 半行宽, 18f);
                        QualityCategory? curQ = 购买品质选择.TryGetValue(def, out var q) ? q : (QualityCategory?)null;
                        string qLabel = curQ.HasValue ? curQ.Value.GetLabel() : "一般";
                        // 截断品质文字防止超出按钮
                        if (qLabel.Length > 4) qLabel = qLabel.Substring(0, 4);
                        qLabel = "品质:" + qLabel;
                        if (Widgets.ButtonText(qRect, qLabel))
                        {
                            List<FloatMenuOption> opts = new List<FloatMenuOption>();
                            foreach (QualityCategory qc in Enum.GetValues(typeof(QualityCategory)))
                            {
                                QualityCategory captured = qc;
                                opts.Add(new FloatMenuOption(qc.GetLabel(), () => {
                                    购买品质选择[def] = captured;
                                }));
                            }
                            Find.WindowStack.Add(new FloatMenu(opts));
                        }
                    }
                    
                    // 材料选择（右半边）- 减少截断
                    if (有材料)
                    {
                        float 材料X = rect.x + 内边距 + 半行宽 + 4f;
                        Rect sRect = new Rect(材料X, 半行Y, 半行宽, 18f);
                        ThingDef curS = 购买材料选择.TryGetValue(def, out var s) ? s : (def.defaultStuff ?? ThingDefOf.Steel);
                        string sLabel = curS.LabelAsStuff;
                        if (sLabel.Length > 5) sLabel = sLabel.Substring(0, 5);
                        if (Widgets.ButtonText(sRect, sLabel))
                        {
                            List<FloatMenuOption> opts = new List<FloatMenuOption>();
                            foreach (ThingDef stuffDef in GenStuff.AllowedStuffsFor(def))
                            {
                                ThingDef captured = stuffDef;
                                opts.Add(new FloatMenuOption(stuffDef.LabelAsStuff, () => {
                                    购买材料选择[def] = captured;
                                }));
                            }
                            Find.WindowStack.Add(new FloatMenu(opts));
                        }
                    }
                    
                    名称Rect = new Rect(名称Rect.x, 名称Rect.y, 名称Rect.width, 名称Rect.height + 20f);
                }
            }
            else
            {
                // 非大布局：品质和材料各占一行
                if (是购买模式 && def.HasComp(typeof(CompQuality)))
                {
                    float qY = 名称Rect.yMax + 1f;
                    Rect qRect = new Rect(rect.x + 内边距, qY, 可用宽, 18f);
                    QualityCategory? curQ = 购买品质选择.TryGetValue(def, out var q) ? q : (QualityCategory?)null;
                    string qLabel = curQ?.GetLabel() ?? "品质: 一般";
                    if (Widgets.ButtonText(qRect, qLabel))
                    {
                        List<FloatMenuOption> opts = new List<FloatMenuOption>();
                        foreach (QualityCategory qc in Enum.GetValues(typeof(QualityCategory)))
                        {
                            QualityCategory captured = qc;
                            opts.Add(new FloatMenuOption(qc.GetLabel(), () => {
                                购买品质选择[def] = captured;
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(opts));
                    }
                    名称Rect = new Rect(名称Rect.x, 名称Rect.y, 名称Rect.width, 名称Rect.height + 20f);
                }

                if (是购买模式 && def.MadeFromStuff)
                {
                    float sY = 名称Rect.yMax + 1f;
                    Rect sRect = new Rect(rect.x + 内边距, sY, 可用宽, 18f);
                    ThingDef curS = 购买材料选择.TryGetValue(def, out var s) ? s : (def.defaultStuff ?? ThingDefOf.Steel);
                    if (Widgets.ButtonText(sRect, curS.LabelAsStuff))
                    {
                        List<FloatMenuOption> opts = new List<FloatMenuOption>();
                        foreach (ThingDef stuffDef in GenStuff.AllowedStuffsFor(def))
                        {
                            ThingDef captured = stuffDef;
                            opts.Add(new FloatMenuOption(stuffDef.LabelAsStuff, () => {
                                购买材料选择[def] = captured;
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(opts));
                    }
                    名称Rect = new Rect(名称Rect.x, 名称Rect.y, 名称Rect.width, 名称Rect.height + 20f);
                }
            }

            // 价格和数量（并排分色）- 大布局不显示底部数量控制
            QualityCategory? 当前品质 = null;
            ThingDef 当前材料 = null;
            if (是购买模式)
            {
                购买品质选择.TryGetValue(def, out 当前品质);
                购买材料选择.TryGetValue(def, out 当前材料);
            }
            float 单价 = 是购买模式 ? 获取购买价格(def, 当前品质, 当前材料) : 获取出售价格(def);
            
            // 获取当前已选数量
            TransactionKey 价格key = new TransactionKey(def, 当前品质, 当前材料);
            int 已选数量 = 当前交易数量.TryGetValue(价格key, out var n) ? n : 0;
            
            if (当前布局 == 布局类型.大)
            {
                // 大布局：价格和数量并排显示，不同颜色
                float 价格Y = 名称Rect.yMax + 2f;
                float 价格区高 = 18f;
                float 价格宽 = 可用宽 * 0.55f;
                float 数量宽 = 可用宽 * 0.45f;

                // 价格（金色，左边）
                Rect 价格Rect = new Rect(rect.x + 内边距, 价格Y, 价格宽, 价格区高);
                GUI.color = 价格色;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(价格Rect, "⛃ " + 单价.ToString("F0"));

                // 数量（科技蓝，右边）
                Rect 数量Rect = new Rect(价格Rect.xMax + 4f, 价格Y, 数量宽 - 4f, 价格区高);
                GUI.color = 主色调;
                Text.Anchor = TextAnchor.MiddleRight;
                if (已选数量 > 0)
                    Widgets.Label(数量Rect, 已选数量 + "个");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // 出售模式：显示变体摘要
                float 库存Y = 价格Rect.yMax + 1f;
                if (!是购买模式)
                {
                    绘制出售变体摘要(rect, def, 库存Y, 可用宽, 内边距, ref 名称Y);
                }
            }
            else
            {
                // 其他布局：只显示价格
                float 价格Y = 名称Rect.yMax + 2f;
                GUI.color = 价格色;
                Text.Font = GameFont.Tiny;
                Rect 价格Rect = new Rect(rect.x + 内边距, 价格Y, 可用宽, 20f);
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(价格Rect, "⛃ " + 单价.ToString("F0"));
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // 出售模式：显示变体摘要
                float 库存Y = 价格Rect.yMax + 1f;
                if (!是购买模式)
                {
                    绘制出售变体摘要(rect, def, 库存Y, 可用宽, 内边距, ref 名称Y);
                }
            }

            // 条件不满足时显示原因
            if (!条件满足)
            {
                string 条件描述 = 交易条件管理器.获取条件描述(def.defName);
                if (!string.IsNullOrEmpty(条件描述))
                {
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    Text.Font = GameFont.Tiny;
                    float 锁定Y = 名称Rect.yMax + 名称Rect.height + 2f;
                    Rect 锁定Rect = new Rect(rect.x + 内边距, 锁定Y, 可用宽, 16f);
                    Text.Anchor = TextAnchor.UpperCenter;
                    Widgets.Label(锁定Rect, "StarStore_Locked".Translate() + " " + 条件描述.Truncate(14));
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;
                }
            }

            // 数量控制区域（格子底部）
            if (当前布局 == 布局类型.大)
            {
                // 大布局：紧凑数量控制（滑条+输入框+按钮）
                Rect 紧凑控制 = new Rect(rect.x + 内边距 + 2f, rect.yMax - 内边距 - 28f, 可用宽 - 4f, 24f);
                绘制数量控制(紧凑控制, 价格key);
            }
            else
            {
                float 控制高 = 当前数量控制高度;
                float 控制Y = rect.yMax - 内边距 - 控制高;
                if (控制Y > 名称Y + 10f && 条件满足 && !已隐藏)
                {
                    Rect 数量区域 = new Rect(rect.x + 内边距, 控制Y, 可用宽, 控制高);

                    if (!是购买模式 && 物品有变体(def))
                    {
                        Rect 选择变体Rect = new Rect(rect.x + 内边距, 控制Y + 6f, 可用宽, 24f);
                        if (Widgets.ButtonText(选择变体Rect, "StarStore_SelectVariant".Translate()))
                        {
                            Map map = Find.CurrentMap;
                            if (map != null)
                            {
                                List<Thing> inventory = 获取库存物品(def, map);
                                Find.WindowStack.Add(new Dialog_SellVariant(def, inventory));
                            }
                        }
                    }
                    else
                    {
                        QualityCategory? 控品质 = null;
                        ThingDef 控材料 = null;
                        if (是购买模式)
                        {
                            购买品质选择.TryGetValue(def, out 控品质);
                            购买材料选择.TryGetValue(def, out 控材料);
                        }
                        TransactionKey key = new TransactionKey(def, 控品质, 控材料);

                        if (显示完整数量控制)
                            绘制数量控制(数量区域, key);
                        else
                            绘制迷你数量控制(数量区域, key);
                    }
                }
                else if (条件满足 && !已隐藏)
                {
                    Rect 迷你控制区域 = new Rect(rect.x + 内边距, rect.yMax - 内边距 - 20f, 可用宽, 20f);
                    QualityCategory? 控品质 = null;
                    ThingDef 控材料 = null;
                    if (是购买模式)
                    {
                        购买品质选择.TryGetValue(def, out 控品质);
                        购买材料选择.TryGetValue(def, out 控材料);
                    }
                    TransactionKey key = new TransactionKey(def, 控品质, 控材料);
                    绘制迷你数量控制(迷你控制区域, key);
                }
            }

            // ===== 开发者模式：右键点击弹出配置菜单 =====
            if (Prefs.DevMode && Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(rect))
            {
                右键配置物品 = def;
                配置研究输入 = "";
                配置物品输入 = "";
                配置提示信息 = "";
                物品交易条件 现有条件 = 交易条件管理器.条件Def.获取条件(def.defName);
                if (现有条件 != null)
                {
                    配置研究输入 = 现有条件.requiredResearch ?? "";
                    配置物品输入 = 现有条件.requiredItem ?? "";
                }
                Event.current.Use();
            }
        }

        // 出售变体摘要（提取为独立方法）
        private void 绘制出售变体摘要(Rect rect, ThingDef def, float startY, float 可用宽, float 内边距, ref float 名称Y)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            var 库存映射数据 = 获取库存映射(map);
            List<Thing> things;
            if (!库存映射数据.TryGetValue(def, out things))
                things = new List<Thing>();

            var groups = things.GroupBy(t => new {
                Quality = t.TryGetQuality(out QualityCategory qc2) ? qc2 : QualityCategory.Normal,
                Stuff = t.Stuff
            });

            float variantY = startY;
            int shown = 0;
            foreach (var g in groups.Take(3))
            {
                if (shown >= 3) break;
                int count = g.Sum(t => t.stackCount);
                string vLabel = (g.Key.Quality != QualityCategory.Normal ? g.Key.Quality.GetLabel() : "") +
                                (g.Key.Stuff != null ? g.Key.Stuff.LabelAsStuff : "") +
                                "×" + count;
                Rect vRect = new Rect(rect.x + 内边距, variantY, 可用宽, 14f);
                GUI.color = new Color(0.55f, 0.55f, 0.75f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(vRect, vLabel);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                variantY += 14f;
                shown++;
            }
        }
    }
}
