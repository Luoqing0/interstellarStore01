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
        private int 折扣随机种子 = -1;
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

        private void 绘制看板(Rect rect)
        {
            StarStore_SidebarConfigDef cfg = 侧边栏管理器.配置;

            Widgets.DrawRectFast(rect, 看板背景色);
            GUI.color = 看板边框色;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            string title = cfg != null ? cfg.sidebarTitle : "星际商报";
            float 可用宽 = rect.width - 看板内边距 * 2f;

            GUI.color = 看板标题色;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width, 24f), "◆ " + title + " ◆");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float 内容Y = rect.y + 32f;

            Texture2D mascot = cfg?.获取看板娘贴图();
            if (mascot != null)
            {
                float 图标尺寸 = Mathf.Min(可用宽 - 8f, 140f);
                Rect 图标Rect = new Rect(rect.x + (rect.width - 图标尺寸) / 2f, 内容Y, 图标尺寸, 图标尺寸);
                GUI.DrawTexture(图标Rect, mascot, ScaleMode.ScaleToFit);
                内容Y += 图标尺寸 + 4f;
            }

            if (!string.IsNullOrEmpty(缓存问候文字))
            {
                GUI.color = 看板文字色;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperCenter;
                float 问候高 = Text.CalcHeight(缓存问候文字, 可用宽 - 4f);
                Rect 问候Rect = new Rect(rect.x + 看板内边距, 内容Y, 可用宽, 问候高 + 4f);
                Widgets.Label(问候Rect, 缓存问候文字);
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
            ThingDef 折扣物品 = cfg?.获取今日折扣物品(今日天数);
            if (折扣随机种子 >= 0 && cfg != null)
            {
                // 用随机种子查一次，如果非null就用它
                ThingDef 随机物品 = cfg.获取今日折扣物品(折扣随机种子);
                if (随机物品 != null)
                    折扣物品 = 随机物品;
                // 如果随机种子返回null则保持原折扣物品（不消失）
            }
            if (折扣物品 == null && 上次折扣物品 != null)
                折扣物品 = 上次折扣物品;  // 保底：用上次的
            if (折扣物品 != null && cfg != null)
            {
                上次折扣物品 = 折扣物品;  // AI：记住本次，防刷新变null
                float 折数 = cfg.获取折扣比例() * 10f;
                string 折扣文本 = "折扣 " + 折扣物品.LabelCap + " " + 折数.ToString("F1") + "折";
                cy = 绘制看板区块(滚动内容Rect, cy, "每日折扣",
                    看板区块折扣色, 折扣文本, 可用宽 - 4f, false);

                if (Prefs.DevMode)
                {
                    Rect 折扣刷新Rect = new Rect(滚动内容Rect.width - 4f, cy - 20f, 28f, 16f);
                    if (Widgets.ButtonText(折扣刷新Rect, "刷新"))
                    {
                        折扣随机种子 = Rand.Range(0, 99999);
                        ThingDef 新折扣 = cfg.获取今日折扣物品(折扣随机种子);
                        if (新折扣 != null) 当前折扣物品 = 新折扣;  // AI：同步给交易和物品网格
                    }
                }
            }

            // 新闻公告
            string 新闻;
            if (新闻随机种子 >= 0 && cfg != null && cfg.newsList != null && cfg.newsList.Count > 0)
                新闻 = cfg.newsList[Rand.RangeSeeded(0, cfg.newsList.Count, 新闻随机种子)];
            else
                新闻 = cfg?.获取今日新闻(今日天数) ?? "暂无新闻";

            if (!string.IsNullOrEmpty(新闻) && 新闻 != "暂无新闻")
            {
                cy = 绘制看板区块(滚动内容Rect, cy, "星际新闻",
                    看板区块新闻色, 新闻, 可用宽 - 4f, true);
            }

            // 背景故事
            string 故事 = cfg?.backstory ?? "星际商店，连接银河系各个角落的贸易枢纽。";
            if (!string.IsNullOrEmpty(故事))
            {
                cy = 绘制看板区块(滚动内容Rect, cy, "背景故事",
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
                if (Widgets.ButtonText(刷新Rect, "刷新"))
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