using System; 
using System.Collections.Generic;
using System.Reflection;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MendAndRecycle
{
    public class JobDriver_Mend : JobDriver_DoBill
    {
        readonly FieldInfo ApparelWornByCorpseInt = typeof(Apparel).GetField("wornByCorpseInt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        int costHitPointsPerCycle;
        float workCycle;
        float workCycleProgress;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref costHitPointsPerCycle, "costHitPointsPerCycle", 1);
            Scribe_Values.Look(ref workCycle, "workCycle", 1f);
            Scribe_Values.Look(ref workCycleProgress, "workCycleProgress", 1f);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override Toil DoBill()
        {
            var tableThing = job.GetTarget(BillGiverInd).Thing as Building_WorkTable;
            var refuelableComp = tableThing.GetComp<CompRefuelable>();

            var toil = new Toil ();
            toil.initAction = delegate {
                var objectThing = job.GetTarget(IngredientInd).Thing;

                job.bill.Notify_DoBillStarted(pawn);

                costHitPointsPerCycle = (int)(objectThing.MaxHitPoints * Settings.costFromMaxHitPoints);

                workCycleProgress = workCycle = Math.Max(job.bill.recipe.workAmount, 10f);

                Log.Message(" > workCycleProgress=" + workCycleProgress + " costHitPointsPerCycle" + costHitPointsPerCycle);
            };
            toil.tickAction = delegate {
                var objectThing = job.GetTarget(IngredientInd).Thing;

                if (objectThing == null || objectThing.Destroyed) {
                    pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
                }

                workCycleProgress -= StatExtension.GetStatValue (pawn, StatDefOf.WorkToMake, true);

                tableThing.UsedThisTick ();

                if (! (tableThing.CurrentlyUsableForBills() && (refuelableComp == null || refuelableComp.HasFuel)) ) {
                    pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
                }

                if (workCycleProgress <= 0) {
                    int remainingHitPoints = objectThing.MaxHitPoints - objectThing.HitPoints;
                    if (remainingHitPoints > 0) {
                        objectThing.HitPoints += (int) Math.Min(remainingHitPoints, costHitPointsPerCycle);
                    }

                    float skillPerc = 0.5f;

                    var skillDef = job.RecipeDef.workSkill;
                    if (skillDef != null) {
                        var skill = pawn.skills.GetSkill (skillDef);

                        if (skill != null) {
                            skillPerc = (float)skill.Level / 20f;

                            skill.Learn (0.11f * job.RecipeDef.workSkillLearnFactor);
                        }
                    }

                    if (Settings.chances[objectThing.def.techLevel] > 1 - Mathf.Pow(Rand.Value, 1 + skillPerc * 3f))
                    {
                        objectThing.HitPoints -= Rand.RangeInclusive(costHitPointsPerCycle, costHitPointsPerCycle * 4);

                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed");
                    }

                    pawn.GainComfortFromCellIfPossible ();

                    if (objectThing.HitPoints <= 0) {
                        // recycling whats left...
                        float skillFactor = Mathf.Lerp(0.5f, 1.5f, skillPerc);

                        var list = JobDriverUtils.Reclaim(objectThing, skillFactor * 0.1f);

                        pawn.Map.reservationManager.Release(job.targetB, pawn, job);
                        objectThing.Destroy(DestroyMode.Vanish);

                        if (list.Count > 1) {
                            for (int j = 1; j < list.Count; j++) {
                                if (!GenPlace.TryPlaceThing (list [j], pawn.Position, pawn.Map, ThingPlaceMode.Near, null)) {
                                    Log.Error("MendAndRecycle :: " + pawn + " could not drop recipe product " + list [j] + " near " + pawn.Position);
                                }
                            }
                        }
                        list[0].SetPositionDirect (pawn.Position);

                        job.targetB = list[0];
                        job.bill.Notify_IterationCompleted (pawn, list);

                        pawn.Map.reservationManager.Reserve(pawn, job, job.targetB, 1);

                        ReadyForNextToil();

                    } else if (objectThing.HitPoints == objectThing.MaxHitPoints) {
                        // fixed!

                        if (Settings.removesDeadman && objectThing is Apparel mendApparel) {
                            ApparelWornByCorpseInt.SetValue(mendApparel, false);
                        }

                        var list = new List<Thing> ();
                        list.Add(objectThing);
                        job.bill.Notify_IterationCompleted (pawn, list);

                        ReadyForNextToil();

                    } else if (objectThing.HitPoints > objectThing.MaxHitPoints) {
                        Log.Error("MendAndRecycle :: This should never happen! HitPoints > MaxHitPoints");
                        pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
                    }

                    workCycleProgress = workCycle;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect (() => job.bill.recipe.effectWorking, BillGiverInd);
            toil.PlaySustainerOrSound (() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar(BillGiverInd, () => {
                var objectThing = job.GetTarget(IngredientInd).Thing;
                return (float)objectThing.HitPoints / (float)objectThing.MaxHitPoints;
            }, false, 0.5f);
            toil.FailOn(() => {
                var billGiver = job.GetTarget (BillGiverInd).Thing as IBillGiver;

                return job.bill.suspended || job.bill.DeletedOrDereferenced || (billGiver != null && !billGiver.CurrentlyUsableForBills ());
            });
            return toil;
        }
    }
}
