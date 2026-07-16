using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    public partial class MainTabWindow_星际商店
    {
        // ================================================================
        //  看板（独立面板 - 深色报纸风，与商店蓝紫科技风形成视觉对比）
        //  AI 辅助生成
        // ================================================================
        private const float 看板内边距 = 8f;
        private Vector2 看板滚动;
        private int 新闻随机种子 = -1;
        private ThingDef 上次折扣物品 = null;  // AI：防止刷新后折扣物品变null导致区域消失

        public Window_看板 看板窗口;

        // 深色报纸风配色
        private static readonly Color 看板背景色 = new Color(0.10f, 0.07f, 0.04f, 0.92f);
        private static readonly Color 看板边框色 = new Color(0.30f, 0.20f, 0.10f, 0.60f);
        private static readonly Color 看板标题色 = new Color(0.85f, 0.65f, 0.30f);
        private static readonly Color 看板文字色 = new Color(0.82f, 0.78f, 0.70f);
        private static readonly Color 看板分隔色 = new Color(0.25f, 0.18f, 0.10f, 0.50f);
        private static readonly Color 看板区块折扣色 = new Color(1.0f, 0.45f, 0.1f);
        private static readonly Color 看板区块新闻色 = new Color(0.6f, 0.55f, 0.4f);
        private static readonly Color 看板区块故事色 = new Color(0.7f, 0.55f, 0.3f);

        public void 绘制看板公开(Rect rect)
        {
            绘制看板(rect);
        }

        /// <summary>获取当前看板应显示的折扣物品（与主窗口共享同一数据源）</summary>
        private ThingDef 获取看板折扣物品(StarStore_SidebarConfigDef cfg, int 今日天数)
        {
            // AI 辅助生成：统一使用主窗口的当前折扣物品，避免看板与物品格不一致
            return 当前折扣物品 ?? cfg?.获取今日折扣物品(今日天数);
        }

        private void 绘制看板(Rect rect)
        {
            StarStore_SidebarConfigDef cfg = 侧边栏管理器.配置;

            Widgets.DrawRectFast(rect, 看板背景色);
            GUI.color = 看板边框色;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            string title = cfg != null ? cfg.sidebarTitle : "StarStore_SidebarTitle".Translate();
            float 可用宽 = rect.width - 看板内边距 * 2f;

            GUI.color = 看板标题色;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width, 24f), "◆ " + title + " ◆");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float 内容Y = rect.y + 32f;

            Texture2D mascot = 当前看板娘?.获取贴图() ?? cfg?.获取看板娘贴图();
            if (mascot != null)
            {
                float 图标尺寸 = Mathf.Min(可用宽 - 8f, 140f);
                Rect 图标Rect = new Rect(rect.x + (rect.width - 图标尺寸) / 2f, 内容Y, 图标尺寸, 图标尺寸);
                GUI.DrawTexture(图标Rect, mascot, ScaleMode.ScaleToFit);
                内容Y += 图标尺寸 + 4f;
            }

            string 问候 = 当前问候语 ?? 缓存问候文字;
            if (!string.IsNullOrEmpty(问候))
            {
                GUI.color = 看板文字色;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperCenter;
                float 问候高 = Text.CalcHeight(问候, 可用宽 - 4f);
                Rect 问候Rect = new Rect(rect.x + 看板内边距, 内容Y, 可用宽, 问候高 + 4f);
                Widgets.Label(问候Rect, 问候);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                内容Y += 问候高 + 10f;
            }

            int 今日天数 = GenDate.DayOfYear(Find.TickManager.TicksAbs, 0f);

            Rect 视图Rect = new Rect(rect.x + 看板内边距, 内容Y, 可用宽, rect.yMax - 内容Y - 4f);
            float 滚动内容高 = 400f;
            Rect 滚动内容Rect = new Rect(0f, 0f, 可用宽 - 16f, 滚动内容高);
            Widgets.BeginScrollView(视图Rect, ref 看板滚动, 滚动内容Rect);

            float cy = 0f;

            // 每日折扣 —— AI：防止刷新后折扣物品为null导致区域消失
            ThingDef 折扣物品 = 获取看板折扣物品(cfg, 今日天数);
            if (折扣物品 == null && 上次折扣物品 != null)
                折扣物品 = 上次折扣物品;  // 保底：用上次的
            if (折扣物品 != null && cfg != null)
            {
                上次折扣物品 = 折扣物品;  // AI：记住本次，防刷新变null
                float 折数 = cfg.获取折扣比例() * 10f;
                string 折扣文本 = "StarStore_DiscountItem".Translate(折扣物品.LabelCap, 折数.ToString("F1"));
                cy = 绘制看板区块(滚动内容Rect, cy, "StarStore_DailyDiscount".Translate(),
                    看板区块折扣色, 折扣文本, 可用宽 - 4f, false);

                if (Prefs.DevMode)
                {
                    Rect 折扣刷新Rect = new Rect(滚动内容Rect.width - 4f, cy - 20f, 28f, 16f);
                    if (Widgets.ButtonText(折扣刷新Rect, "StarStore_Refresh".Translate()))
                    {
                        // AI 辅助生成：统一走主窗口的手动刷新方法，确保看板与物品格同步
                        手动刷新折扣();
                    }
                }
            }

            // 新闻公告
            string 新闻;
            if (新闻随机种子 >= 0 && cfg != null && cfg.newsList != null && cfg.newsList.Count > 0)
                新闻 = cfg.newsList[Rand.RangeSeeded(0, cfg.newsList.Count, 新闻随机种子)];
            else
                新闻 = cfg?.获取今日新闻(今日天数);

            if (!string.IsNullOrEmpty(新闻))
            {
                cy = 绘制看板区块(滚动内容Rect, cy, "StarStore_InterstellarNews".Translate(),
                    看板区块新闻色, 新闻, Mathf.Floor(可用宽 - 4f), true);
            }

            // 背景故事
            string 故事 = cfg?.backstory ?? "StarStore_Backstory".Translate();
            if (!string.IsNullOrEmpty(故事))
            {
                cy = 绘制看板区块(滚动内容Rect, cy, "StarStore_BackstoryTitle".Translate(),
                    看板区块故事色, 故事, 可用宽 - 4f, false);
            }

            Widgets.EndScrollView();
        }

        private float 绘制看板区块(Rect rect, float y, string 标题, Color 标题色, string 内容, float 文本宽, bool 显示刷新按钮)
        {
            GUI.color = 标题色;
            Text.Font = GameFont.Small;
            Rect 标题Rect = new Rect(0f, y, rect.width - (显示刷新按钮 ? 28f : 0f), 22f);
            Widgets.Label(标题Rect, 标题);
            GUI.color = Color.white;

            if (显示刷新按钮)
            {
                Rect 刷新Rect = new Rect(rect.width - 26f, y + 1f, 24f, 18f);
                if (Widgets.ButtonText(刷新Rect, "StarStore_Refresh".Translate()))
                {
                    新闻随机种子 = Rand.Range(0, 99999);
                    看板滚动 = Vector2.zero;
                }
            }

            float 内容高 = Text.CalcHeight(内容, 文本宽);
            Text.Font = GameFont.Tiny;
            GUI.color = 看板文字色;
            Widgets.Label(new Rect(0f, y + 22f, 文本宽, 内容高), 内容);
            GUI.color = Color.white;

            float 下Y = y + 22f + 内容高 + 4f;
            GUI.color = 看板分隔色;
            Widgets.DrawLineHorizontal(0f, 下Y, rect.width);
            GUI.color = Color.white;

            return 下Y + 6f;
        }
    }
}
