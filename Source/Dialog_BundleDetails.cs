using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 礼包详情 / 购买确认弹窗
    /// AI 辅助生成
    /// </summary>
    public class Dialog_BundleDetails : Window
    {
        private readonly StarStore_BundleDef bundle;
        private readonly bool isConfirmMode;
        private readonly Action onConfirm;

        private Vector2 scrollPosition = Vector2.zero;
        private readonly float 内容高度;

        // 科幻电商风格颜色（与主窗口保持一致）
        private static readonly Color 背景色 = new Color(0.06f, 0.06f, 0.12f);
        private static readonly Color 格子背景色 = new Color(0.10f, 0.10f, 0.20f);
        private static readonly Color 边框色 = new Color(0.18f, 0.16f, 0.28f);
        private static readonly Color 文字色 = new Color(0.90f, 0.92f, 0.95f);
        private static readonly Color 次要文字色 = new Color(0.55f, 0.58f, 0.65f);
        private static readonly Color 价格色 = new Color(1.0f, 0.75f, 0.2f);
        private static readonly Color 原价色 = new Color(0.5f, 0.45f, 0.4f);
        private static readonly Color 购买按钮色 = new Color(1.0f, 0.5f, 0.15f);
        private static readonly Color 取消按钮常亮色 = new Color(0.35f, 0.30f, 0.48f);
        private static readonly Color 取消按钮Hover色 = new Color(0.45f, 0.40f, 0.60f);
        private static readonly Color 信息按钮色 = new Color(0.45f, 0.55f, 0.75f);
        private static readonly Color 信息按钮Hover色 = new Color(0.60f, 0.70f, 0.95f);

        public Dialog_BundleDetails(StarStore_BundleDef bundle, bool isConfirmMode, Action onConfirm)
        {
            this.bundle = bundle;
            this.isConfirmMode = isConfirmMode;
            this.onConfirm = onConfirm;

            doCloseButton = false;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;

            // 预估内容高度
            int 固定条目数 = bundle.fixedItems?.Count ?? 0;
            int 随机池条目数 = 0;
            if (bundle.randomGroups != null)
                foreach (var g in bundle.randomGroups)
                    if (g?.thingDefPool != null)
                        随机池条目数 += g.thingDefPool.Count;
            内容高度 = 40f + 固定条目数 * 44f + (固定条目数 > 0 ? 24f : 0f)
                           + 随机池条目数 * 44f + (随机池条目数 > 0 ? 24f : 0f);
        }

        public override Vector2 InitialSize => new Vector2(460f, 520f);

        public override void DoWindowContents(Rect inRect)
        {
            GUI.color = 背景色;
            Widgets.DrawRectFast(inRect, 背景色);
            GUI.color = Color.white;

            float 内边距 = 12f;
            float y = inRect.y + 内边距;
            float 可用宽 = inRect.width - 内边距 * 2f;

            // ===== 顶部：图标 + 名称 + 描述 =====
            float iconSize = 64f;
            Rect 图标Rect = new Rect(inRect.x + 内边距, y, iconSize, iconSize);
            Texture2D icon = bundle.获取图标();
            if (icon != null)
                GUI.DrawTexture(图标Rect, icon, ScaleMode.ScaleToFit);

            GUI.color = 文字色;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(图标Rect.xMax + 10f, y, 可用宽 - iconSize - 10f, 28f), bundle.LabelCap);

            Text.Font = GameFont.Tiny;
            GUI.color = 次要文字色;
            string desc = bundle.description ?? "";
            float descH = Text.CalcHeight(desc, 可用宽 - iconSize - 10f);
            Widgets.Label(new Rect(图标Rect.xMax + 10f, y + 30f, 可用宽 - iconSize - 10f, Mathf.Min(descH, 40f)), desc);
            GUI.color = Color.white;

            y += Mathf.Max(iconSize, 36f + descH) + 10f;

            // ===== 中部：内容滚动区 =====
            float 内容区高 = inRect.yMax - y - 110f;
            Rect 视图Rect = new Rect(inRect.x + 内边距, y, 可用宽, 内容区高);
            Rect 内容Rect = new Rect(0f, 0f, 可用宽 - 16f, Mathf.Max(内容高度, 内容区高));
            Widgets.BeginScrollView(视图Rect, ref scrollPosition, 内容Rect);

            float cy = 0f;

            // 固定内容
            if (bundle.fixedItems != null && bundle.fixedItems.Count > 0)
            {
                Text.Font = GameFont.Small;
                GUI.color = 文字色;
                Widgets.Label(new Rect(0f, cy, 内容Rect.width, 22f), "StarStore_BundleFixedItems".Translate());
                cy += 24f;

                foreach (var e in bundle.fixedItems)
                {
                    if (e?.thingDef == null) continue;
                    Rect row = new Rect(4f, cy, 内容Rect.width - 8f, 40f);
                    Widgets.DrawRectFast(row, 格子背景色);
                    GUI.color = 边框色;
                    Widgets.DrawBox(row);
                    GUI.color = Color.white;

                    Rect itemIconRect = new Rect(row.x + 6f, row.y + 4f, 32f, 32f);
                    Texture2D tex = e.thingDef.uiIcon;
                    if (tex != null)
                        GUI.DrawTexture(itemIconRect, tex, ScaleMode.ScaleToFit);

                    string label = e.thingDef.LabelCap + " x" + e.count;
                    if (e.stuff != null)
                        label += " (" + e.stuff.LabelAsStuff + ")";
                    if (!e.randomQuality)
                        label += " [" + e.quality.GetLabel() + "]";

                    Text.Font = GameFont.Tiny;
                    GUI.color = 文字色;
                    Widgets.Label(new Rect(itemIconRect.xMax + 8f, row.y + 10f, row.width - itemIconRect.width - 36f, 20f), label);
                    GUI.color = Color.white;

                    // 标准 RimWorld i 详情按钮
                    Widgets.InfoCardButton(row.xMax - 24f, row.y + 10f, e.thingDef, e.stuff);

                    cy += 44f;
                }
                cy += 8f;
            }

            // 随机内容
            if (bundle.randomGroups != null && bundle.randomGroups.Count > 0)
            {
                Text.Font = GameFont.Small;
                GUI.color = 文字色;
                Widgets.Label(new Rect(0f, cy, 内容Rect.width, 22f), "StarStore_BundleRandomItems".Translate());
                cy += 24f;

                foreach (var g in bundle.randomGroups)
                {
                    if (g?.thingDefPool == null || g.thingDefPool.Count == 0) continue;

                    // 组头：显示组数量和抽取范围
                    string countInfo = "StarStore_BundleRandomGroup".Translate(g.count, g.itemCountRange.min, g.itemCountRange.max);
                    Text.Font = GameFont.Tiny;
                    GUI.color = 次要文字色;
                    Widgets.Label(new Rect(4f, cy, 内容Rect.width - 8f, 18f), countInfo);
                    cy += 20f;

                    // AI 辅助生成：展开随机池中每个物品为独立行，供查看详情
                    foreach (ThingDef poolDef in g.thingDefPool)
                    {
                        if (poolDef == null) continue;
                        Rect row = new Rect(4f, cy, 内容Rect.width - 8f, 40f);
                        Widgets.DrawRectFast(row, 格子背景色);
                        GUI.color = 边框色;
                        Widgets.DrawBox(row);
                        GUI.color = Color.white;

                        Rect itemIconRect = new Rect(row.x + 6f, row.y + 4f, 32f, 32f);
                        Texture2D tex = poolDef.uiIcon;
                        if (tex != null)
                            GUI.DrawTexture(itemIconRect, tex, ScaleMode.ScaleToFit);

                        Text.Font = GameFont.Tiny;
                        GUI.color = 文字色;
                        Widgets.Label(new Rect(itemIconRect.xMax + 8f, row.y + 10f, row.width - itemIconRect.width - 36f, 20f), poolDef.LabelCap);
                        GUI.color = Color.white;

                        // 标准 RimWorld i 详情按钮
                        Widgets.InfoCardButton(row.xMax - 24f, row.y + 10f, poolDef);

                        cy += 44f;
                    }
                }
                cy += 8f;
            }

            Widgets.EndScrollView();

            y += 内容区高 + 12f;

            // ===== 价格区 =====
            float 原价 = 捆绑包管理器.获取原价(bundle);
            float 现价 = 捆绑包管理器.获取折扣价(bundle);
            float 折扣率 = bundle.获取折扣率();

            Rect 价格区域 = new Rect(inRect.x + 内边距, y, 可用宽, 40f);
            Widgets.DrawRectFast(价格区域, 格子背景色);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = 原价色;
            Widgets.Label(new Rect(价格区域.x, 价格区域.y, 可用宽 / 3f, 40f), "StarStore_BundleOriginalPrice".Translate(原价.ToString("F0")));

            GUI.color = 次要文字色;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(价格区域.x + 可用宽 / 3f, 价格区域.y, 可用宽 / 3f, 40f), "StarStore_DiscountRate".Translate((折扣率 * 10f).ToString("F1")));

            GUI.color = 价格色;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(价格区域.x + 可用宽 * 2f / 3f, 价格区域.y, 可用宽 / 3f, 40f), "StarStore_BundleDiscountPrice".Translate(现价.ToString("F0")));

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            y += 50f;

            // ===== 按钮区 =====
            float 按钮宽 = 100f;
            float 按钮高 = 32f;
            float 按钮间距 = 12f;
            float 总按钮宽 = isConfirmMode ? 按钮宽 * 2f + 按钮间距 : 按钮宽;
            float 起始X = inRect.x + (inRect.width - 总按钮宽) / 2f;

            if (isConfirmMode)
            {
                // AI 辅助生成：取消按钮常亮
                Rect 取消Rect = new Rect(起始X, y, 按钮宽, 按钮高);
                bool cancelHover = Mouse.IsOver(取消Rect);
                GUI.color = cancelHover ? 取消按钮Hover色 : 取消按钮常亮色;
                Widgets.DrawRectFast(取消Rect, GUI.color);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(取消Rect, "StarStore_BundleCancel".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(取消Rect)) Close();

                // 确认购买按钮
                Rect 确认Rect = new Rect(起始X + 按钮宽 + 按钮间距, y, 按钮宽, 按钮高);
                GUI.color = 购买按钮色;
                if (Widgets.ButtonText(确认Rect, "StarStore_BundleBuyConfirm".Translate()))
                {
                    onConfirm?.Invoke();
                    Close();
                }
                GUI.color = Color.white;
            }
            else
            {
                // AI 辅助生成：关闭/取消按钮常亮
                Rect 关闭Rect = new Rect(起始X, y, 按钮宽, 按钮高);
                bool closeHover = Mouse.IsOver(关闭Rect);
                GUI.color = closeHover ? 取消按钮Hover色 : 取消按钮常亮色;
                Widgets.DrawRectFast(关闭Rect, GUI.color);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(关闭Rect, "StarStore_BundleCancel".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(关闭Rect)) Close();
            }
        }
    }
}
