using System;
using System.Collections.Generic;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace Mending
{
	public class JobDriver_Recycle : JobDriver_DoBill
	{
		private const int fixedHitPointsPerCycle = 5;
		private const int fixedFailedDamage = 50;

		private int processedHitPoints;
		private float workCycle;
		private float workCycleProgress;
		private ChanceDef failChance;

		protected override Toil DoBill()
		{
			Pawn actor = GetActor();
			SkillRecord skill = actor.skills.GetSkill (SkillDefOf.Crafting);
			Job curJob = actor.jobs.curJob;
			Thing objectThing = curJob.GetTarget(objectTI).Thing;
			CompQuality qualityComponent = objectThing.TryGetComp<CompQuality>();
			Building_WorkTable tableThing = curJob.GetTarget(tableTI).Thing as Building_WorkTable;

			Toil toil = new Toil ();
			toil.initAction = delegate {
				curJob.bill.Notify_DoBillStarted ();

				this.processedHitPoints = 0;
				this.failChance = ChanceDef.GetFor(objectThing);

				this.workCycleProgress = this.workCycle = Math.Max(curJob.bill.recipe.workAmount, 10f);
			};
			toil.tickAction = delegate {
				if (objectThing == null || objectThing.Destroyed) {
					actor.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				workCycleProgress -= StatExtension.GetStatValue (actor, StatDefOf.WorkToMake, true);
				tableThing.Tick();
				if (!tableThing.UsableNow) {
					actor.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				if (workCycleProgress <= 0) {
					objectThing.HitPoints -= fixedHitPointsPerCycle;
					processedHitPoints += fixedHitPointsPerCycle;

					if (skill != null) {
						skill.Learn (0.11f);

						if (qualityComponent != null && qualityComponent.Quality > QualityCategory.Awful) {
							QualityCategory qc = qualityComponent.Quality;

							if (failChance != null && Rand.Value < failChance.Chance(qc, skill.Level)) {
								objectThing.HitPoints -= fixedFailedDamage;

								MoteMaker.ThrowText(actor.DrawPos, actor.Map, "Failed");
							}
						}
					}

					actor.GainComfortFromCellIfPossible ();

					if (objectThing.HitPoints <= 0) {
						pawn.Map.reservationManager.Release(curJob.targetB, pawn);
						objectThing.Destroy(DestroyMode.Vanish);

						float skillPerc = (float) skill.Level / 20f;
						float skillFactor = Mathf.Lerp(0.5f, 1.5f, skillPerc);
						float healthPerc = (float) processedHitPoints / (float) objectThing.MaxHitPoints;
						float healthFactor = Mathf.Lerp(0f, 0.4f, healthPerc);

						var list = JobDriverUtils.Reclaim(objectThing, skillFactor * healthFactor);

						if (list.Count > 1) {
							for (int j = 1; j < list.Count; j++) {
								if (!GenPlace.TryPlaceThing (list [j], actor.Position, actor.Map, ThingPlaceMode.Near, null)) {
									Log.Error("Mending :: " + actor + " could not drop recipe product " + list [j] + " near " + actor.Position);
								}
							}
						}

						list[0].SetPositionDirect (actor.Position);

						curJob.bill.Notify_IterationCompleted (actor, list);
						curJob.targetB = list[0];

						pawn.Map.reservationManager.Reserve(pawn, curJob.targetB, 1);

						ReadyForNextToil();
					}

					workCycleProgress = workCycle;
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.WithEffect (() => curJob.bill.recipe.effectWorking, tableTI);
			toil.PlaySustainerOrSound (() => toil.actor.CurJob.bill.recipe.soundWorking);
			toil.WithProgressBar(tableTI, delegate {
				return (float)objectThing.HitPoints / (float)objectThing.MaxHitPoints;
			}, false, 0.5f);
			toil.FailOn (() => {
				return toil.actor.CurJob.bill.suspended || !tableThing.UsableNow;
			});
			return toil;
		}

	}
}

