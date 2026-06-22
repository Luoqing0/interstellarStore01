using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace 星际商店
{
    /// <summary>
    /// 抽卡结果展示弹窗
    /// AI 辅助生成
    /// </summary>
    public class Dialog_GachaResult : Window
    {
        private readonly StarStore_GachaDef 卡池;
        private readonly bool isTenPull;
        private readonly List<抽卡结果条目> results;

        private Vector2 scrollPosition = Vector2.zero;

        private static readonly Color 背景色 = new Color(0.06f, 0.06f, 0.12f);
        private static readonly Color 格子背景色 = new Color(0.10f, 0.10f, 0.20f);
        private static readonly Color 边框色 = new Color(0.18f, 0.16f, 0.28f);
        private static readonly Color 文字色 = new Color(0.90f, 0.92f, 0.95f);
        private static readonly Color 确认按钮色 = new Color(0.35f, 0.30f, 0.48f);
        private static readonly Color 确认按钮Hover色 = new Color(0.45f, 0.40f, 0.60f);

        public override Vector2 InitialSize => new Vector2(460f, isTenPull ? 520f : 320f);

        public Dialog_GachaResult(StarStore_GachaDef 卡池, List<Thing> things, bool isTenPull)
        {
            this.卡池 = 卡池;
            this.isTenPull = isTenPull;
            this.results = new List<抽卡结果条目>();

            // 给结果排序：稀有度高的排前面
            var 稀有度列表 = 星际商店抽卡管理器.获取所有稀有度();
            foreach (Thing t in things)
            {
                if (t == null) continue;
                var rarity = 稀有度列表.FirstOrDefault(r =>
                    卡池.poolEntries.Any(e =>
                        e.rarityDefName == r.defName &&
                        e.thingDefs.Contains(t.def)));
                this.results.Add(new 抽卡结果条目 { thing = t, 稀有度 = rarity });
            }
            this.results = this.results
                .OrderBy(r => r.稀有度?.weight ?? float.MaxValue)
                .ThenBy(r => r.thing?.def?.label ?? "")
                .ToList();

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

            // 标题
            Text.Font = GameFont.Medium;
            GUI.color = 文字色;
            string title = isTenPull ? "StarStore_GachaTenResult".Translate(卡池.LabelCap) : "StarStore_GachaSingleResult".Translate(卡池.LabelCap);
            Widgets.Label(new Rect(0f, cy, inRect.width, 30f), title);
            cy += 30f;

            // 分隔线
            GUI.color = 边框色;
            Widgets.DrawLineHorizontal(0f, cy, inRect.width);
            cy += 8f;

            // 结果列表
            float listHeight = isTenPull ? 380f : 200f;
            Rect listRect = new Rect(0f, cy, inRect.width, listHeight);
            GUI.color = Color.white;
            Widgets.BeginScrollView(listRect, ref scrollPosition, new Rect(0f, 0f, listRect.width - 20f, results.Count * 58f + 10f));

            float itemY = 0f;
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                DrawResultRow(new Rect(4f, itemY, listRect.width - 28f, 54f), item, i + 1);
                itemY += 58f;
            }

            Widgets.EndScrollView();
            cy += listHeight + 8f;

            // 分隔线
            GUI.color = 边框色;
            Widgets.DrawLineHorizontal(0f, cy, inRect.width);
            cy += 12f;

            // 确认按钮
            Rect btnRect = new Rect(inRect.width / 2f - 60f, cy, 120f, 32f);
            GUI.color = btnRect.Contains(Event.current.mousePosition) ? 确认按钮Hover色 : 确认按钮色;
            if (Widgets.ButtonText(btnRect, "StarStore_GachaClose".Translate()))
            {
                Close();
            }
            GUI.color = Color.white;

            GUI.EndGroup();
        }

        private void DrawResultRow(Rect rect, 抽卡结果条目 item, int index)
        {
            Widgets.DrawRectFast(rect, 格子背景色);
            GUI.color = 边框色;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            // 序号
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.45f, 0.50f, 0.60f);
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 16f, 20f, 20f), $"#{index}");
            GUI.color = Color.white;

            // 物品图标
            Rect iconRect = new Rect(rect.x + 26f, rect.y + 4f, 46f, 46f);
            if (item.thing?.def?.uiIcon != null)
                GUI.DrawTexture(iconRect, item.thing.def.uiIcon, ScaleMode.ScaleToFit);

            // 稀有度边框着色
            if (item.稀有度 != null)
            {
                GUI.color = item.稀有度.rarityColor;
                Widgets.DrawBox(iconRect, 2);
                GUI.color = Color.white;
            }

            // 名称
            Text.Font = GameFont.Small;
            GUI.color = item.稀有度?.rarityColor ?? 文字色;
            string label = item.thing?.LabelCap ?? "???";
            if (item.稀有度 != null && !string.IsNullOrEmpty(item.稀有度.badgeText))
                label = $"[{item.稀有度.badgeText}] {label}";
            Widgets.Label(new Rect(iconRect.xMax + 8f, rect.y + 8f, rect.width - iconRect.xMax - 40f, 22f), label);
            GUI.color = Color.white;

            // 稀有度名
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.55f, 0.58f, 0.65f);
            if (item.稀有度 != null)
                Widgets.Label(new Rect(iconRect.xMax + 8f, rect.y + 30f, rect.width - iconRect.xMax - 40f, 18f), item.稀有度.LabelCap);
            GUI.color = Color.white;

            // 市场价
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1.0f, 0.75f, 0.2f);
            float mv = item.thing?.def?.BaseMarketValue ?? 0f;
            Widgets.Label(new Rect(rect.xMax - 80f, rect.y + 16f, 76f, 20f), $"{mv:F0}¥");
            GUI.color = Color.white;
        }

        private class 抽卡结果条目
        {
            public Thing thing;
            public StarStore_GachaRarityDef 稀有度;
        }
    }
}