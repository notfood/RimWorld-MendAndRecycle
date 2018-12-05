using System;
using System.Linq;

using RimWorld;
using Verse;

namespace MendAndRecycle
{
    class MendAndRecycleMod : Mod
    {
        public MendAndRecycleMod(ModContentPack mcp) : base(mcp)
        {
            GetSettings<Settings>();

            LongEventHandler.ExecuteWhenFinished(Inject);
        }

        public override string SettingsCategory()
        {
            return ResourceBank.Strings.MendAndRecycle;
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect) => Settings.DoSettingsWindowContents(inRect);

        static void Inject()
        {
            if (!Settings.requiresFuel)
            {
                RemoveFuel();
            }
            if (!Settings.requiresPower)
            {
                RemovePower();
            }

            SortApparelsInComplexity();
        }

        static void RemoveFuel()
        {
            ResourceBank.Recipe.MakeMendingKit?.recipeUsers?.Clear();
            ResourceBank.Thing.TableMending?.comps?.RemoveAll(p => p.GetType() == typeof(CompProperties_Refuelable));
        }

        static void RemovePower()
        {
            ResourceBank.ResearchProject.Mending?.prerequisites?.RemoveAll(r => r == ResourceBank.ResearchProject.Electricity);
            ResourceBank.Thing.TableMending?.comps?.RemoveAll(p => p.GetType() == typeof(CompProperties_Power));
        }

        static void SortApparelsInComplexity() {
            // select and group ThingDefs by complexity
            var query = (
                from def in DefDatabase<ThingDef>.AllDefs
                where def.IsApparel ||  def.IsWeapon
                select new { def = def, isComplex = HasComponents(def), isApparel = def.IsApparel}
            );

            var mendComplexApparel = GetCleanFilter(ResourceBank.Recipe.MendComplexApparel);
            var mendSimpleApparel = GetCleanFilter(ResourceBank.Recipe.MendSimpleApparel);
            var mendComplexWeapon = GetCleanFilter(ResourceBank.Recipe.MendComplexWeapon);
            var mendSimpleWeapon = GetCleanFilter(ResourceBank.Recipe.MendSimpleWeapon);

            foreach (var item in query)
            {
                ThingFilter filter;
                if (item.isApparel) {
                    if (item.isComplex)
                    {
                        filter = mendComplexApparel;
                    }
                    else
                    {
                        filter = mendSimpleApparel;
                    }
                } else {
                    if (item.isComplex)
                    {
                        filter = mendComplexWeapon;
                    }
                    else
                    {
                        filter = mendSimpleWeapon;
                    }
                }

                filter.SetAllow(item.def, true);
            }
        }

        static bool HasComponents(ThingDef def)
        {
            return def.costList?.Any(
                t => t.thingDef == ThingDefOf.ComponentIndustrial
                  || t.thingDef == ThingDefOf.ComponentSpacer) ?? false;
        }

        static ThingFilter GetCleanFilter(RecipeDef recipe) {
            var ingredientCount = new IngredientCount();

            recipe.ingredients.Clear();
            recipe.ingredients.Add(ingredientCount);

            var filter = ingredientCount.filter;
            filter.SetDisallowAll();
            filter.AllowedHitPointsPercents = new FloatRange()
            {
                min = 0f, max = 0.99f
            };

            recipe.defaultIngredientFilter = filter;
            recipe.fixedIngredientFilter = filter;

            return filter;
        }
    }
}
