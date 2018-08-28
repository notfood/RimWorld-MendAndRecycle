using System.Collections.Generic;

using Verse;
using RimWorld;

namespace MendAndRecycle
{
    static class JobDriverUtils
    {
        public static List<Thing> Reclaim (Thing thing, float efficiency)
        {
            List<ThingDefCountClass> costListAdj = thing.CostListAdjusted ();

            List<ThingDefCountClass> thingCountList;

            if (!costListAdj.NullOrEmpty ()) {
                thingCountList = costListAdj;
            } else if (!thing.def.smeltProducts.NullOrEmpty ()) {
                thingCountList = thing.def.smeltProducts;
            } else {
                thingCountList = null;
            }

            var list = new List<Thing> ();

            if (thingCountList != null) {
                foreach (var thingCost in thingCountList) {
                    if (!thingCost.thingDef.intricate) {
                        int mainSmeltProductCount = (int)UnityEngine.Mathf.Floor (thingCost.count * efficiency);
                        if (mainSmeltProductCount > 0) {
                            var resultantSmeltedThing = ThingMaker.MakeThing (thingCost.thingDef, null);
                            resultantSmeltedThing.stackCount = mainSmeltProductCount;
                            list.Add (resultantSmeltedThing);
                        }
                    }
                }
            }

            return list;
        }
    }
}

