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
        //  开发者模式 - 配置窗口
        // ================================================================
        private void 绘制配置窗口(Rect rect)
        {
            Widgets.DrawRectFast(rect, new Color(0.10f, 0.08f, 0.18f));
            GUI.color = 主色调;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            float y = rect.y + 10f;
            float 可用宽 = rect.width - 20f;
            float 标签高 = 22f;
            float 输入高 = 22f;
            float 按钮高 = 28f;

            // 标题
            GUI.color = 主色调;
            Widgets.Label(new Rect(rect.x + 10f, y, 可用宽, 标签高), "⚙ 交易条件配置 - " + 右键配置物品.label);
            y += 标签高 + 5f;

            // 物品信息
            GUI.color = 文字色;
            Widgets.Label(new Rect(rect.x + 10f, y, 可用宽, 标签高),
                "物品: " + 右键配置物品.defName + " | 基础价: " + 右键配置物品.BaseMarketValue);
            y += 标签高 + 5f;

            // 分隔线
            GUI.color = 边框色;
            Widgets.DrawLineHorizontal(rect.x + 5f, y, 可用宽);
            y += 10f;
            GUI.color = Color.white;

            // 计算内容总高度，动态设置ScrollView内容区域
            float 内容总高 = 0f;
            内容总高 += 标签高 + 2f + 输入高 + 2f; // 研究输入
            内容总高 += 5f;
            内容总高 += 标签高 + 2f + 输入高 + 2f; // 物品输入
            内容总高 += 5f;
            内容总高 += 按钮高 + 5f; // 隐藏按钮
            内容总高 += 10f;
            内容总高 += 按钮高 + 5f; // 保存/删除/关闭
            内容总高 += 标签高 + 10f; // 提示信息
            内容总高 += 按钮高 + 10f; // 导出按钮
            内容总高 += 60f; // 额外空间

            Rect 视图区域 = new Rect(rect.x, y, rect.width, rect.yMax - y - 10f);
            Rect 内容区域 = new Rect(0f, 0f, 可用宽 - 16f, 内容总高);
            Widgets.BeginScrollView(视图区域, ref 配置窗口滚动位置, 内容区域);

            float cy = 0f;

            // 研究项目输入
            GUI.color = 文字色;
            Widgets.Label(new Rect(0f, cy, 可用宽 - 16f, 标签高), "需要研究项目 (DefName, 留空则不限制):");
            cy += 标签高 + 2f;

            GUI.color = Color.white;
            配置研究输入 = Widgets.TextField(new Rect(0f, cy, 可用宽 - 16f, 输入高), 配置研究输入);
            cy += 输入高 + 2f;

            if (!string.IsNullOrEmpty(配置研究输入))
            {
                ResearchProjectDef rpDef = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(配置研究输入);
                if (rpDef != null)
                {
                    GUI.color = 价格色;
                    Widgets.Label(new Rect(0f, cy, 可用宽 - 16f, 标签高),
                        "✓ 已识别: " + rpDef.label + " (已解锁: " + rpDef.IsFinished + ")");
                }
                else
                {
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    Widgets.Label(new Rect(0f, cy, 可用宽 - 16f, 标签高), "✗ 未找到该研究项目");
                }
                cy += 标签高 + 2f;
            }

            cy += 5f;

            // 物品输入
            GUI.color = 文字色;
            Widgets.Label(new Rect(0f, cy, 可用宽 - 16f, 标签高), "需要殖民地物品 (DefName, 留空则不限制):");
            cy += 标签高 + 2f;

            GUI.color = Color.white;
            配置物品输入 = Widgets.TextField(new Rect(0f, cy, 可用宽 - 16f, 输入高), 配置物品输入);
            cy += 输入高 + 2f;

            if (!string.IsNullOrEmpty(配置物品输入))
            {
                ThingDef tDef = DefDatabase<ThingDef>.GetNamedSilentFail(配置物品输入);
                if (tDef != null)
                {
                    GUI.color = 价格色;
                    Widgets.Label(new Rect(0f, cy, 可用宽 - 16f, 标签高), "✓ 已识别: " + tDef.label);
                }
                else
                {
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    Widgets.Label(new Rect(0f, cy, 可用宽 - 16f, 标签高), "✗ 未找到该物品");
                }
                cy += 标签高 + 2f;
            }

            cy += 5f;

            // 隐藏/显示按钮
            bool 当前隐藏 = 交易条件管理器.条件Def.是否隐藏(右键配置物品.defName);
            Rect 隐藏按钮 = new Rect(0f, cy, 可用宽 - 16f, 按钮高);
            string 隐藏文字 = 当前隐藏 ? "🔓 取消隐藏此物品" : "🔒 隐藏此物品";
            if (Widgets.ButtonText(隐藏按钮, 隐藏文字))
            {
                if (当前隐藏)
                    交易条件管理器.条件Def.取消隐藏(右键配置物品.defName);
                else
                    交易条件管理器.条件Def.设置隐藏(右键配置物品.defName);
                配置提示信息 = 当前隐藏 ? "已取消隐藏" : "已隐藏";
                刷新物品列表();
            }
            cy += 按钮高 + 5f;

            cy += 10f;

            // 保存/删除/关闭 三个按钮
            float 按钮宽 = (可用宽 - 16f - 8f) / 3f;

            Rect 保存按钮 = new Rect(0f, cy, 按钮宽, 按钮高);
            if (Widgets.ButtonText(保存按钮, "保存条件"))
            {
                交易条件管理器.条件Def.设置条件(右键配置物品.defName, 配置研究输入, 配置物品输入);
                配置提示信息 = "条件已保存！";
                刷新物品列表();
            }

            Rect 删除按钮 = new Rect(按钮宽 + 4f, cy, 按钮宽, 按钮高);
            if (Widgets.ButtonText(删除按钮, "删除条件"))
            {
                交易条件管理器.条件Def.删除条件(右键配置物品.defName);
                配置研究输入 = "";
                配置物品输入 = "";
                配置提示信息 = "条件已删除！";
                刷新物品列表();
            }

            Rect 关闭按钮 = new Rect((按钮宽 + 4f) * 2f, cy, 按钮宽, 按钮高);
            if (Widgets.ButtonText(关闭按钮, "关闭"))
            {
                右键配置物品 = null;
            }

            cy += 按钮高 + 5f;

            // 提示信息
            if (!string.IsNullOrEmpty(配置提示信息))
            {
                GUI.color = 价格色;
                Widgets.Label(new Rect(0f, cy, 可用宽 - 16f, 标签高), 配置提示信息);
                GUI.color = Color.white;
                cy += 标签高 + 10f;
            }

            // 导出按钮
            Rect 导出按钮 = new Rect(0f, cy, 可用宽 - 16f, 按钮高);
            if (Widgets.ButtonText(导出按钮, "📤 导出所有配置为XML (保存到桌面)"))
            {
                导出配置XML();
            }

            Widgets.EndScrollView();
        }

        // ================================================================
        //  导出配置为XML
        // ================================================================
        private void 导出配置XML()
        {
            try
            {
                StarStore_TradeConditionDef 条件Def = 交易条件管理器.条件Def;
                if (条件Def.conditions == null || 条件Def.conditions.Count == 0)
                {
                    Messages.Message("没有交易条件可导出。", MessageTypeDefOf.RejectInput);
                    return;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sb.AppendLine("<Defs>");
                sb.AppendLine("  <!-- 星际商店 - 交易条件配置 -->");
                sb.AppendLine("  <!-- 将此文件放入其他模组的 Defs/ 目录即可覆盖条件 -->");
                sb.AppendLine("  <StarStore_TradeConditionDef>");
                sb.AppendLine("    <defName>StarStore_DefaultConditions</defName>");
                sb.AppendLine("    <label>星际商店默认交易条件</label>");
                sb.AppendLine("    <conditions>");

                for (int i = 0; i < 条件Def.conditions.Count; i++)
                {
                    物品交易条件 条件 = 条件Def.conditions[i];
                    sb.AppendLine("      <li>");
                    sb.AppendLine("        <thingDef>" + 转义XML(条件.thingDef) + "</thingDef>");
                    if (!string.IsNullOrEmpty(条件.requiredResearch))
                        sb.AppendLine("        <requiredResearch>" + 转义XML(条件.requiredResearch) + "</requiredResearch>");
                    if (!string.IsNullOrEmpty(条件.requiredItem))
                        sb.AppendLine("        <requiredItem>" + 转义XML(条件.requiredItem) + "</requiredItem>");
                    if (条件.隐藏)
                        sb.AppendLine("        <隐藏>true</隐藏>");
                    sb.AppendLine("      </li>");
                }

                sb.AppendLine("    </conditions>");
                sb.AppendLine("  </StarStore_TradeConditionDef>");
                sb.AppendLine("</Defs>");

                string 桌面路径 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                string 文件名 = "StarStore_TradeConditions_Export.xml";
                string 完整路径 = System.IO.Path.Combine(桌面路径, 文件名);
                System.IO.File.WriteAllText(完整路径, sb.ToString());

                Messages.Message("配置已导出到桌面: " + 文件名, MessageTypeDefOf.TaskCompletion);
                配置提示信息 = "已导出到桌面: " + 文件名;
            }
            catch (System.Exception ex)
            {
                Messages.Message("导出失败: " + ex.Message, MessageTypeDefOf.RejectInput);
                配置提示信息 = "导出失败: " + ex.Message;
            }
        }

        private string 转义XML(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
