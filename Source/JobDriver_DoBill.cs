using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace Mending
{
    public abstract class JobDriver_DoBill : JobDriver
    {
        public const TargetIndex tableTI = TargetIndex.A;
        public const TargetIndex objectTI = TargetIndex.B;
        public const TargetIndex haulTI = TargetIndex.C;

        protected override IEnumerable<Toil> MakeNewToils ()
        {
            this.FailOnDestroyedNullOrForbidden (tableTI);
            this.FailOnBurningImmobile (objectTI);
            this.FailOnDestroyedNullOrForbidden (objectTI);
            this.FailOnBurningImmobile (objectTI);
            yield return Toils_Reserve.Reserve (tableTI, 1);
            yield return Toils_Reserve.Reserve (objectTI, 1);
            yield return Toils_Goto.GotoThing (objectTI, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing (objectTI);
            yield return Toils_Goto.GotoThing (tableTI, PathEndMode.InteractionCell);
            yield return Toils_Haul.PlaceHauledThingInCell (tableTI, null, false);
            yield return DoBill ();
            yield return Store ();
            yield return Toils_Reserve.Reserve (haulTI, 1);
            yield return Toils_Haul.CarryHauledThingToCell (haulTI);
            yield return Toils_Haul.PlaceHauledThingInCell (haulTI, null, false);
            yield return Toils_Reserve.Release (objectTI);
            yield return Toils_Reserve.Release (haulTI);
            yield return Toils_Reserve.Release (tableTI);

            yield break;
        }

        protected abstract Toil DoBill ();

        Toil Store ()
        {
            return new Toil () {
                initAction = delegate {
                    var objectThing = job.GetTarget (objectTI).Thing;

                    if (job.bill.GetStoreMode () != BillStoreModeDefOf.DropOnFloor) {
                        IntVec3 vec;
                        if (StoreUtility.TryFindBestBetterStoreCellFor (objectThing, pawn, pawn.Map, StoragePriority.Unstored, pawn.Faction, out vec, true)) {
                            pawn.carryTracker.TryStartCarry (objectThing, objectThing.stackCount);
                            job.SetTarget (haulTI, vec);
                            job.count = 99999;
                            return;
                        }
                    }
                    pawn.carryTracker.TryStartCarry (objectThing, objectThing.stackCount);
                    pawn.carryTracker.TryDropCarriedThing (pawn.Position, ThingPlaceMode.Near, out objectThing);

                    pawn.jobs.EndCurrentJob (JobCondition.Succeeded);
                }
            };
        }

        public override string GetReport ()
        {
            if (job.RecipeDef != null) {
                return ReportStringProcessed (job.RecipeDef.jobString);
            }
            return base.GetReport ();
        }

        public override bool TryMakePreToilReservations ()
        {
            pawn.ReserveAsManyAsPossible (job.GetTargetQueue (TargetIndex.B), job, 1);
            return pawn.Reserve (job.GetTarget (TargetIndex.A), job, 1);
        }
    }
}

