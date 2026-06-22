using System.Collections.Generic;
using Verse;

/// <summary>
/// 星际商店 - 抽卡卡池定义
/// AI 辅助生成
/// 放在全局命名空间，否则 RimWorld XML 解析器找不到
/// </summary>
public class StarStore_GachaDef : Def
{
    /// <summary>单抽价格（银币）</summary>
    public int singlePullCost = 500;

    /// <summary>十连价格（银币，通常 10 倍有折扣）</summary>
    public int tenPullCost = 4500;

    /// <summary>十连折扣描述（如 "9折"）</summary>
    public string tenPullDiscountLabel = "9折";

    /// <summary>十连是否保底稀有</summary>
    public bool tenPullGuaranteeRare = true;

    /// <summary>保底稀有度等级（通常稀有）</summary>
    public string guaranteeRarityDefName = "";

    /// <summary>按稀有度分组的物品池</summary>
    public List<GachaPoolEntry> poolEntries = new List<GachaPoolEntry>();

    /// <summary>卡池横幅图标路径</summary>
    public string bannerIconPath;

    /// <summary>排序权重（越小越靠前）</summary>
    public int sortOrder = 0;

    /// <summary>单抽描述标题</summary>
    public string singleLabel = "";

    /// <summary>十连描述标题</summary>
    public string tenLabel = "";
}

/// <summary>
/// 卡池条目：每个稀有度下的物品列表
/// </summary>
public class GachaPoolEntry
{
    /// <summary>该条目的稀有度 DefName</summary>
    public string rarityDefName;

    /// <summary>该稀有度下的物品列表</summary>
    public List<ThingDef> thingDefs = new List<ThingDef>();
}