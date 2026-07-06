using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 星际商店 - 抽卡页面绘制
    /// AI 辅助生成
    /// </summary>
    public partial class MainTabWindow_星际商店
    {
        private Vector2 抽卡滚动位置 = Vector2.zero;

        /// <summary>
        /// 绘制抽卡网格
        /// </summary>
        private void 绘制抽卡网格(Rect rect)
        {
            var 卡池列表 = 星际商店抽卡管理器.获取所有卡池();
            if (卡池列表.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                GUI.color = new Color(0.55f, 0.58f, 0.65f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "StarStore_GachaNoPools".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            // 卡片布局：每行一张，上下排列（卡片宽度包含稀有度标签高度）
            float cardHeight = 160f;
            float gap = 10f;
            float totalHeight = 卡池列表.Count * (cardHeight + gap) + 10f;

            Widgets.BeginScrollView(rect, ref 抽卡滚动位置, new Rect(0f, 0f, rect.width - 20f, totalHeight));

            float cy = 10f;
            foreach (var pool in 卡池列表)
            {
                Rect cardRect = new Rect(6f, cy, rect.width - 32f, cardHeight);
                绘制卡池卡片(cardRect, pool);
                cy += cardHeight + gap;
            }

            Widgets.EndScrollView();
        }

        /// <summary>
        /// 绘制单个卡池卡片
        /// </summary>
        private void 绘制卡池卡片(Rect rect, StarStore_GachaDef 卡池)
        {
            // 背景
            Widgets.DrawRectFast(rect, new Color(0.08f, 0.08f, 0.16f));
            GUI.color = new Color(0.18f, 0.16f, 0.28f);
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            float x = rect.x + 10f;
            float y = rect.y + 8f;
            float w = rect.width - 20f;

            // 卡池标题
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.85f, 0.86f, 0.92f);
            Widgets.Label(new Rect(x, y, w, 26f), 卡池.LabelCap);
            y += 28f;

            // 描述（自动换行）
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.55f, 0.58f, 0.65f);
            string desc = 卡池.description?.Trim() ?? "";
            float descHeight = Text.CalcHeight(desc, w);
            if (descHeight > 18f) descHeight = 32f; // 最多两行
            Widgets.Label(new Rect(x, y, w, descHeight), desc);
            y += descHeight + 4f;

            // 价格
            Text.Font = GameFont.Small;
            GUI.color = new Color(1.0f, 0.75f, 0.2f);
            string priceLabel = string.Format("{0}: {1}¥  |  {2}: {3}¥ ({4})",
                "StarStore_GachaSinglePull".Translate(),
                卡池.singlePullCost,
                "StarStore_GachaTenPull".Translate(),
                卡池.tenPullCost,
                卡池.tenPullDiscountLabel);
            Widgets.Label(new Rect(x, y, w, 22f), priceLabel);
            y += 26f;

            // 物品池预览（按稀有度显示颜色标签）
            Text.Font = GameFont.Tiny;
            float labelX = x;
            foreach (var entry in 卡池.poolEntries)
            {
                var rarity = 星际商店抽卡管理器.获取稀有度(entry.rarityDefName);
                if (rarity == null) continue;

                GUI.color = rarity.rarityColor;
                string poolLabel = $"[{rarity.badgeText}] {entry.thingDefs.Count}种";
                float labelW = Text.CalcSize(poolLabel).x + 8f;
                Widgets.Label(new Rect(labelX, y, labelW, 20f), poolLabel);
                labelX += labelW + 6f;

                if (labelX > rect.xMax - 80f) break;
            }
            GUI.color = Color.white;

            // 按钮：单抽 / 十连
            float btnY = rect.y + rect.height - 34f;
            Rect singleBtn = new Rect(x, btnY, 90f, 26f);
            Rect tenBtn = new Rect(x + 98f, btnY, 90f, 26f);
            Rect infoBtn = new Rect(rect.xMax - 80f, btnY, 70f, 26f);

            // 单抽按钮（暗紫底白字）
            Widgets.DrawRectFast(singleBtn, new Color(0.35f, 0.30f, 0.48f));
            GUI.color = Color.white;
            if (Widgets.ButtonText(singleBtn, "StarStore_GachaSingleBtn".Translate()))
            {
                执行抽卡(卡池, false);
            }
            GUI.color = Color.white;

            // 十连按钮（紫底白字）
            Widgets.DrawRectFast(tenBtn, new Color(0.45f, 0.30f, 0.60f));
            GUI.color = Color.white;
            if (Widgets.ButtonText(tenBtn, "StarStore_GachaTenBtn".Translate()))
            {
                执行抽卡(卡池, true);
            }
            GUI.color = Color.white;

            // 概率公示按钮（蓝灰底白字）
            Widgets.DrawRectFast(infoBtn, new Color(0.35f, 0.40f, 0.55f));
            GUI.color = Color.white;
            if (Widgets.ButtonText(infoBtn, "StarStore_GachaInfo".Translate()))
            {
                Find.WindowStack.Add(new Dialog_GachaInfo(卡池));
            }
            GUI.color = Color.white;
        }

        /// <summary>
        /// 执行抽卡：扣银币 -> 抽 -> 弹窗 + 消息跳转
        /// </summary>
        private void 执行抽卡(StarStore_GachaDef 卡池, bool isTenPull)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            int cost = isTenPull ? 卡池.tenPullCost : 卡池.singlePullCost;
            int 银币 = 获取白银总量(map);
            if (银币 < cost)
            {
                Messages.Message("StarStore_InsufficientSilver".Translate(cost, 银币), MessageTypeDefOf.RejectInput);
                return;
            }

            // 两阶段白银扣减（修复吞白银 Bug，详见 扣除白银 方法注释）
            if (!扣除白银(map, cost))
            {
                Messages.Message("StarStore_InsufficientSilver".Translate(cost, 获取白银总量(map)), MessageTypeDefOf.RejectInput);
                return;
            }

            // 执行抽取
            List<Thing> things;
            if (isTenPull)
                things = 星际商店抽卡管理器.执行十连(卡池, map);
            else
            {
                var t = 星际商店抽卡管理器.执行单抽(卡池, map);
                things = t != null ? new List<Thing> { t } : new List<Thing>();
            }

            // 投放物品到地图
            var dropSpot = 获取有效降落点(map);
            LookTargets lookTarget = default(LookTargets);
            if (dropSpot.IsValid && dropSpot.InBounds(map) && !dropSpot.Roofed(map)
                && !map.terrainGrid.TerrainAt(dropSpot).IsWater)
            {
                DropPodUtility.DropThingsNear(dropSpot, map, things, 110,
                    canInstaDropDuringInit: false, leaveSlag: false,
                    canRoofPunch: true, forbid: false, allowFogged: false);
                lookTarget = new LookTargets(dropSpot, map);
            }

            // 弹窗展示结果
            Find.WindowStack.Add(new Dialog_GachaResult(卡池, things, isTenPull));

            // AI 辅助生成：抽卡消息带 LookTargets，点击可跳转
            string msgKey = isTenPull ? "StarStore_GachaTenComplete" : "StarStore_GachaSingleComplete";
            Messages.Message(msgKey.Translate(卡池.LabelCap), lookTarget, MessageTypeDefOf.TaskCompletion);
        }
    }

    /// <summary>
    /// 卡池概率公示弹窗 — 显示各稀有度概率 + 具体物品列表 + i 详情按钮
    /// AI 辅助生成
    /// </summary>
    public class Dialog_GachaInfo : Window
    {
        private readonly StarStore_GachaDef 卡池;
        private Vector2 scrollPos = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(500f, 450f);

        public Dialog_GachaInfo(StarStore_GachaDef 卡池)
        {
            this.卡池 = 卡池;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            float cy = 0f;

            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.85f, 0.86f, 0.92f);
            Widgets.Label(new Rect(0f, cy, inRect.width, 28f), "StarStore_GachaProbability".Translate(卡池.LabelCap));
            cy += 32f;

            var 稀有度列表 = 星际商店抽卡管理器.获取所有稀有度();
            float 总权重 = 稀有度列表.Sum(r => r.weight);

            // 计算内容总高度
            float infoHeight = 0f;
            foreach (var rarity in 稀有度列表)
            {
                infoHeight += 28f; // 稀有度标题行
                var entry = 卡池.poolEntries.FirstOrDefault(e => e.rarityDefName == rarity.defName);
                int itemCount = entry?.thingDefs?.Count ?? 0;
                infoHeight += itemCount * 26f + 8f;
            }
            infoHeight += 20f;

            Rect scrollRect = new Rect(0f, cy, inRect.width, inRect.height - cy - 50f);
            Widgets.BeginScrollView(scrollRect, ref scrollPos, new Rect(0f, 0f, scrollRect.width - 20f, infoHeight));

            float itemY = 0f;
            foreach (var rarity in 稀有度列表)
            {
                float prob = 总权重 > 0f ? rarity.weight / 总权重 * 100f : 0f;
                var entry = 卡池.poolEntries.FirstOrDefault(e => e.rarityDefName == rarity.defName);
                int count = entry?.thingDefs?.Count ?? 0;

                // 稀有度标题行
                GUI.color = rarity.rarityColor;
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(4f, itemY, 80f, 22f), $"[{rarity.badgeText}]");
                GUI.color = new Color(0.90f, 0.92f, 0.95f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(84f, itemY, 120f, 18f), $"{rarity.LabelCap}  {prob:F1}%");
                GUI.color = new Color(0.55f, 0.58f, 0.65f);
                Widgets.Label(new Rect(84f, itemY + 14f, 150f, 18f), "StarStore_GachaItemCount".Translate(count));
                GUI.color = Color.white;
                itemY += 28f;

                // 该稀有度下每个物品的详情行
                if (entry?.thingDefs != null)
                {
                    foreach (var def in entry.thingDefs)
                    {
                        if (def == null) continue;

                        Rect itemRow = new Rect(8f, itemY, scrollRect.width - 36f, 24f);

                        // 物品图标
                        float iconSize = 20f;
                        Rect iconRect = new Rect(itemRow.x, itemRow.y + 2f, iconSize, iconSize);
                        if (def.uiIcon != null)
                            GUI.DrawTexture(iconRect, def.uiIcon, ScaleMode.ScaleToFit);

                        // 物品名
                        Text.Font = GameFont.Tiny;
                        GUI.color = new Color(0.80f, 0.82f, 0.88f);
                        Widgets.Label(new Rect(iconRect.xMax + 6f, itemRow.y + 3f, itemRow.width - iconRect.xMax - 30f, 20f), def.LabelCap);
                        GUI.color = Color.white;

                        // 标准 RimWorld 信息按钮（蓝色 i 图标）
                        Widgets.InfoCardButton(itemRow.xMax - 22f, itemRow.y + 2f, def);

                        itemY += 26f;
                    }
                }

                itemY += 8f;
            }

            Widgets.EndScrollView();
            cy += scrollRect.height;

            // 关闭按钮
            Rect closeBtn = new Rect(inRect.width / 2f - 50f, inRect.height - 38f, 100f, 28f);
            Widgets.DrawRectFast(closeBtn, new Color(0.35f, 0.30f, 0.48f));
            GUI.color = Color.white;
            if (Widgets.ButtonText(closeBtn, "StarStore_GachaClose".Translate()))
                Close();
            GUI.color = Color.white;

            GUI.EndGroup();
        }
    }
}