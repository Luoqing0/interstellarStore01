using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

/// <summary>
/// 星际商店 - 捆绑包 Def
/// AI 辅助生成
/// 必须放在全局命名空间，否则 RimWorld XML 解析器找不到
/// </summary>
public class StarStore_BundleDef : Def
{
    /// <summary>固定物品条目</summary>
    public class BundleEntry
    {
        public ThingDef thingDef;
        public int count = 1;
        public QualityCategory quality = QualityCategory.Normal;
        public ThingDef stuff;
        public bool randomQuality = false;
    }

    /// <summary>随机组合条目</summary>
    public class RandomEntry
    {
        public int count = 1;
        public IntRange itemCountRange = new IntRange(1, 3);
        public List<ThingDef> thingDefPool = new List<ThingDef>();
        public bool randomQuality = false;
    }

    public List<BundleEntry> fixedItems = new List<BundleEntry>();
    public List<RandomEntry> randomGroups = new List<RandomEntry>();

    /// <summary>礼包折扣比例（0-1，0.75 表示 7.5 折）</summary>
    public float discountPercent = 0.75f;

    /// <summary>图标路径（相对于 Textures/，留空则自动回退到内容物品图标）</summary>
    public string iconPath;

    /// <summary>排序权重，越小越靠前</summary>
    public int sortOrder = 0;

    private Texture2D _iconTex;

    /// <summary>
    /// 获取礼包图标（带缓存）
    /// 优先级：XML 配置 -> 首个固定物品 -> 首个随机池物品 -> 默认礼盒
    /// AI 辅助生成
    /// </summary>
    public Texture2D 获取图标()
    {
        if (_iconTex != null && _iconTex != BaseContent.BadTex) return _iconTex;

        Texture2D tex = null;

        // 1. XML 配置图标
        if (!string.IsNullOrEmpty(iconPath))
            tex = ContentFinder<Texture2D>.Get(iconPath, false);

        // 2. 首个固定物品图标
        if (tex == null || tex == BaseContent.BadTex)
        {
            ThingDef firstFixed = fixedItems?.FirstOrDefault(e => e?.thingDef != null)?.thingDef;
            if (firstFixed != null)
                tex = firstFixed.uiIcon;
        }

        // 3. 首个随机池物品图标
        if (tex == null || tex == BaseContent.BadTex)
        {
            ThingDef firstRandom = randomGroups?
                .FirstOrDefault(g => g?.thingDefPool != null && g.thingDefPool.Count > 0)?
                .thingDefPool.FirstOrDefault();
            if (firstRandom != null)
                tex = firstRandom.uiIcon;
        }

        // 4. 默认礼盒图标
        if (tex == null || tex == BaseContent.BadTex)
            tex = ContentFinder<Texture2D>.Get("UI/Buttons/StarStore_Bundle", false);

        _iconTex = tex ?? BaseContent.BadTex;
        return _iconTex;
    }

    /// <summary>礼包是否包含任何有效内容</summary>
    public bool 是否有效()
    {
        return (fixedItems != null && fixedItems.Any(e => e.thingDef != null)) ||
               (randomGroups != null && randomGroups.Any(g => g.thingDefPool != null && g.thingDefPool.Any()));
    }

    /// <summary>获取有效的折扣率</summary>
    public float 获取折扣率()
    {
        return discountPercent <= 0f || discountPercent >= 1f ? 0.75f : discountPercent;
    }
}
