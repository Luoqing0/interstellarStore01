using System.Collections.Generic;
using System.Linq;
using Verse;

namespace 星际商店
{
    /// <summary>
    /// 星际商店通用工具方法（AI 辅助生成）
    /// 供交易窗口、折扣计算等共享使用
    /// </summary>
    public static class 星际商店工具
    {
        /// <summary>
        /// 判断物品是否属于某个具体商店分类（不含 All / Favorites）
        /// </summary>
        public static bool 物品属于具体分类(ThingDef def, string 分类Key)
        {
            if (def.thingCategories == null) return false;

            HashSet<string> 所有分类 = 获取所有分类(def);

            if (分类Key == "StarStore_Cat_Food")
                return 所有分类.Any(c =>
                    c == "Foods" || c == "FoodMeals" || c == "FoodRaw" ||
                    c == "FoodManufactured" || c == "PlantsFood" ||
                    c == "AnimalProducts" || c == "Eggs" ||
                    c == "MeatRaw" || c == "Milk" ||
                    c.StartsWith("Food"));
            if (分类Key == "StarStore_Cat_Medicine")
                return 所有分类.Any(c =>
                    c == "Medicine" || c == "Drugs" ||
                    c == "MedicalItems" || c.StartsWith("Drug") ||
                    c.StartsWith("Medi"));
            if (分类Key == "StarStore_Cat_Weapons")
                return 所有分类.Any(c =>
                    c == "Weapons" || c.StartsWith("Weapon") ||
                    c == "Guns" || c == "MeleeWeapons" ||
                    c == "RangedWeapons" || c == "Grenades" ||
                    c == "MortarShells");
            if (分类Key == "StarStore_Cat_Apparel")
                return 所有分类.Any(c =>
                    c == "Apparel" || c.StartsWith("Apparel") ||
                    c == "Armor" || c == "Clothing" ||
                    c == "Headgear" || c == "Shields");
            if (分类Key == "StarStore_Cat_Animals")
                return 所有分类.Any(c => c == "Animals") || (def.race != null && def.race.Animal);
            if (分类Key == "StarStore_Cat_RawMaterials")
                return 所有分类.Any(c =>
                    c == "Resources" || c == "RawMaterials" ||
                    c == "Metals" || c == "StoneBlocks" ||
                    c == "Wood" || c == "Textile" ||
                    c == "Leather" || c == "Fabrics" ||
                    c == "Chemicals" || c.StartsWith("Resource") ||
                    c.StartsWith("Raw") || c.StartsWith("Stone") ||
                    c.StartsWith("Metal"));
            if (分类Key == "StarStore_Cat_Manufactured")
                return 所有分类.Any(c =>
                    c == "Manufactured" || c == "Components" ||
                    c == "Parts" || c == "Tools" ||
                    c == "CraftingMaterials" || c.StartsWith("Manufactur"));
            if (分类Key == "StarStore_Cat_Buildings")
                return 所有分类.Any(c =>
                    c == "Buildings" || c.StartsWith("Building") ||
                    c == "Structures" || c == "Floors" ||
                    c == "Walls" || c == "Doors" ||
                    c == "Security" || c == "Power" ||
                    c == "Production" || c.StartsWith("Structure"));
            if (分类Key == "StarStore_Cat_Furniture")
                return 所有分类.Any(c =>
                    c == "BuildingsFurniture" || c == "Furniture" ||
                    c == "BuildingsJoy" || c == "Joy" ||
                    c == "BuildingsArt" || c == "Art" ||
                    c == "BuildingsTemperature" || c == "Temperature" ||
                    c == "Beds" || c == "Tables" || c == "Chairs" ||
                    c == "Lighting" || c == "Storage" ||
                    c == "Containers" || c == "Sculpture" ||
                    c == "Recreation" || c == "Furnishings");
            if (分类Key == "StarStore_Cat_Electronics")
                return 所有分类.Any(c =>
                    c == "BuildingsPower" || c == "Power" ||
                    c == "BuildingsSecurity" || c == "Security" ||
                    c == "BuildingsProduction" || c == "Production" ||
                    c == "BuildingsSpecial" || c == "Special" ||
                    c == "BuildingsMisc" || c == "Misc" ||
                    c == "Electronics" || c.StartsWith("Electron") ||
                    c == "Components" || c == "Chips" ||
                    c == "Mechanoids" || c == "MechParts" ||
                    c == "Energy" || c == "Batteries" || c == "Solar");
            if (分类Key == "StarStore_Cat_Misc")
                return 所有分类.Any(c =>
                    c == "Misc" || c == "Miscellaneous" ||
                    c == "Items" || c == "Goods" ||
                    c == "Chunks" || c == "Corpses" ||
                    c == "Plants" ||
                    c == "Seeds" || c == "Books");
            return false;
        }

        /// <summary>
        /// 递归获取物品的所有分类（包括父分类）
        /// </summary>
        public static HashSet<string> 获取所有分类(ThingDef def)
        {
            HashSet<string> 结果 = new HashSet<string>();
            if (def.thingCategories == null) return 结果;

            foreach (ThingCategoryDef cat in def.thingCategories)
            {
                添加分类及父分类(cat, 结果);
            }
            return 结果;
        }

        private static void 添加分类及父分类(ThingCategoryDef cat, HashSet<string> 结果)
        {
            if (cat == null || 结果.Contains(cat.defName)) return;

            结果.Add(cat.defName);

            if (cat.parent != null)
            {
                添加分类及父分类(cat.parent, 结果);
            }
        }
    }
}
