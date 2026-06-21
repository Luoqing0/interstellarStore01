using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace 星际商店
{
    /// <summary>
    /// 星际商店 - 折扣相关静态缓存
    /// AI 辅助生成
    /// 由于 DefDatabase 在游戏运行期间不变，缓存只需构建一次
    /// </summary>
    public static class 星际商店折扣缓存
    {
        private static Dictionary<string, List<ThingDef>> _分类物品表;
        private static HashSet<string> _礼包物品集合;
        private static bool _已构建;

        /// <summary>获取“分类 → 可购买物品”映射表</summary>
        public static Dictionary<string, List<ThingDef>> 获取分类物品表()
        {
            if (!_已构建) 构建();
            return _分类物品表;
        }

        /// <summary>获取所有出现在固定礼包中的 ThingDef defName 集合</summary>
        public static HashSet<string> 获取礼包物品集合()
        {
            if (_礼包物品集合 == null)
            {
                _礼包物品集合 = new HashSet<string>();
                var bundles = DefDatabase<StarStore_BundleDef>.AllDefs;
                foreach (var b in bundles)
                {
                    if (b.fixedItems != null)
                    {
                        foreach (var e in b.fixedItems)
                        {
                            if (e.thingDef != null)
                                _礼包物品集合.Add(e.thingDef.defName);
                        }
                    }
                }
            }
            return _礼包物品集合;
        }

        /// <summary>强制重新构建缓存（用于加载新 Def 后，通常不需要手动调用）</summary>
        public static void 重建()
        {
            _已构建 = false;
            _礼包物品集合 = null;
            构建();
        }

        private static void 构建()
        {
            _分类物品表 = new Dictionary<string, List<ThingDef>>();
            List<string> 候选分类 = new List<string>
            {
                "StarStore_Cat_Food", "StarStore_Cat_Medicine", "StarStore_Cat_Weapons",
                "StarStore_Cat_Apparel", "StarStore_Cat_Animals", "StarStore_Cat_RawMaterials",
                "StarStore_Cat_Manufactured", "StarStore_Cat_Buildings", "StarStore_Cat_Furniture",
                "StarStore_Cat_Electronics"
            };

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.destroyOnDrop || def.BaseMarketValue <= 0f) continue;
                if (def.tradeability != Tradeability.Buyable && def.tradeability != Tradeability.All) continue;
                if (def.thingCategories.NullOrEmpty()) continue;

                foreach (string cat in 候选分类)
                {
                    if (星际商店工具.物品属于具体分类(def, cat))
                    {
                        if (!_分类物品表.ContainsKey(cat))
                            _分类物品表[cat] = new List<ThingDef>();
                        _分类物品表[cat].Add(def);
                        break; // 一个物品只归入第一个匹配分类
                    }
                }
            }
            _已构建 = true;
        }
    }
}
