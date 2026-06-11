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
        //  购物车面板（淘宝风格 - 橙色购买、绿色出售）
        // ================================================================
        private void 绘制购物车(Rect rect)
        {
            // 半透明玻璃背景
            Widgets.DrawRectFast(rect, 购物车背景);
            GUI.color = 主色调暗;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            // 标题（淘宝橙色）- 增加高度避免截断
            GUI.color = 购买按钮色;  // 淘宝橙色
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 32f), "🛒 " + "StarStore_Cart".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 分两个区域：购买清单 和 出售清单
            float y = rect.y + 38f;  // 增加标题区域高度
            float halfHeight = (rect.yMax - y - 8f) / 2f;

            // === 上半部分：购买清单（淘宝橙色）===
            Rect 购买区域 = new Rect(rect.x, y, rect.width, halfHeight);
            绘制购物车分区(购买区域, true, "🛒 购买清单", 购买按钮色, ref 购物车购买滚动);

            // === 下半部分：出售清单（绿色）===
            Rect 出售区域 = new Rect(rect.x, y + halfHeight + 4f, rect.width, halfHeight - 4f);
            绘制购物车分区(出售区域, false, "💰 出售清单", 出售按钮色, ref 购物车出售滚动);
        }

        private void 绘制购物车分区(Rect rect, bool 购买模式分区, string 标题, Color 标题色, ref Vector2 scrollPos)
        {
            // 分区标题（增加高度避免截断）
            GUI.color = 标题色;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, 22f), 标题);
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
                Widgets.Label(new Rect(rect.x, rect.y + 28f, rect.width, 24f), "（空）");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            // 滚动列表（动态行高，避免换行截断）
            float 列表Y = rect.y + 26f;
            // 第一遍：预计算总高度
            float 列表高 = 0f;
            Text.Font = GameFont.Tiny;
            for (int i = 0; i < 分区物品.Count; i++)
            {
                string 预名 = 分区物品[i].Key.ToString();
                if (预名.Length > 6) 预名 = 预名.Substring(0, 6) + "..";
                float 名高 = Text.CalcHeight(预名, 60f);
                列表高 += Mathf.Max(24f, 名高 + 4f);
            }
            Rect 列表视图 = new Rect(rect.x + 2f, 列表Y, rect.width - 4f, rect.yMax - 列表Y - 22f);
            Rect 列表内容 = new Rect(0f, 0f, 列表视图.width - 16f, 列表高);

            Widgets.BeginScrollView(列表视图, ref scrollPos, 列表内容);

            float cy = 0f;
            for (int i = 0; i < 分区物品.Count; i++)
            {
                var kv = 分区物品[i];
                float 单价 = 购买模式分区 ? 获取购买价格(kv.Key.def, kv.Key.quality, kv.Key.stuff)
                                        : 获取出售价格(kv.Key.def, kv.Key.quality, kv.Key.stuff);

                // 商品名 - 动态高度
                Text.Font = GameFont.Tiny;
                string 商品名 = kv.Key.ToString();
                if (商品名.Length > 6)
                    商品名 = 商品名.Substring(0, 6) + "..";
                float 名高 = Text.CalcHeight(商品名, 60f);
                float 行高 = Mathf.Max(24f, 名高 + 4f);
                
                // 商品名（白色）
                GUI.color = 文字色;
                Rect 商品名Rect = new Rect(8f, cy + 1f, 60f, 名高);
                Widgets.Label(商品名Rect, 商品名);
                
                float 居中Y = cy + (行高 - 20f) / 2f;
                // 数量（科技蓝，与价格区分）
                GUI.color = 主色调;
                Rect 数量Rect = new Rect(商品名Rect.xMax, 居中Y, 30f, 20f);
                Widgets.Label(数量Rect, "x" + kv.Value);
                
                // 总价（金色）
                GUI.color = 价格色;
                Rect 总价Rect = new Rect(数量Rect.xMax, 居中Y, 50f, 20f);
                Widgets.Label(总价Rect, "⛃" + (单价 * kv.Value).ToString("F0"));
                
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                // 删除按钮（右对齐，垂直居中）
                Rect 删除Rect = new Rect(列表内容.width - 24f, 居中Y + 1f, 20f, 18f);
                if (Widgets.ButtonText(删除Rect, "✕"))
                {
                    dict.Remove(kv.Key);
                    刷新物品列表();
                }
                cy += 行高;
            }
            Widgets.EndScrollView();

            // 分区总计（增加高度避免截断）
            GUI.color = 标题色;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.yMax - 20f, rect.width - 8f, 20f),
                "小计: ⛃" + 分区总价.ToString("F0"));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
