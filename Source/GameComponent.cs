using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Mending
{
    class GameComp : GameComponent
    {
        static bool init = false;
        public GameComp(Game game) : base()
        {
            if (!init)
            {
                init = true;
                RecipeDef simpleApparel = null;
                RecipeDef complexApparel = null;

                foreach (RecipeDef def in DefDatabase<RecipeDef>.AllDefs)
                {
                    if (def.defName.Equals("MendSimpleApparel"))
                    {
                        simpleApparel = def;
                        if (complexApparel != null)
                            break;
                    }
                    if (def.defName.Equals("MendComplexApparel"))
                    {
                        complexApparel = def;
                        if (simpleApparel != null)
                            break;
                    }
                }

                HashSet<ThingDef> simple = new HashSet<ThingDef>();
                HashSet<ThingDef> complex = new HashSet<ThingDef>();
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.IsApparel)
                    {
                        bool isComplex = false;
                        List<ThingDefCountClass> costList = def.costList;
                        if (costList != null)
                        {
                            foreach (ThingDefCountClass tdcc in costList)
                            {
                                if (tdcc.thingDef == ThingDefOf.ComponentIndustrial ||
                                    tdcc.thingDef == ThingDefOf.ComponentSpacer)
                                {
                                    isComplex = true;
                                    complex.Add(def);
                                    break;
                                }
                            }
                        }
                        if (!isComplex)
                        {
                            simple.Add(def);
                        }
                    }
                }

                AddDefToFilters(complexApparel, complex);
                AddDefToFilters(simpleApparel, simple);

                simple.Clear();
                simple = null;
                complex.Clear();
                complex = null;
            }
        }

        private static void AddDefToFilters(RecipeDef recDef, HashSet<ThingDef> defs)
        {
            FieldInfo allowedThingDefsFI = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.NonPublic | BindingFlags.Instance);
            allowedThingDefsFI.SetValue(recDef.defaultIngredientFilter, new HashSet<ThingDef>(defs));
            allowedThingDefsFI.SetValue(recDef.fixedIngredientFilter, new HashSet<ThingDef>(defs));
        }
    }
}
