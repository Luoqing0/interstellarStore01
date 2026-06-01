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
        //  数量控制（滑块 + 输入框 + 快速按钮）
        // ================================================================
        private void 绘制数量控制(Rect rect, TransactionKey key)
        {
            if (!交易数量.ContainsKey(key))
                交易数量[key] = 0;

            int 当前值 = 交易数量[key];

            // 滑块
            Rect 滑块Rect = new Rect(rect.x, rect.y, rect.width, 18f);
            float 浮点值 = (float)当前值;
            float 旧滑块值 = 浮点值;
            float 新值 = Widgets.HorizontalSlider(滑块Rect, 浮点值, 0f, 1000f, true, 当前值.ToString(), null, null, 0.5f);
            int 滑块值 = Mathf.RoundToInt(新值);

            // 输入框
            float 输入Y = 滑块Rect.yMax + 1f;
            float 输入高 = 18f;
            Rect 标签Rect = new Rect(rect.x, 输入Y, 28f, 输入高);
            Rect 输入Rect = new Rect(rect.x + 30f, 输入Y, rect.width - 30f, 输入高);
            GUI.color = 文字色;
            Widgets.Label(标签Rect, "StarStore_Quantity".Translate());
            GUI.color = Color.white;

            string 文本 = 当前值.ToString();
            Widgets.TextFieldNumeric(输入Rect, ref 当前值, ref 文本, 0, 999999);

            // 仅当滑块被实际拖动时才使用滑块值；否则以文本输入为准
            if (Mathf.Abs(新值 - 旧滑块值) > 0.001f)
                交易数量[key] = 滑块值;
            else
                交易数量[key] = 当前值;

            // 快速数量按钮：<<(归零) -1 +1 >>(最大)
            float 按钮Y = 输入Rect.yMax + 1f;
            float 按钮高 = 14f;
            float 按钮宽 = (rect.width - 6f) / 4f;

            // << 按钮 - 归零
            Rect 归零按钮 = new Rect(rect.x, 按钮Y, 按钮宽, 按钮高);
            if (Widgets.ButtonText(归零按钮, "<<"))
            {
                交易数量[key] = 0;
            }

            // -1 按钮
            Rect 减一按钮 = new Rect(rect.x + (按钮宽 + 2f), 按钮Y, 按钮宽, 按钮高);
            if (Widgets.ButtonText(减一按钮, "-1"))
            {
                if (交易数量[key] > 0) 交易数量[key]--;
            }

            // +1 按钮
            Rect 加一按钮 = new Rect(rect.x + (按钮宽 + 2f) * 2f, 按钮Y, 按钮宽, 按钮高);
            if (Widgets.ButtonText(加一按钮, "+1"))
            {
                交易数量[key]++;
            }

            // >> 按钮 - 最大数量
            Rect 最大按钮 = new Rect(rect.x + (按钮宽 + 2f) * 3f, 按钮Y, 按钮宽, 按钮高);
            if (Widgets.ButtonText(最大按钮, ">>"))
            {
                if (是购买模式)
                {
                    // 购买模式：设置为玩家剩余资金最多能购买的数量
                    Map map = Find.CurrentMap;
                    if (map != null)
                    {
                        float 单价 = 获取购买价格(key.def, key.quality, key.stuff);
                        if (单价 > 0f)
                        {
                            int 总白银 = 获取白银总量(map);
                            交易数量[key] = Mathf.FloorToInt(总白银 / 单价);
                        }
                    }
                }
                else
                {
                    // 卖出模式：设置为最多能出售的数量（匹配品质/材料的库存）
                    Map map = Find.CurrentMap;
                    if (map != null)
                    {
                        int 库存 = 0;
                        var 库存映射数据 = 获取库存映射(map);
                        if (库存映射数据.TryGetValue(key.def, out List<Thing> things))
                        {
                            for (int i = 0; i < things.Count; i++)
                            {
                                Thing t = things[i];
                                TransactionKey tk = new TransactionKey(t);
                                if (tk.Equals(key))
                                    库存 += t.stackCount;
                            }
                        }
                        交易数量[key] = 库存;
                    }
                }
            }
        }
    }
}
