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

        // 紧凑数量控制V2（大布局，滑块去内置数字 + 5按钮单行）
        // AI 辅助生成：修复按钮失效 —— 按钮操作后同步更新当前值给TextFieldNumeric
        private void 绘制紧凑数量控制V2(Rect rect, TransactionKey key)
        {
            if (!当前交易数量.ContainsKey(key))
                当前交易数量[key] = 0;

            int 当前值 = 当前交易数量[key];

            // 滑块（隐藏内置数字，避免与输入框重复）
            float 滑块Y = rect.y;
            float 滑块高 = 14f;
            Rect 滑块Rect = new Rect(rect.x, 滑块Y, rect.width, 滑块高);
            float 浮点值 = (float)当前值;
            float 新值 = Widgets.HorizontalSlider(滑块Rect, 浮点值, 0f, 1000f, true, "", null, null, 0.5f);
            if (Mathf.Abs(新值 - 浮点值) > 0.001f)
            {
                int 滑块值 = Mathf.RoundToInt(新值);
                当前交易数量[key] = 滑块值;
                当前值 = 滑块值;
            }

            // 按钮行
            float 按钮Y = 滑块Rect.yMax + 1f;
            float 小按钮宽 = 20f;
            float 间隙 = 2f;
            // 输入框宽度：优先 50f（可显示 5 位数），但不超过可用宽度
            // 修复"三千多显示三百"的视觉截断（输入框原宽 30f 只能显示 3 位数）
            float 可用控制宽 = rect.width;
            float 数量宽 = Mathf.Min(50f, 可用控制宽 - 小按钮宽 * 4f - 间隙 * 5f);
            if (数量宽 < 30f) 数量宽 = 30f;  // 保底不小于原来的 30f
            float 总宽 = 小按钮宽 * 4f + 数量宽 + 间隙 * 5f;
            float 起始X = rect.x + (可用控制宽 - 总宽) / 2f;

            // << 归零
            Rect 归零 = new Rect(起始X, 按钮Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(归零, "<<"))
            {
                当前交易数量[key] = 0;
                当前值 = 0;  // AI：同步给后续TextFieldNumeric
            }

            // -1
            Rect 减一 = new Rect(起始X + 小按钮宽 + 间隙, 按钮Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(减一, "-1"))
            {
                if (当前交易数量[key] > 0)
                {
                    当前交易数量[key]--;
                    当前值 = 当前交易数量[key];  // AI：同步
                }
            }

            // 数量输入框
            Rect 数量Rect = new Rect(起始X + (小按钮宽 + 间隙) * 2f, 按钮Y, 数量宽, 16f);
            // 使用持久化 buffer 避免光标乱跳：原代码每帧重建 buffer 导致 TextFieldNumeric 焦点丢失
            if (!数量输入缓冲.TryGetValue(key, out string 数量文本) || 数量文本 == null)
                数量文本 = 当前值.ToString();
            // 按钮操作改变了值时，同步 buffer
            if (!数量文本.Equals(当前值.ToString()) && !GUI.GetNameOfFocusedControl().StartsWith("TextField"))
                数量文本 = 当前值.ToString();
            // AI：当前值已与字典同步，TextFieldNumeric回写不会覆盖按钮修改
            Widgets.TextFieldNumeric(数量Rect, ref 当前值, ref 数量文本, 0, 999999);
            数量输入缓冲[key] = 数量文本;
            当前交易数量[key] = 当前值;

            // +1
            Rect 加一 = new Rect(起始X + (小按钮宽 + 间隙) * 2f + 数量宽 + 间隙, 按钮Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(加一, "+1"))
            {
                当前交易数量[key]++;
                当前值 = 当前交易数量[key];  // AI：同步
            }

            // >> 最大
            Rect 最大 = new Rect(起始X + (小按钮宽 + 间隙) * 3f + 数量宽 + 间隙, 按钮Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(最大, ">>"))
            {
                if (是购买模式)
                {
                    Map map = Find.CurrentMap;
                    if (map != null)
                    {
                        float 单价 = 获取购买价格(key.def, key.quality, key.stuff);
                        if (单价 > 0f)
                        {
                            int 总白银 = 获取白银总量(map);
                            当前交易数量[key] = Mathf.FloorToInt(总白银 / 单价);
                            当前值 = 当前交易数量[key];  // AI：同步
                        }
                    }
                }
                else
                {
                    Map map = Find.CurrentMap;
                    if (map != null)
                    {
                        int 库存 = 0;
                        // 机械族是 Pawn，不在库存映射中，需独立计数
                        if (key.def.race != null && key.def.race.IsMechanoid)
                        {
                            库存 = 机械族管理器.获取殖民地机械族(key.def, map, 9999).Count();
                        }
                        // 动物也是 Pawn，不在库存映射中，需独立计数
                        else if (key.def.race != null && key.def.race.Animal)
                        {
                            库存 = 机械族管理器.获取殖民地动物(key.def, map, 9999).Count();
                        }
                        else
                        {
                            var 库存映射数据 = 获取库存映射(map);
                            if (库存映射数据.TryGetValue(key.def, out List<Thing> things))
                            {
                                for (int i = 0; i < things.Count; i++)
                                {
                                    Thing t = things[i];
                                    TransactionKey tk = new TransactionKey(t);
                                    if (tk.宽松匹配(key))
                                        库存 += t.stackCount;
                                }
                            }
                        }
                        当前交易数量[key] = 库存;
                        当前值 = 库存;  // AI：同步
                    }
                }
            }
        }

        // 迷你数量控制（只显示 +/- 按钮，用于极小格子）
        private void 绘制迷你数量控制(Rect rect, TransactionKey key)
        {
            if (!当前交易数量.ContainsKey(key))
                当前交易数量[key] = 0;

            float 按钮宽 = rect.width / 2f - 2f;

            Rect 减按钮 = new Rect(rect.x, rect.y, 按钮宽, rect.height);
            if (Widgets.ButtonText(减按钮, "-"))
            {
                if (当前交易数量[key] > 0)
                    当前交易数量[key]--;
            }

            Rect 加按钮 = new Rect(rect.x + 按钮宽 + 4f, rect.y, 按钮宽, rect.height);
            if (Widgets.ButtonText(加按钮, "+"))
            {
                当前交易数量[key]++;
            }
        }

        // 完整数量控制（中布局，有滑块+输入+4按钮，回到原版功能）
        // AI 辅助生成：滑块去内置数字，按钮同步修复
        private void 绘制完整数量控制(Rect rect, TransactionKey key)
        {
            if (!当前交易数量.ContainsKey(key))
                当前交易数量[key] = 0;

            int 当前值 = 当前交易数量[key];

            // 滑块（隐藏内置数字）
            Rect 滑块Rect = new Rect(rect.x, rect.y + 2f, rect.width, 14f);
            float 浮点值 = (float)当前值;
            float 新值 = Widgets.HorizontalSlider(滑块Rect, 浮点值, 0f, 1000f, true, "", null, null, 0.5f);
            if (Mathf.Abs(新值 - 浮点值) > 0.001f)
            {
                int 滑块值 = Mathf.RoundToInt(新值);
                当前交易数量[key] = 滑块值;
                当前值 = 滑块值;
            }

            // 输入框 + 四按钮在下一行
            float 输入Y = 滑块Rect.yMax + 1f;
            float 按钮总宽 = rect.width - 50f;
            float 小按钮宽 = (按钮总宽 - 6f) / 4f;

            // 归零 <<
            Rect 归零 = new Rect(rect.x, 输入Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(归零, "<<")) { 当前交易数量[key] = 0; 当前值 = 0; }

            // -1
            Rect 减一 = new Rect(rect.x + 小按钮宽 + 2f, 输入Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(减一, "-1")) { if (当前交易数量[key] > 0) { 当前交易数量[key]--; 当前值 = 当前交易数量[key]; } }

            // 输入框
            // 使用持久化 buffer 避免光标乱跳
            if (!数量输入缓冲.TryGetValue(key, out string 文本) || 文本 == null)
                文本 = 当前值.ToString();
            if (!文本.Equals(当前值.ToString()) && !GUI.GetNameOfFocusedControl().StartsWith("TextField"))
                文本 = 当前值.ToString();
            Rect 输入Rect = new Rect(rect.x + (小按钮宽 + 2f) * 2f, 输入Y, 小按钮宽, 16f);
            Widgets.TextFieldNumeric(输入Rect, ref 当前值, ref 文本, 0, 999999);
            数量输入缓冲[key] = 文本;
            当前交易数量[key] = 当前值;

            // +1
            Rect 加一 = new Rect(rect.x + (小按钮宽 + 2f) * 3f, 输入Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(加一, "+1")) { 当前交易数量[key]++; 当前值 = 当前交易数量[key]; }

            // >> 最大
            Rect 最大 = new Rect(rect.x + (小按钮宽 + 2f) * 4f, 输入Y, 小按钮宽, 16f);
            if (Widgets.ButtonText(最大, ">>"))
            {
                if (是购买模式)
                {
                    Map map = Find.CurrentMap;
                    if (map != null)
                    {
                        float 单价 = 获取购买价格(key.def, key.quality, key.stuff);
                        if (单价 > 0f)
                        {
                            int 总白银 = 获取白银总量(map);
                            当前交易数量[key] = Mathf.FloorToInt(总白银 / 单价);
                            当前值 = 当前交易数量[key];
                        }
                    }
                }
                else
                {
                    Map map = Find.CurrentMap;
                    if (map != null)
                    {
                        int 库存 = 0;
                        // 机械族是 Pawn，不在库存映射中，需独立计数
                        if (key.def.race != null && key.def.race.IsMechanoid)
                        {
                            库存 = 机械族管理器.获取殖民地机械族(key.def, map, 9999).Count();
                        }
                        // 动物也是 Pawn，不在库存映射中，需独立计数
                        else if (key.def.race != null && key.def.race.Animal)
                        {
                            库存 = 机械族管理器.获取殖民地动物(key.def, map, 9999).Count();
                        }
                        else
                        {
                            var 库存映射数据 = 获取库存映射(map);
                            if (库存映射数据.TryGetValue(key.def, out List<Thing> things))
                            {
                                for (int i = 0; i < things.Count; i++)
                                {
                                    Thing t = things[i];
                                    TransactionKey tk = new TransactionKey(t);
                                    if (tk.宽松匹配(key)) 库存 += t.stackCount;
                                }
                            }
                        }
                        当前交易数量[key] = 库存;
                        当前值 = 库存;
                    }
                }
            }
        }
    }
}