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
        //  标题栏
        // ================================================================
        private void 绘制标题栏(Rect rect)
        {
            // 标题（左侧）
            GUI.color = 主色调;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y + 2f, 160f, 30f), "StarStore_Title".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 模式切换（标题旁边）
            float btnX = rect.x + 165f;
            Rect 购买按钮 = new Rect(btnX, rect.y + 4f, 按钮标准宽, 按钮标准高);
            Rect 卖出按钮 = new Rect(btnX + 65f, rect.y + 4f, 按钮标准宽, 按钮标准高);
            绘制科幻按钮(购买按钮, "StarStore_BuyMode".Translate(), 是购买模式);
            绘制科幻按钮(卖出按钮, "StarStore_SellMode".Translate(), !是购买模式);
            if (Widgets.ButtonInvisible(购买按钮)) { 是购买模式 = true; 当前页码 = 1; 刷新物品列表(); }
            if (Widgets.ButtonInvisible(卖出按钮)) { 是购买模式 = false; 当前页码 = 1; 刷新物品列表(); }

            // 搜索框（在模式按钮右侧）
            float 搜索X = btnX + 135f;
            Rect 搜索Rect = new Rect(搜索X, rect.y + 6f, 搜索框宽, 搜索框高);
            Widgets.DrawRectFast(搜索Rect, new Color(0.06f, 0.06f, 0.2f));
            GUI.color = 边框色;
            Widgets.DrawBox(搜索Rect);
            GUI.color = Color.white;
            string 旧搜索 = 搜索文本;
            搜索文本 = Widgets.TextField(搜索Rect, 搜索文本);
            if (搜索文本 != 旧搜索) { 当前页码 = 1; 刷新物品列表(); }

            // 布局选择按钮（大/中/小）- 放在搜索框右侧
            float 布局X = 搜索Rect.xMax + 10f;
            Rect 大布局按钮 = new Rect(布局X, rect.y + 4f, 24f, 20f);
            Rect 中布局按钮 = new Rect(布局X + 26f, rect.y + 4f, 24f, 20f);
            Rect 小布局按钮 = new Rect(布局X + 52f, rect.y + 4f, 24f, 20f);

            绘制科幻按钮(大布局按钮, "大", 当前布局 == 布局类型.大);
            绘制科幻按钮(中布局按钮, "中", 当前布局 == 布局类型.中);
            绘制科幻按钮(小布局按钮, "小", 当前布局 == 布局类型.小);

            if (Widgets.ButtonInvisible(大布局按钮)) { 应用布局(布局类型.大); }
            if (Widgets.ButtonInvisible(中布局按钮)) { 应用布局(布局类型.中); }
            if (Widgets.ButtonInvisible(小布局按钮)) { 应用布局(布局类型.小); }

            // 卖出模式：仅显示库存（布局按钮右侧，紧凑布局）
            if (!是购买模式)
            {
                float 库存X = 小布局按钮.xMax + 5f;
                Rect 库存开关 = new Rect(库存X, rect.y + 5f, 110f, 22f);
                bool 旧库存 = 仅显示库存;
                Widgets.CheckboxLabeled(库存开关, "StarStore_StockOnly".Translate(), ref 仅显示库存);
                if (仅显示库存 != 旧库存)
                {
                    当前页码 = 1;
                    刷新物品列表();
                }
            }

            // 右侧按钮
            Rect 购物车按钮 = new Rect(rect.xMax - 170f, rect.y + 4f, 70f, 按钮标准高);
            绘制科幻按钮(购物车按钮, "StarStore_Cart".Translate(), 显示购物车);
            if (Widgets.ButtonInvisible(购物车按钮)) 显示购物车 = !显示购物车;

            Rect 筛选按钮 = new Rect(rect.xMax - 90f, rect.y + 4f, 80f, 按钮标准高);
            绘制科幻按钮(筛选按钮, "StarStore_Filter".Translate(), 显示筛选面板);
            if (Widgets.ButtonInvisible(筛选按钮)) 显示筛选面板 = !显示筛选面板;

            // 分隔线
            GUI.color = 主色调暗;
            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
            GUI.color = Color.white;
        }

        // ================================================================
        //  分类标签页（顶部水平排列）
        // ================================================================
        private void 绘制分类标签页(Rect rect)
        {
            // 背景
            Widgets.DrawRectFast(rect, 分类栏背景);
            GUI.color = 边框色;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            // 水平排列标签页
            float 标签宽 = (rect.width - 8f) / 预定义分类.Count;
            float 标签高 = rect.height - 4f;
            for (int i = 0; i < 预定义分类.Count; i++)
            {
                string 标签 = 预定义分类[i];
                Rect 标签Rect = new Rect(rect.x + 2f + i * 标签宽, rect.y + 2f, 标签宽 - 2f, 标签高);
                bool 选中 = (当前分类标签 == 标签);

                // 选中状态用高亮色，未选中用暗色
                Color bgColor = 选中 ? 按钮选中色 : 按钮色;
                if (Mouse.IsOver(标签Rect) && !选中)
                    bgColor = 按钮hover色;

                Widgets.DrawRectFast(标签Rect, bgColor);
                if (选中)
                {
                    GUI.color = 主色调;
                    Widgets.DrawRectFast(new Rect(标签Rect.x, 标签Rect.yMax - 2f, 标签Rect.width, 2f), 主色调);
                    GUI.color = Color.white;
                }

                GUI.color = 选中 ? Color.white : 文字色;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(标签Rect, 标签.Translate());
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                if (Widgets.ButtonInvisible(标签Rect))
                {
                    当前分类标签 = 标签;
                    当前页码 = 1;
                    刷新物品列表();
                }
            }
        }

        // ================================================================
        //  科幻风格按钮
        // ================================================================
        private void 绘制科幻按钮(Rect rect, string 文字, bool 选中)
        {
            bool hover = Mouse.IsOver(rect);
            Color bg;
            if (选中)
                bg = 按钮选中色;
            else if (hover)
                bg = 按钮hover色;
            else
                bg = 按钮色;

            Widgets.DrawRectFast(rect, bg);
            GUI.color = 选中 ? Color.white : (hover ? Color.white : 文字色);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, 文字);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        // ================================================================
        //  筛选面板
        // ================================================================
        private void 绘制筛选面板(Rect rect)
        {
            Widgets.DrawRectFast(rect, new Color(0.10f, 0.08f, 0.18f));
            GUI.color = 主色调暗;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            float y = rect.y + 10f;
            Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 20f, 25f), "StarStore_FilterTitle".Translate());
            y += 30f;

            Rect 科技Rect = new Rect(rect.x + 10f, y, rect.width - 20f, 25f);
            Widgets.CheckboxLabeled(科技Rect, "StarStore_FilterTechOnly".Translate(), ref 仅已解锁科技);
            y += 30f;

            // 模组筛选下拉
            GUI.color = 文字色;
            Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 20f, 25f), "StarStore_FilterByMod".Translate());
            y += 22f;

            Rect 模组按钮Rect = new Rect(rect.x + 10f, y, rect.width - 20f, 25f);
            string 当前模组显示名 = 筛选模组 == "StarStore_AllMods" ? "StarStore_AllMods".Translate() : 筛选模组;
            if (Widgets.ButtonText(模组按钮Rect, 当前模组显示名.Truncate(30)))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                foreach (string modName in 可选模组列表)
                {
                    string captured = modName;
                    string label = captured == "StarStore_AllMods" ? "StarStore_AllMods".Translate() : captured;
                    opts.Add(new FloatMenuOption(label, () => {
                        筛选模组 = captured;
                        刷新物品列表();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            y += 30f;

            if (Widgets.ButtonText(new Rect(rect.x + 10f, rect.yMax - 35f, 80f, 25f), "StarStore_FilterApply".Translate()))
            {
                当前页码 = 1;
                刷新物品列表();
            }
        }
    }
}
