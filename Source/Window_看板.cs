using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 看板独立窗口 - 浮动在主商店窗口左侧，脱离商店灰色边框
    /// AI 辅助生成
    /// </summary>
    public class Window_看板 : Window
    {
        private MainTabWindow_星际商店 主窗口;
        private float 看板宽 = 220f;

        public Window_看板(MainTabWindow_星际商店 主窗口)
        {
            this.主窗口 = 主窗口;
            doCloseButton = false;
            doCloseX = false;
            absorbInputAroundWindow = false;
            closeOnAccept = false;
            closeOnClickedOutside = false;
            closeOnCancel = false;        // AI：不响应Esc，让主窗口统一处理
            forcePause = false;
            draggable = false;
            preventCameraMotion = false;
            layer = WindowLayer.GameUI;
        }

        public override Vector2 InitialSize => new Vector2(看板宽, 600f);

        public void 更新位置(float x, float y, float 高度)
        {
            windowRect.x = x;
            windowRect.y = y;
            windowRect.width = 看板宽;
            windowRect.height = 高度;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (主窗口 != null)
            {
                主窗口.绘制看板公开(inRect);
            }
        }

        public override void OnCancelKeyPressed()
        {
            // Esc时关闭主窗口（主窗口PreClose会自动关闭看板）
            if (主窗口 != null)
            {
                主窗口.Close(false);
            }
        }
    }
}