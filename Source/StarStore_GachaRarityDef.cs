using UnityEngine;
using Verse;

/// <summary>
/// 星际商店 - 抽卡稀有度定义
/// AI 辅助生成
/// 放在全局命名空间，否则 RimWorld XML 解析器找不到
/// </summary>
public class StarStore_GachaRarityDef : Def
{
    /// <summary>抽取权重（越高越容易出）</summary>
    public float weight = 100f;

    /// <summary>稀有度显示颜色</summary>
    public Color rarityColor = Color.white;

    /// <summary>稀有度徽章文本（简短标识，如"普通""传说"）</summary>
    public string badgeText = "";

    /// <summary>价格倍率（该稀有度物品的售价额外乘数）</summary>
    public float priceMultiplier = 1.0f;

    /// <summary>十连保底层级（0=无保底，1=至少稀有，2=至少史诗）</summary>
    public int tenPullGuaranteeTier = 0;

    /// <summary>背景光效颜色（稀有度特效着色）</summary>
    public Color effectColor = Color.white;
}