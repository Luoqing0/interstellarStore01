using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 出售变体选择弹窗 - 按品质/材料区分出售物品
    /// </summary>
    public class Dialog_SellVariant : Verse.Window
    {
        private ThingDef thingDef;
        private List<VariantRow> rows = new List<VariantRow>();
        private Vector2 scrollPosition;
        private string 提示信息 = "";

        private const float 行高 = 30f;
        private const float 列标签高 = 20f;
        private const float 底部栏高 = 52f;

        private class VariantRow
        {
            public TransactionKey key;
            public int stock;
            public float unitPrice;
            public int quantity;
            public string label;
        }

        public Dialog_SellVariant(ThingDef def, List<Thing> inventory)
        {
            thingDef = def;
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            closeOnClickedOutside = false;
            forcePause = false;

            // 按品质+材料分组
            var groups = inventory.Where(t => t != null && !t.Destroyed)
                .GroupBy(t => new TransactionKey(t));

            var mainWin = MainTabWindow_星际商店.Instance;

            foreach (var g in groups)
            {
                int totalStock = g.Sum(t => t.stackCount);
                if (totalStock <= 0) continue;

                // 用分组中任意一件计算单价（含耐久折损）
                Thing sample = g.First();
                float hpFactor = Mathf.Clamp01((float)sample.HitPoints / (float)sample.MaxHitPoints);
                float basePrice = mainWin != null
                    ? mainWin.获取出售价格_公开(g.Key.def, g.Key.quality, g.Key.stuff)
                    : g.Key.def.BaseMarketValue * 0.8f;
                float price = basePrice * hpFactor;

                rows.Add(new VariantRow
                {
                    key = g.Key,
                    stock = totalStock,
                    unitPrice = price,
                    quantity = 0,
                    label = g.Key.ToString()
                });
            }

            // 排序：品质降序，同品质按材料
            rows = rows.OrderByDescending(r =>
            {
                if (r.key.quality != null) return (int)r.key.quality.Value;
                return -1; // 无品质的排最后
            }).ThenBy(r => r.key.stuff?.label ?? "").ToList();
        }

        public override Vector2 InitialSize => new Vector2(480f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y + 4f;
            float 可用宽 = inRect.width - 20f;

            // 标题
            GUI.color = MainTabWindow_星际商店.主色调_公开;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x + 10f, y, 可用宽, 28f),
                "StarStore_SellDialogTitle".Translate(thingDef.label));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            y += 30f;

            if (rows.Count == 0)
            {
                GUI.color = Color.grey;
                Widgets.Label(new Rect(inRect.x + 10f, y, 可用宽, 24f), "StarStore_NoVariants".Translate());
                GUI.color = Color.white;
                return;
            }

            // 列头
            float 数量列X = inRect.x + 可用宽 - 160f;
            float 价格列X = 数量列X - 70f;
            float 库存列X = 价格列X - 55f;

            GUI.color = new Color(0.5f, 0.55f, 0.65f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(inRect.x + 10f, y, 可用宽 - 300f, 列标签高), "StarStore_Variant".Translate());
            Widgets.Label(new Rect(库存列X, y, 55f, 列标签高), "StarStore_Stock".Translate());
            Widgets.Label(new Rect(价格列X, y, 70f, 列标签高), "StarStore_UnitPrice".Translate());
            Widgets.Label(new Rect(数量列X, y, 160f, 列标签高), "StarStore_Amount".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            y += 列标签高 + 2f;

            // 分隔线
            GUI.color = new Color(0.15f, 0.13f, 0.25f);
            Widgets.DrawLineHorizontal(inRect.x + 5f, y, 可用宽);
            y += 4f;
            GUI.color = Color.white;

            // 滚动区域
            float 列表总高 = rows.Count * (行高 + 4f);
            Rect 视图区域 = new Rect(inRect.x + 5f, y, 可用宽, inRect.yMax - y - 底部栏高 - 8f);
            Rect 内容区域 = new Rect(0f, 0f, 视图区域.width - 16f, 列表总高);

            Widgets.BeginScrollView(视图区域, ref scrollPosition, 内容区域);

            float ry = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                // 行背景（交替色）
                if (i % 2 == 0)
                {
                    GUI.color = new Color(0.10f, 0.10f, 0.18f);
                    Widgets.DrawRectFast(new Rect(0f, ry, 内容区域.width, 行高),
                        new Color(0.10f, 0.10f, 0.18f));
                }
                GUI.color = Color.white;

                // 品质颜色
                Color qColor = 获取品质颜色(row.key.quality);
                GUI.color = qColor;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(2f, ry + 4f, 内容区域.width - 300f, 22f), row.label);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // 库存
                GUI.color = new Color(0.6f, 0.65f, 0.75f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(库存列X - inRect.x - 5f, ry + 4f, 55f, 22f), row.stock.ToString());
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // 单价
                GUI.color = new Color(1f, 0.75f, 0.2f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(价格列X - inRect.x - 5f, ry + 4f, 70f, 22f),
                    "银" + row.unitPrice.ToString("F0"));
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // 数量控制
                float qX = 数量列X - inRect.x - 5f;
                Rect 减Rect = new Rect(qX, ry + 2f, 22f, 22f);
                if (Widgets.ButtonText(减Rect, "-"))
                {
                    if (row.quantity > 0) row.quantity--;
                }

                Rect 数量Rect = new Rect(qX + 24f, ry + 2f, 40f, 22f);
                string 数量文本 = row.quantity.ToString();
                数量文本 = Widgets.TextField(数量Rect, 数量文本);
                if (int.TryParse(数量文本, out int parsed))
                {
                    row.quantity = Mathf.Clamp(parsed, 0, row.stock);
                }

                Rect 加Rect = new Rect(qX + 66f, ry + 2f, 22f, 22f);
                if (Widgets.ButtonText(加Rect, "+"))
                {
                    if (row.quantity < row.stock) row.quantity++;
                }

                // 小计
                float 小计 = row.quantity * row.unitPrice;
                GUI.color = new Color(1f, 0.75f, 0.2f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(qX + 92f, ry + 4f, 60f, 22f),
                    "银" + 小计.ToString("F0"));
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                ry += 行高 + 4f;
            }

            Widgets.EndScrollView();

            // 底部栏
            float 底部Y = inRect.yMax - 底部栏高;

            // 分隔线
            GUI.color = new Color(0.15f, 0.13f, 0.25f);
            Widgets.DrawLineHorizontal(inRect.x + 5f, 底部Y - 2f, 可用宽);
            GUI.color = Color.white;

            // 统计
            int 选中总数 = rows.Sum(r => r.quantity);
            float 预计收益 = rows.Sum(r => r.quantity * r.unitPrice);
            GUI.color = new Color(0.8f, 0.85f, 0.9f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(inRect.x + 10f, 底部Y, 200f, 18f),
                "StarStore_SelectedSummary".Translate(选中总数));
            GUI.color = new Color(1f, 0.75f, 0.2f);
            Widgets.Label(new Rect(inRect.x + 10f, 底部Y + 17f, 200f, 18f),
                "StarStore_ExpectedRevenue".Translate(预计收益.ToString("F0")));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 全部出售按钮
            Rect 全部出售Rect = new Rect(inRect.x + 可用宽 - 200f, 底部Y + 2f, 84f, 26f);
            if (Widgets.ButtonText(全部出售Rect, "StarStore_MaxAll".Translate()))
            {
                for (int i = 0; i < rows.Count; i++)
                    rows[i].quantity = rows[i].stock;
            }

            // 加入购物车按钮
            Rect 加入购物车Rect = new Rect(inRect.x + 可用宽 - 110f, 底部Y + 2f, 100f, 26f);
            if (Widgets.ButtonText(加入购物车Rect, "StarStore_AddToCart".Translate()))
            {
                var mainWin = MainTabWindow_星际商店.Instance;
                if (mainWin != null)
                {
                    int added = 0;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (rows[i].quantity > 0)
                        {
                            mainWin.添加到交易数量(rows[i].key, rows[i].quantity);
                            added++;
                        }
                    }
                    if (added > 0)
                        mainWin.公开刷新();
                    提示信息 = "StarStore_AddedVariants".Translate(选中总数);
                }
                else
                {
                    Messages.Message("StarStore_MainWindowUnavailable".Translate(), MessageTypeDefOf.RejectInput);
                    Log.Error("星际商店: Dialog_SellVariant 无法获取 MainTabWindow_星际商店.Instance，出售变体未能加入购物车");
                }
                Close();
            }

            // 提示
            if (!string.IsNullOrEmpty(提示信息))
            {
                GUI.color = new Color(1f, 0.75f, 0.2f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(inRect.x + 10f, 底部Y + 36f, 可用宽, 16f), 提示信息);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }

        private Color 获取品质颜色(QualityCategory? q)
        {
            if (q == null) return new Color(0.7f, 0.75f, 0.8f);
            switch (q.Value)
            {
                case QualityCategory.Legendary: return new Color(1f, 0.85f, 0.3f);
                case QualityCategory.Masterwork: return new Color(1f, 0.75f, 0.25f);
                case QualityCategory.Excellent: return new Color(0.2f, 0.85f, 0.85f);
                case QualityCategory.Good: return new Color(0.3f, 0.8f, 0.4f);
                case QualityCategory.Normal: return new Color(0.7f, 0.75f, 0.8f);
                case QualityCategory.Poor: return new Color(0.9f, 0.5f, 0.2f);
                case QualityCategory.Awful: return new Color(0.9f, 0.2f, 0.2f);
                default: return Color.white;
            }
        }
    }
}
