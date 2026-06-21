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

    /// <summary>图标路径（相对于 Textures/）</summary>
    public string iconPath = "UI/Buttons/StarStore";

    /// <summary>排序权重，越小越靠前</summary>
    public int sortOrder = 0;

    private Texture2D _iconTex;

    /// <summary>获取礼包图标（带缓存）</summary>
    public Texture2D 获取图标()
    {
        if (_iconTex == null || _iconTex == BaseContent.BadTex)
            _iconTex = ContentFinder<Texture2D>.Get(iconPath, false);
        return _iconTex ?? BaseContent.BadTex;
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
