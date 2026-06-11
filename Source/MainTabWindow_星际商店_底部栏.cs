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
        //  底部栏（淘宝风格 - 橙色购买、绿色出售）
        // ================================================================
        private void 绘制底部栏(Rect rect)
        {
            // 顶部分隔线（淘宝橙色）
            GUI.color = 购买按钮色;  // 淘宝橙色
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
            GUI.color = Color.white;

            // 统计信息
            float 总价 = 0f;
            int 总数量 = 0;
            var 当前交易数量 = 是购买模式 ? 购买交易数量 : 出售交易数量;
            foreach (KeyValuePair<TransactionKey, int> kv in 当前交易数量)
            {
                if (kv.Value > 0)
                {
                    float 单价 = 是购买模式 ? 获取购买价格(kv.Key.def, kv.Key.quality, kv.Key.stuff) : 获取出售价格(kv.Key.def, kv.Key.quality, kv.Key.stuff);
                    总价 += 单价 * kv.Value;
                    总数量 += kv.Value;
                }
            }

            // 获取白银余额（一直显示）
            Map map = Find.CurrentMap;
            int 当前白银 = map != null ? 获取白银总量(map) : 0;
            int 需要白银 = Mathf.RoundToInt(总价);

            // 左侧：已选数量 + 白银余额（一直显示）
            GUI.color = 文字色;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            // 第一行：已选数量
            Widgets.Label(new Rect(rect.x + 5f, rect.y + 2f, 200f, 16f),
                "StarStore_SelectedCount".Translate(总数量, 总价.ToString("F0")));
            // 第二行：白银余额（金色）
            GUI.color = 价格色;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 5f, rect.y + 18f, 200f, 18f),
                "白银余额: ⛃" + 当前白银);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // 检查交易按钮是否可用
            bool 交易可用 = 总数量 > 0;
            string 提示 = "";
            if (是购买模式)
            {
                交易可用 = 当前白银 >= 需要白银;
                if (!交易可用 && 总数量 > 0)
                    提示 = "StarStore_SilverShortfall".Translate(需要白银, 当前白银);
            }

            // 提示文字（白银不足时显示）
            if (!string.IsNullOrEmpty(提示))
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x + 210f, rect.y + 18f, 200f, 18f), 提示);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }

            // 清空按钮（淘宝橙色边框）
            Rect 清空按钮 = new Rect(rect.xMax - 240f, rect.y + 5f, 100f, 26f);
            绘制科幻按钮(清空按钮, "StarStore_ClearButton".Translate(), false);
            if (Widgets.ButtonInvisible(清空按钮))
            {
                购买交易数量.Clear();
                出售交易数量.Clear();
            }

            // 交易按钮（淘宝橙色购买、绿色出售）
            string 按钮文字 = 是购买模式 ? "StarStore_BuyButton".Translate() : "StarStore_SellButton".Translate();
            Rect 交易按钮 = new Rect(rect.xMax - 130f, rect.y + 5f, 120f, 26f);

            Color btnColor = 交易可用 ? (是购买模式 ? 购买按钮色 : 出售按钮色) : 按钮不可用色;
            GUI.color = btnColor;
            if (交易可用 && Widgets.ButtonText(交易按钮, 按钮文字))
            {
                if (是购买模式) 执行购买();
                else 执行卖出();
            }
            else if (!交易可用)
            {
                Widgets.DrawRectFast(交易按钮, 按钮不可用色);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = 次要文字色;
                Widgets.Label(交易按钮, 按钮文字);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            GUI.color = Color.white;
        }
    }
}
