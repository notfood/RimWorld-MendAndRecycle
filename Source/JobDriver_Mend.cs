using System;
using System.Collections.Generic;
using System.Reflection;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Mending
{
	public class JobDriver_Mend : JobDriver_DoBill
	{
		const int fixedHitPointsPerCycle = 5;
		const int fixedFailedDamage = 50;

        readonly FieldInfo ApparelWornByCorpseInt = typeof (Apparel).GetField ("wornByCorpseInt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		float workCycle;
		float workCycleProgress;

        protected override Toil DoBill()
		{
			var objectThing = job.GetTarget(objectTI).Thing;
			var tableThing = job.GetTarget(tableTI).Thing as Building_WorkTable;

			var toil = new Toil ();
			toil.initAction = delegate {
				job.bill.Notify_DoBillStarted (pawn);

				workCycleProgress = workCycle = Math.Max(job.bill.recipe.workAmount, 10f);
			};
			toil.tickAction = delegate {
				if (objectThing == null || objectThing.Destroyed) {
					pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				workCycleProgress -= StatExtension.GetStatValue (pawn, StatDefOf.WorkToMake, true);

				tableThing.UsedThisTick ();
				if (!tableThing.CurrentlyUsableForBills()) {
					pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				if (workCycleProgress <= 0) {
					int remainingHitPoints = objectThing.MaxHitPoints - objectThing.HitPoints;
					if (remainingHitPoints > 0) {
						objectThing.HitPoints += (int) Math.Min(remainingHitPoints, fixedHitPointsPerCycle);
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

					var qualityComponent = objectThing.TryGetComp<CompQuality>();
					if (qualityComponent != null && qualityComponent.Quality > QualityCategory.Awful) {
						var qc = qualityComponent.Quality;

						float skillFactor = Mathf.Lerp(1.5f, 0f, skillPerc);

						if (!SuccessChanceUtil.SuccessOnAction(pawn, skillFactor, objectThing)) {
							objectThing.HitPoints -= fixedFailedDamage;

							MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed");
						}
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
									Log.Error("Mending :: " + pawn + " could not drop recipe product " + list [j] + " near " + pawn.Position);
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

						var mendApparel = objectThing as Apparel;
						if (mendApparel != null) {
							ApparelWornByCorpseInt.SetValue(mendApparel, false);
						}

						var list = new List<Thing> ();
						list.Add(objectThing);
						job.bill.Notify_IterationCompleted (pawn, list);

						ReadyForNextToil();

					} else if (objectThing.HitPoints > objectThing.MaxHitPoints) {
						Log.Error("Mending :: This should never happen! HitPoints > MaxHitPoints");
						pawn.jobs.EndCurrentJob (JobCondition.Incompletable);
					}

					workCycleProgress = workCycle;
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.WithEffect (() => job.bill.recipe.effectWorking, tableTI);
			toil.PlaySustainerOrSound (() => toil.actor.CurJob.bill.recipe.soundWorking);
			toil.WithProgressBar(tableTI, delegate {
				return (float)objectThing.HitPoints / (float)objectThing.MaxHitPoints;
			}, false, 0.5f);
			toil.FailOn(() => {
				var billGiver = job.GetTarget (tableTI).Thing as IBillGiver;

				return job.bill.suspended || job.bill.DeletedOrDereferenced || (billGiver != null && !billGiver.CurrentlyUsableForBills ());
			});
			return toil;
		}


	}
}
