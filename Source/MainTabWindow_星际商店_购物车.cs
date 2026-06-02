using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    public partial class MainTabWindow_星际商店
    {
        // ================================================================
        //  购物车面板（玻璃态半透明风格）
        // ================================================================
        private void 绘制购物车(Rect rect)
        {
            // 半透明玻璃背景
            Widgets.DrawRectFast(rect, 购物车背景);
            GUI.color = 主色调暗;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            // 标题
            GUI.color = 主色调;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 25f), "🛒 " + "StarStore_Cart".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 分两个区域：购买清单 和 出售清单
            float y = rect.y + 32f;
            float halfHeight = (rect.yMax - y - 5f) / 2f;

            // === 上半部分：购买清单 ===
            Rect 购买区域 = new Rect(rect.x, y, rect.width, halfHeight);
            绘制购物车分区(购买区域, true, "🛒 购买清单", 购买按钮色, ref 购物车购买滚动);

            // === 下半部分：出售清单 ===
            Rect 出售区域 = new Rect(rect.x, y + halfHeight + 2f, rect.width, halfHeight - 2f);
            绘制购物车分区(出售区域, false, "💰 出售清单", 出售按钮色, ref 购物车出售滚动);
        }

        private void 绘制购物车分区(Rect rect, bool 购买模式分区, string 标题, Color 标题色, ref Vector2 scrollPos)
        {
            // 分区标题
            GUI.color = 标题色;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, 16f), 标题);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 统计当前模式匹配的物品
            List<KeyValuePair<TransactionKey, int>> 分区物品 = new List<KeyValuePair<TransactionKey, int>>();
            float 分区总价 = 0f;
            var dict = 购买模式分区 ? 购买交易数量 : 出售交易数量;
            foreach (var kv in dict)
            {
                if (kv.Value > 0)
                {
                    float 单价 = 购买模式分区 ? 获取购买价格(kv.Key.def, kv.Key.quality, kv.Key.stuff)
                                            : 获取出售价格(kv.Key.def, kv.Key.quality, kv.Key.stuff);
                    分区物品.Add(kv);
                    分区总价 += 单价 * kv.Value;
                }
            }

            if (分区物品.Count == 0)
            {
                GUI.color = 次要文字色;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(rect.x, rect.y + 20f, rect.width, 20f), "（空）");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            // 滚动列表
            float 列表Y = rect.y + 18f;
            float 列表高 = 分区物品.Count * 20f;
            Rect 列表视图 = new Rect(rect.x + 2f, 列表Y, rect.width - 4f, rect.yMax - 列表Y - 16f);
            Rect 列表内容 = new Rect(0f, 0f, 列表视图.width - 16f, 列表高);

            Widgets.BeginScrollView(列表视图, ref scrollPos, 列表内容);

            float cy = 0f;
            for (int i = 0; i < 分区物品.Count; i++)
            {
                var kv = 分区物品[i];
                float 单价 = 购买模式分区 ? 获取购买价格(kv.Key.def, kv.Key.quality, kv.Key.stuff)
                                        : 获取出售价格(kv.Key.def, kv.Key.quality, kv.Key.stuff);

                GUI.color = 购买模式分区 ? 文字色 : 出售按钮色;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(0f, cy, 列表内容.width - 22f, 18f),
                    kv.Key.ToString().Truncate(16) + " x" + kv.Value + " ⛃" + (单价 * kv.Value).ToString("F0"));
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // 删除按钮
                Rect 删除Rect = new Rect(列表内容.width - 20f, cy + 1f, 18f, 16f);
                if (Widgets.ButtonText(删除Rect, "✕"))
                {
                    dict.Remove(kv.Key);
                    刷新物品列表();
                }
                cy += 20f;
            }
            Widgets.EndScrollView();

            // 分区总计
            GUI.color = 标题色;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.yMax - 14f, rect.width - 8f, 14f),
                "小计: ⛃" + 分区总价.ToString("F0"));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
