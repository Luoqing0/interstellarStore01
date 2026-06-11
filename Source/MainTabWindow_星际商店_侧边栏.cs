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
        //  左侧侧边栏（看板娘/折扣/新闻/背景故事）
        //  AI 辅助生成
        // ================================================================
        private const float 侧边栏宽 = 175f;
        private const float 侧边栏内边距 = 6f;
        private Vector2 侧边栏滚动;

        private void 绘制侧边栏(Rect rect)
        {
            StarStore_SidebarConfigDef cfg = 侧边栏管理器.配置;

            // 半透明背景
            Widgets.DrawRectFast(rect, new Color(0.04f, 0.07f, 0.16f, 0.75f));
            GUI.color = new Color(0.25f, 0.35f, 0.55f, 0.5f);
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            string title = cfg != null ? cfg.sidebarTitle : "星际商报";
            float 可用宽 = rect.width - 侧边栏内边距 * 2f;

            // 标题
            GUI.color = 购买按钮色;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x, rect.y + 3f, rect.width, 20f), "◆ " + title + " ◆");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            float 内容Y = rect.y + 28f;
            float 内容高 = rect.height - 32f;

            // 看板娘图片
            Texture2D mascot = cfg?.获取看板娘贴图();
            if (mascot != null)
            {
                float 图标尺寸 = Mathf.Min(可用宽 - 4f, 100f);
                Rect 图标Rect = new Rect(rect.x + (rect.width - 图标尺寸) / 2f, 内容Y, 图标尺寸, 图标尺寸);
                GUI.DrawTexture(图标Rect, mascot, ScaleMode.ScaleToFit);
                内容Y += 图标尺寸 + 4f;
            }

            // 滚动内容区
            Rect 视图Rect = new Rect(rect.x + 侧边栏内边距, 内容Y, 可用宽, rect.yMax - 内容Y - 4f);
            float 滚动内容高 = 400f;
            Rect 滚动内容Rect = new Rect(0f, 0f, 可用宽 - 16f, 滚动内容高);
            Widgets.BeginScrollView(视图Rect, ref 侧边栏滚动, 滚动内容Rect);

            float cy = 0f;

            // 每日折扣
            string 折扣 = cfg?.获取今日折扣() ?? "今日无折扣";
            if (!string.IsNullOrEmpty(折扣) && 折扣 != "今日无折扣")
            {
                cy = 绘制侧边栏区块(滚动内容Rect, cy, "🎫 今日折扣",
                    new Color(1f, 0.45f, 0.1f), 折扣, 可用宽 - 20f);
            }

            // 新闻公告
            string 新闻 = cfg?.获取今日新闻() ?? "暂无新闻";
            if (!string.IsNullOrEmpty(新闻) && 新闻 != "暂无新闻")
            {
                cy = 绘制侧边栏区块(滚动内容Rect, cy, "📰 星际新闻",
                    new Color(0.4f, 0.7f, 1f), 新闻, 可用宽 - 20f);
            }

            // 背景故事
            string 故事 = cfg?.backstory ?? "星际商店，连接银河系各个角落的贸易枢纽。";
            if (!string.IsNullOrEmpty(故事))
            {
                cy = 绘制侧边栏区块(滚动内容Rect, cy, "📖 背景故事",
                    new Color(0.7f, 0.65f, 0.9f), 故事, 可用宽 - 20f);
            }

            Widgets.EndScrollView();
        }

        private float 绘制侧边栏区块(Rect rect, float y, string 标题, Color 标题色, string 内容, float 文本宽)
        {
            // 区块标题
            GUI.color = 标题色;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(0f, y, rect.width, 18f), 标题);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            float 内容高 = Text.CalcHeight(内容, 文本宽);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.82f, 0.88f);
            Widgets.Label(new Rect(0f, y + 18f, 文本宽, 内容高), 内容);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // 分隔线
            float 下Y = y + 18f + 内容高 + 4f;
            GUI.color = new Color(0.2f, 0.25f, 0.4f, 0.5f);
            Widgets.DrawLineHorizontal(0f, 下Y, rect.width);
            GUI.color = Color.white;

            return 下Y + 6f;
        }
    }
}