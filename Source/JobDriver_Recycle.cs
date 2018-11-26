using System;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MendAndRecycle
{
    public class JobDriver_Recycle : JobDriver_DoBill
    {
        int costHitPointsPerCycle;
        int processedHitPoints;
        float workCycle;
        float workCycleProgress;

        protected override Toil DoBill ()
        {
            var tableThing = job.GetTarget (BillGiverInd).Thing as Building_WorkTable;
            var tablePowerTraderComp = tableThing.GetComp<CompPowerTrader> ();

            var toil = new Toil ();
            toil.initAction = delegate {
                var objectThing = job.GetTarget(IngredientInd).Thing;

                job.bill.Notify_DoBillStarted (pawn);

                costHitPointsPerCycle = (int) (objectThing.MaxHitPoints * Settings.costFromMaxHitPoints);
                processedHitPoints = 0;

                workCycleProgress = workCycle = Math.Max (job.bill.recipe.workAmount, 10f);
            };
            toil.tickAction = delegate {
                var objectThing = job.GetTarget(IngredientInd).Thing;

                if (objectThing == null || objectThing.Destroyed) {
                    pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
                }

                workCycleProgress -= StatExtension.GetStatValue (pawn, StatDefOf.WorkToMake, true);

                tableThing.UsedThisTick ();
                if (!tableThing.CurrentlyUsableForBills()) {
                    pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
                }

                if (workCycleProgress <= 0) {
                    objectThing.HitPoints -= costHitPointsPerCycle;

                    if (tablePowerTraderComp != null && tablePowerTraderComp.PowerOn) {
                        processedHitPoints += costHitPointsPerCycle;
                    } else {
                        processedHitPoints += costHitPointsPerCycle / 2;
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
                        pawn.Map.reservationManager.Release (job.targetB, pawn, job);
                        objectThing.Destroy (DestroyMode.Vanish);

                        float skillFactor = Mathf.Lerp (0.5f, 1.5f, skillPerc);
                        float healthPerc = (float)processedHitPoints / (float)objectThing.MaxHitPoints;
                        float healthFactor = Mathf.Lerp (0f, 0.4f, healthPerc);

                        var list = JobDriverUtils.Reclaim (objectThing, skillFactor * healthFactor);

                        if (list.Count > 1) {
                            for (int j = 1; j < list.Count; j++) {
                                if (!GenPlace.TryPlaceThing (list [j], pawn.Position, pawn.Map, ThingPlaceMode.Near, null)) {
                                    Log.Error ("MendAndRecycle :: " + pawn + " could not drop recipe product " + list [j] + " near " + pawn.Position);
                                }
                            }
                        } else if (list.Count == 1) {
                            list [0].SetPositionDirect (pawn.Position);

                            job.bill.Notify_IterationCompleted (pawn, list);
                            job.targetB = list [0];

                            pawn.Map.reservationManager.Reserve (pawn, job, job.targetB, 1);
                        } else {
                            Log.Message ("MendAndRecycle :: " + pawn + " could not reclaim anything from " + objectThing);
                        }

                        ReadyForNextToil ();
                    }

                    workCycleProgress = workCycle;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect (() => job.bill.recipe.effectWorking, BillGiverInd);
            toil.PlaySustainerOrSound (() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar (BillGiverInd, delegate {
                var objectThing = job.GetTarget(IngredientInd).Thing;

                return (float)objectThing.HitPoints / (float)objectThing.MaxHitPoints;
            }, false, 0.5f);
            toil.FailOn (() => {
                return toil.actor.CurJob.bill.suspended || !tableThing.CurrentlyUsableForBills();
            });
            return toil;
        }

    }
}

