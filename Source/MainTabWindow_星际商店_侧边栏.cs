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
        //  左侧侧边栏（看板娘/问候/折扣/新闻/背景故事）
        //  AI 辅助生成
        // ================================================================
        private const float 侧边栏内边距 = 8f;
        private Vector2 侧边栏滚动;
        private int 新闻随机种子 = -1;  // 点击刷新时随机变化

        private void 绘制侧边栏(Rect rect)
        {
            StarStore_SidebarConfigDef cfg = 侧边栏管理器.配置;

            // 侧边栏背景（半透明）
            Widgets.DrawRectFast(rect, new Color(0.05f, 0.08f, 0.18f, 0.88f));
            GUI.color = new Color(0.3f, 0.4f, 0.6f, 0.5f);
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            string title = cfg != null ? cfg.sidebarTitle : "星际商报";
            float 可用宽 = rect.width - 侧边栏内边距 * 2f;

            // 标题
            GUI.color = 购买按钮色;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width, 24f), "◆ " + title + " ◆");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float 内容Y = rect.y + 32f;

            // 看板娘图片（大区域）
            Texture2D mascot = cfg?.获取看板娘贴图();
            if (mascot != null)
            {
                float 图标尺寸 = Mathf.Min(可用宽 - 8f, 140f);
                Rect 图标Rect = new Rect(rect.x + (rect.width - 图标尺寸) / 2f, 内容Y, 图标尺寸, 图标尺寸);
                GUI.DrawTexture(图标Rect, mascot, ScaleMode.ScaleToFit);
                内容Y += 图标尺寸 + 4f;
            }

            // 随机问候
            if (cfg != null && cfg.greetings != null && cfg.greetings.Count > 0)
            {
                int 问候Idx = Rand.Range(0, cfg.greetings.Count);  // 每次打开随机
                string 问候 = cfg.greetings[问候Idx];
                if (!string.IsNullOrEmpty(问候))
                {
                    GUI.color = new Color(0.7f, 0.75f, 0.85f);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperCenter;
                    float 问候高 = Text.CalcHeight(问候, 可用宽 - 4f);
                    Rect 问候Rect = new Rect(rect.x + 侧边栏内边距, 内容Y, 可用宽, 问候高 + 4f);
                    Widgets.Label(问候Rect, 问候);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                    内容Y += 问候高 + 10f;
                }
            }

            // 游戏内日期
            int 今日天数 = GenDate.DayOfYear(Find.TickManager.TicksAbs, 0f);

            // 滚动内容区
            Rect 视图Rect = new Rect(rect.x + 侧边栏内边距, 内容Y, 可用宽, rect.yMax - 内容Y - 4f);
            float 滚动内容高 = 400f;
            Rect 滚动内容Rect = new Rect(0f, 0f, 可用宽 - 16f, 滚动内容高);
            Widgets.BeginScrollView(视图Rect, ref 侧边栏滚动, 滚动内容Rect);

            float cy = 0f;

            // 每日折扣（游戏内日期）
            ThingDef 折扣物品 = cfg?.获取今日折扣物品(今日天数);
            if (折扣物品 != null && cfg != null)
            {
                float 折数 = cfg.获取折扣比例() * 10f;
                string 折扣文本 = "🎫 " + 折扣物品.LabelCap + " " + 折数.ToString("F1") + "折";
                cy = 绘制侧边栏区块(滚动内容Rect, cy, "每日折扣",
                    new Color(1f, 0.45f, 0.1f), 折扣文本, 可用宽 - 20f, false);

                // 开发者模式：刷新折扣按钮
                if (Prefs.DevMode)
                {
                    Rect 折扣刷新Rect = new Rect(滚动内容Rect.width - 4f, cy - 20f, 20f, 16f);
                    if (Widgets.ButtonText(折扣刷新Rect, "⟳"))
                    {
                        新闻随机种子 = Rand.Range(0, 99999);
                    }
                }
            }

            // 新闻公告（带刷新按钮）
            string 新闻;
            if (新闻随机种子 >= 0 && cfg != null && cfg.newsList != null && cfg.newsList.Count > 0)
                // 已刷新过：随机取一条
                新闻 = cfg.newsList[Rand.RangeSeeded(0, cfg.newsList.Count, 新闻随机种子)];
            else
                新闻 = cfg?.获取今日新闻(今日天数) ?? "暂无新闻";

            if (!string.IsNullOrEmpty(新闻) && 新闻 != "暂无新闻")
            {
                cy = 绘制侧边栏区块(滚动内容Rect, cy, "星际新闻",
                    new Color(0.4f, 0.7f, 1f), 新闻, 可用宽 - 20f, true);
            }

            // 背景故事
            string 故事 = cfg?.backstory ?? "星际商店，连接银河系各个角落的贸易枢纽。";
            if (!string.IsNullOrEmpty(故事))
            {
                cy = 绘制侧边栏区块(滚动内容Rect, cy, "背景故事",
                    new Color(0.7f, 0.65f, 0.9f), 故事, 可用宽 - 20f, false);
            }

            Widgets.EndScrollView();
        }

        private float 绘制侧边栏区块(Rect rect, float y, string 标题, Color 标题色, string 内容, float 文本宽, bool 显示刷新按钮)
        {
            // 区块标题（增大行高避免截断）
            GUI.color = 标题色;
            Text.Font = GameFont.Small;
            Rect 标题Rect = new Rect(0f, y, rect.width - (显示刷新按钮 ? 28f : 0f), 22f);
            Widgets.Label(标题Rect, 标题);
            GUI.color = Color.white;

            // 新闻刷新按钮
            if (显示刷新按钮)
            {
                Rect 刷新Rect = new Rect(rect.width - 26f, y + 1f, 24f, 18f);
                if (Widgets.ButtonText(刷新Rect, "⟳"))
                {
                    新闻随机种子 = Rand.Range(0, 99999);
                    侧边栏滚动 = Vector2.zero;
                }
            }

            float 内容高 = Text.CalcHeight(内容, 文本宽);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.82f, 0.88f);
            Widgets.Label(new Rect(0f, y + 22f, 文本宽, 内容高), 内容);
            GUI.color = Color.white;

            // 分隔线
            float 下Y = y + 22f + 内容高 + 4f;
            GUI.color = new Color(0.2f, 0.25f, 0.4f, 0.5f);
            Widgets.DrawLineHorizontal(0f, 下Y, rect.width);
            GUI.color = Color.white;

            return 下Y + 6f;
        }
    }
}