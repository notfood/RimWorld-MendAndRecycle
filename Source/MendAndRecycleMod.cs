using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MendAndRecycle
{
    class MendAndRecycleMod : Mod
    {
        public MendAndRecycleMod(ModContentPack mcp) : base(mcp)
        {
            GetSettings<Settings>();

            LongEventHandler.ExecuteWhenFinished(Inject);

            Patch();
        }

        public override string SettingsCategory()
        {
            return ResourceBank.Strings.MendAndRecycle;
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect) => Settings.DoSettingsWindowContents(inRect);

        static void Patch()
        {
            var harmony = new Harmony("notfood.mendandrecycle");

            harmony.Patch(AccessTools.Method(typeof(WorkGiver_DoBill), "StartOrResumeBillJob"), 
                postfix: new HarmonyMethod(typeof(MendAndRecycleMod), nameof(ReplaceJob)));
        }

        static Job ReplaceJob(Job __result)
        {
            if (__result?.RecipeDef.Worker is RecipeWorkerWithJob_Mend worker)
            {
                return new Job(ResourceBank.Job.Mend, __result.targetA)
                {
                    targetQueueB = __result.targetQueueB,
                    countQueue = __result.countQueue,
                    haulMode = __result.haulMode,
                    bill = __result.bill
                };
            }

            return __result;
        }

        static void Inject()
        {
            if (!Settings.useTableMending)
            {
                RemoveTable();
            }
            if (!Settings.requiresFuel)
            {
                RemoveFuel();
            }
            if (!Settings.useTableMending || !Settings.requiresFuel)
            {
                RemoveKits();
            }
            if (!Settings.requiresPower)
            {
                RemovePower();
            }

            SortApparelsInComplexity();
        }

        static void RemoveTable()
        {
            ResourceBank.Thing.TableMending.designationCategory = null;

            var mendApparel = new [] {ResourceBank.Recipe.MendSimpleApparel, ResourceBank.Recipe.MendComplexApparel};
            var mendWeapon = new [] {ResourceBank.Recipe.MendSimpleWeapon, ResourceBank.Recipe.MendComplexWeapon};

            foreach (var recipe in ResourceBank.Thing.TableMending.AllRecipes)
            {
                if (mendApparel.Contains(recipe))
                {
                    recipe.recipeUsers = ThingDefOf.Apparel_Parka.recipeMaker.recipeUsers;
                }
                else if (mendWeapon.Contains(recipe))
                {
                    recipe.recipeUsers = ThingDefOf.Piano.recipeMaker.recipeUsers;
                }
                else
                {
                    recipe.recipeUsers = ThingDefOf.Apparel_ShieldBelt.recipeMaker.recipeUsers;
                }
            }
        }

        static void RemoveFuel()
        {
            ResourceBank.Thing.TableMending?.comps?.RemoveAll(p => p.GetType() == typeof(CompProperties_Refuelable));
        }

        static void RemoveKits()
        {
            ResourceBank.Recipe.MakeMendingKit?.recipeUsers?.Clear();
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
                where def.useHitPoints && def.ingestible == null && (def.IsApparel ||  def.IsWeapon)
                select new { def, isComplex = HasComponents(def), isApparel = def.IsApparel}
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

            recipe.fixedIngredientFilter = filter;

            return filter;
        }
    }
}
