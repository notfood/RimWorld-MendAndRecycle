using System;

using RimWorld;
using Verse;
using Verse.AI;

namespace MendAndRecycle
{
    public abstract class WorkGiver_DoBill : RimWorld.WorkGiver_Scanner
    {
        static readonly IntRange ReCheckFailedBillTicksRange = new IntRange (500, 600);

        readonly JobDef jobDef;

        bool ignoreHitPoints;

        public override PathEndMode PathEndMode { get { return PathEndMode.Touch; } }

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Count == 1)
                {
                    return ThingRequest.ForDef(def.fixedBillGiverDefs[0]);
                }
                return ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
            }
        }

        protected WorkGiver_DoBill (JobDef job, bool ignoreHitPoints)
        {
            jobDef = job;
            this.ignoreHitPoints = ignoreHitPoints;
        }

        public override Job JobOnThing (Pawn pawn, Thing t, bool forced = false)
        {
            var billGiver = t as IBillGiver;

            if (billGiver == null || !ThingIsUsableBillGiver (t) || !billGiver.CurrentlyUsableForBills () || !billGiver.BillStack.AnyShouldDoNow || !pawn.CanReserve (t, 1) || t.IsBurning () || t.IsForbidden (pawn)) {
                return null;
            }
            if (!pawn.CanReach (t.InteractionCell, PathEndMode.OnCell, Danger.Some, false, TraverseMode.ByPawn)) {
                return null;
            }
            billGiver.BillStack.RemoveIncompletableBills ();
            return StartOrResumeBillJob (pawn, billGiver);
        }

        bool ThingIsUsableBillGiver (Thing thing)
        {
            if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Contains (thing.def)) {
                return true;
            }
            return false;
        }

        Job StartOrResumeBillJob (Pawn pawn, IBillGiver giver)
        {
            for (int i = 0; i < giver.BillStack.Count; i++) {
                Bill bill = giver.BillStack [i];

                // use MendAndRecycle.Worker as a filter so we can use the same tables.
                if (bill.recipe.workerClass != typeof (Worker)) {
                    continue;
                }

                if (Find.TickManager.TicksGame >= bill.lastIngredientSearchFailTicks + ReCheckFailedBillTicksRange.RandomInRange
                    || FloatMenuMakerMap.makingFor != pawn) {
                    if (bill.ShouldDoNow ()) {
                        if (bill.PawnAllowedToStartAnew (pawn)) {
                            if (!bill.recipe.PawnSatisfiesSkillRequirements (pawn)) {
                                JobFailReason.Is (ResourceBank.MissingSkill);
                            } else {
                                Thing chosen;
                                if (TryFindBestBillIngredients (bill, pawn, (Thing)giver, ignoreHitPoints, out chosen)) {
                                    return TryStartNewDoBillJob (pawn, bill, giver, chosen);
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        static bool TryFindBestBillIngredients (Bill bill, Pawn pawn, Thing billGiver, bool ignoreHitPoints, out Thing chosen)
        {
            var billGiverRootCell = GetBillGiverRootCell (billGiver, pawn);
            var validRegionAt = pawn.Map.regionGrid.GetValidRegionAt (billGiverRootCell);
            if (validRegionAt == null) {
                chosen = null;
                return false;
            }

            Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden (pawn)
                && (ignoreHitPoints || t.HitPoints < t.MaxHitPoints && t.HitPoints > 0)
                && bill.recipe.fixedIngredientFilter.Allows (t) && bill.ingredientFilter.Allows (t)
                && bill.recipe.ingredients.Any ((IngredientCount ingNeed) => ingNeed.filter.Allows (t))
                && pawn.CanReserve (t, 1)
                && (!bill.CheckIngredientsIfSociallyProper || t.IsSociallyProper (pawn));

            chosen = GenClosest.ClosestThingReachable (
                billGiverRootCell,
                pawn.Map,
                ThingRequest.ForGroup (ThingRequestGroup.HaulableEver),
                PathEndMode.Touch,
                TraverseParms.For (pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                bill.ingredientSearchRadius,
                new Predicate<Thing> (baseValidator));

            return chosen != null;
        }

        static IntVec3 GetBillGiverRootCell (Thing billGiver, Pawn forPawn)
        {
            var building = billGiver as Building;
            if (building == null) {
                return billGiver.Position;
            }
            if (building.def.hasInteractionCell) {
                return building.InteractionCell;
            }
            Log.Error ("MendAndRecycle :: Tried to find bill ingredients for " + billGiver + " which has no interaction cell.");
            return forPawn.Position;
        }

        Job TryStartNewDoBillJob (Pawn pawn, Bill bill, IBillGiver giver, Thing chosen)
        {
            var job = WorkGiverUtility.HaulStuffOffBillGiverJob (pawn, giver, null);
            if (job != null) {
                return job;
            }

            var job2 = new Job (jobDef, (Thing)giver, chosen);
            job2.count = 1;
            job2.haulMode = HaulMode.ToCellNonStorage;
            job2.bill = bill;
            return job2;
        }
    }
}
