using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace Mending
{
	public class JobDriver_Mend : JobDriver_DoBill
	{
		private const int fixedHitPointsPerCycle = 5;
		private const int fixedFailedDamage = 50;

		private float workCycle;
		private float workCycleProgress;
		private ChanceDef failChance;

		private FieldInfo compQualityInt = typeof(CompQuality).GetField ("qualityInt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		private FieldInfo ApparelWornByCorpseInt = typeof(Apparel).GetField("wornByCorpseInt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		protected override Toil DoBill()
		{
			Pawn actor = GetActor();
			Job curJob = actor.jobs.curJob;
			Thing objectThing = curJob.GetTarget(objectTI).Thing;
			Building_WorkTable tableThing = curJob.GetTarget(tableTI).Thing as Building_WorkTable;

			Toil toil = new Toil ();
			toil.initAction = delegate {
				curJob.bill.Notify_DoBillStarted ();

				this.failChance = ChanceDef.GetFor(objectThing);

				this.workCycleProgress = this.workCycle = Math.Max(curJob.bill.recipe.workAmount, 10f);
			};
			toil.tickAction = delegate {
				if (objectThing == null || objectThing.Destroyed) {
					actor.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				workCycleProgress -= StatExtension.GetStatValue (actor, StatDefOf.WorkToMake, true);

				if (!tableThing.UsableNow) {
					actor.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				if (workCycleProgress <= 0) {
					int remainingHitPoints = objectThing.MaxHitPoints - objectThing.HitPoints;
					if (remainingHitPoints > 0) {
						objectThing.HitPoints += (int) Math.Min(remainingHitPoints, fixedHitPointsPerCycle);
					}

					SkillRecord skill = actor.skills.GetSkill (SkillDefOf.Crafting);

					if (skill == null) {
						Log.Error("Mending :: This should never happen! skill == null");

						actor.jobs.EndCurrentJob (JobCondition.Incompletable);

						return;
					}

					float skillPerc = (float) skill.Level / 20f;

					skill.Learn (0.11f);

					CompQuality qualityComponent = objectThing.TryGetComp<CompQuality>();
					if (qualityComponent != null && qualityComponent.Quality > QualityCategory.Awful) {
						QualityCategory qc = qualityComponent.Quality;

						float skillFactor = Mathf.Lerp(1.5f, 0f, skillPerc);

						if (failChance != null && Rand.Value < failChance.Chance(qc) * skillFactor) {
							compQualityInt.SetValue(qualityComponent, qualityComponent.Quality - 1);

							objectThing.HitPoints -= fixedFailedDamage;

							MoteMaker.ThrowText(actor.DrawPos, actor.Map, "Failed");
						}
					}

					actor.GainComfortFromCellIfPossible ();

					if (objectThing.HitPoints <= 0) {
						// recycling whats left...
						float skillFactor = Mathf.Lerp(0.5f, 1.5f, skillPerc);

						var list = JobDriverUtils.Reclaim(objectThing, skillFactor * 0.1f);

						pawn.Map.reservationManager.Release(curJob.targetB, pawn);
						objectThing.Destroy(DestroyMode.Vanish);

						if (list.Count > 1) {
							for (int j = 1; j < list.Count; j++) {
								if (!GenPlace.TryPlaceThing (list [j], actor.Position, actor.Map, ThingPlaceMode.Near, null)) {
									Log.Error("Mending :: " + actor + " could not drop recipe product " + list [j] + " near " + actor.Position);
								}
							}
						}
						list[0].SetPositionDirect (actor.Position);

						curJob.targetB = list[0];
						curJob.bill.Notify_IterationCompleted (actor, list);

						pawn.Map.reservationManager.Reserve(pawn, curJob.targetB, 1);

						ReadyForNextToil();

					} else if (objectThing.HitPoints == objectThing.MaxHitPoints) {
						// fixed!

						Apparel mendApparel = objectThing as Apparel;
						if (mendApparel != null) {
							ApparelWornByCorpseInt.SetValue(mendApparel, false);
						}

						List<Thing> list = new List<Thing> ();
						list.Add(objectThing);
						curJob.bill.Notify_IterationCompleted (actor, list);

						ReadyForNextToil();

					} else if (objectThing.HitPoints > objectThing.MaxHitPoints) {
						Log.Error("Mending :: This should never happen! HitPoints > MaxHitPoints");
						actor.jobs.EndCurrentJob (JobCondition.Incompletable);
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
			toil.FailOn(() => {
				IBillGiver billGiver = curJob.GetTarget (tableTI).Thing as IBillGiver;

				return curJob.bill.suspended || curJob.bill.DeletedOrDereferenced || (billGiver != null && !billGiver.CurrentlyUsable ());
			});
			return toil;
		}


	}
}
