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
        //  捆绑包页面
        //  AI 辅助生成
        // ================================================================
        private Vector2 捆绑包滚动位置 = Vector2.zero;

        private List<StarStore_BundleDef> 当前有效礼包
        {
            get
            {
                return 捆绑包管理器.获取所有有效礼包()
                    .OrderBy(b => b.sortOrder)
                    .ThenBy(b => b.label)
                    .ToList();
            }
        }

        private void 绘制捆绑包网格(Rect rect)
        {
            if (!是购买模式)
            {
                GUI.color = 次要文字色;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "StarStore_BundleBuyOnly".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            var bundles = 当前有效礼包;
            if (bundles.NullOrEmpty())
            {
                GUI.color = 次要文字色;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "StarStore_BundleEmpty".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            // 开发者模式：在右上角添加刷新礼包按钮
            if (Prefs.DevMode)
            {
                Rect 刷新Rect = new Rect(rect.xMax - 60f, rect.y + 4f, 56f, 22f);
                if (Widgets.ButtonText(刷新Rect, "刷新"))
                {
                    刷新礼包列表();
                    Messages.Message("StarStore_DevRefreshBundles".Translate(), MessageTypeDefOf.TaskCompletion);
                }
            }

            float 可用宽 = rect.width - 网格内边距;
            float 起始Y = Prefs.DevMode ? 32f : 0f;
            float 卡片高 = 118f;
            float 卡片间距 = 8f;
            float 内容高 = bundles.Count * (卡片高 + 卡片间距) + 网格内边距;

            Rect 视图区域 = new Rect(0f, 0f, 可用宽 - 16f, 内容高);
            Widgets.BeginScrollView(new Rect(rect.x, rect.y + 起始Y, rect.width, rect.height - 起始Y), ref 捆绑包滚动位置, 视图区域);

            for (int i = 0; i < bundles.Count; i++)
            {
                StarStore_BundleDef bundle = bundles[i];
                Rect 卡片 = new Rect(4f, i * (卡片高 + 卡片间距), 可用宽 - 24f, 卡片高);
                绘制捆绑包卡片(卡片, bundle);
            }

            Widgets.EndScrollView();
        }

        private void 绘制捆绑包卡片(Rect rect, StarStore_BundleDef bundle)
        {
            // 背景
            Widgets.DrawRectFast(rect, 格子背景色);
            GUI.color = 边框色;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            float 内边距 = 8f;
            float iconSize = rect.height - 内边距 * 2f;
            Rect 图标Rect = new Rect(rect.x + 内边距, rect.y + 内边距, iconSize, iconSize);
            Texture2D icon = bundle.获取图标();
            if (icon != null)
                GUI.DrawTexture(图标Rect, icon, ScaleMode.ScaleToFit);

            float 文本X = 图标Rect.xMax + 内边距;
            float 文本宽 = rect.width - 文本X - 内边距 - 90f;

            // 名称
            GUI.color = 文字色;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(文本X, rect.y + 内边距, 文本宽, 22f), bundle.LabelCap);

            // 内容摘要
            Text.Font = GameFont.Tiny;
            GUI.color = 次要文字色;
            string summary = 获取礼包内容摘要(bundle);
            float 描述高 = Text.CalcHeight(summary, 文本宽);
            Widgets.Label(new Rect(文本X, rect.y + 内边距 + 22f, 文本宽, Mathf.Min(描述高, rect.height - 内边距 * 2f - 22f)), summary);

            // 价格
            float 原价 = 捆绑包管理器.获取原价(bundle);
            float 现价 = 捆绑包管理器.获取折扣价(bundle);
            Rect 原价Rect = new Rect(rect.xMax - 90f - 内边距, rect.y + 内边距, 90f, 18f);
            Rect 现价Rect = new Rect(rect.xMax - 90f - 内边距, rect.y + 内边距 + 20f, 90f, 22f);

            GUI.color = 原价色;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(原价Rect, "StarStore_BundleOriginalPrice".Translate(原价.ToString("F0")));

            GUI.color = 价格色;
            Text.Font = GameFont.Small;
            Widgets.Label(现价Rect, "StarStore_BundleDiscountPrice".Translate(现价.ToString("F0")));

            // AI 辅助生成：详情按钮 + 购买按钮（购买改为打开二次确认窗口）
            Rect 详情按钮Rect = new Rect(rect.xMax - 90f - 内边距, rect.yMax - 60f - 内边距, 90f, 26f);
            Color 详情按钮色 = new Color(0.35f, 0.30f, 0.48f);
            Widgets.DrawRectFast(详情按钮Rect, 详情按钮色);
            GUI.color = Color.white;
            if (Widgets.ButtonText(详情按钮Rect, "StarStore_BundleDetails".Translate()))
            {
                Find.WindowStack.Add(new Dialog_BundleDetails(bundle, false, null));
            }

            Rect 购买按钮Rect = new Rect(rect.xMax - 90f - 内边距, rect.yMax - 28f - 内边距, 90f, 28f);
            bool 可购买 = Find.CurrentMap != null && 获取白银总量(Find.CurrentMap) >= Mathf.RoundToInt(现价);
            Color 购买按钮背景色 = 可购买 ? 购买按钮色 : 按钮不可用色;
            Widgets.DrawRectFast(购买按钮Rect, 购买按钮背景色);
            GUI.color = Color.white;
            if (Widgets.ButtonText(购买按钮Rect, "StarStore_BundleBuy".Translate()))
            {
                if (可购买)
                    Find.WindowStack.Add(new Dialog_BundleDetails(bundle, true, () => 执行购买捆绑包(bundle)));
                else
                    Messages.Message("StarStore_BundleCannotAfford".Translate(bundle.LabelCap, Mathf.RoundToInt(现价), 获取白银总量(Find.CurrentMap)), MessageTypeDefOf.RejectInput);
            }
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private string 获取礼包内容摘要(StarStore_BundleDef bundle)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (bundle.fixedItems != null)
            {
                foreach (var e in bundle.fixedItems)
                {
                    if (e.thingDef == null) continue;
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(e.thingDef.label + " x" + e.count);
                }
            }
            if (bundle.randomGroups != null)
            {
                foreach (var g in bundle.randomGroups)
                {
                    if (g.thingDefPool == null) continue;
                    if (sb.Length > 0) sb.Append("; ");
                    sb.Append("随机: ");
                    sb.Append(string.Join(", ", g.thingDefPool.Select(d => d.label).ToArray()));
                }
            }
            if (sb.Length == 0) return bundle.description;
            return sb.ToString();
        }

        private void 执行购买捆绑包(StarStore_BundleDef bundle)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            float 总价 = 捆绑包管理器.获取折扣价(bundle);
            int 需要白银 = Mathf.RoundToInt(总价);
            int 当前白银 = 获取白银总量(map);
            if (当前白银 < 需要白银)
            {
                Messages.Message("StarStore_BundleCannotAfford".Translate(bundle.LabelCap, 需要白银, 当前白银), MessageTypeDefOf.RejectInput);
                return;
            }

            // 两阶段白银扣减（修复吞白银 Bug，详见 扣除白银 方法注释）
            if (!扣除白银(map, 需要白银))
            {
                Messages.Message("StarStore_BundleCannotAfford".Translate(bundle.LabelCap, 需要白银, 获取白银总量(map)), MessageTypeDefOf.RejectInput);
                return;
            }

            // 生成礼包内容
            List<Thing> 待生成 = 捆绑包管理器.生成礼包内容(bundle);
            if (待生成.Count == 0) return;

            // AI 辅助生成：复用主窗口的分发逻辑，建筑直接生成，其他进运输舱
            LookTargets dropTarget;
            if (分发购买物品(待生成, map, out dropTarget))
                Messages.Message("StarStore_BundlePurchased".Translate(bundle.LabelCap), dropTarget, MessageTypeDefOf.TaskCompletion);
        }
    }
}
