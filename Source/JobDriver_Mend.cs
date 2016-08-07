using System;
using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace Mending
{
	public class JobDriver_Mend : JobDriver
	{
		public const TargetIndex mendTableTI = TargetIndex.A;
		public const TargetIndex mendObjectTI = TargetIndex.B;
		public const TargetIndex mendHaulTI = TargetIndex.C;

		private const int fixedHitPointsPerCycle = 5;

		private float workCycle;
		private float workCycleProgress;

		protected override IEnumerable<Toil> MakeNewToils ()
		{
			ToilFailConditions.FailOnDestroyedNullOrForbidden<JobDriver_Mend> (this, mendTableTI);
			ToilFailConditions.FailOnBurningImmobile<JobDriver_Mend> (this, mendObjectTI);
			ToilFailConditions.FailOnDestroyedNullOrForbidden<JobDriver_Mend> (this, mendObjectTI);
			ToilFailConditions.FailOnBurningImmobile<JobDriver_Mend> (this, mendObjectTI);
			ToilFailConditions.FailOn<JobDriver_Mend> (this, delegate {
				Job curJob = CurJob;
				IBillGiver billGiver = curJob.GetTarget (mendTableTI).Thing as IBillGiver;
				Thing mendObject = curJob.GetTarget(mendObjectTI).Thing;
				if (billGiver != null) {
					if (curJob.bill.DeletedOrDereferenced) {
						return true;
					}
					if (!billGiver.CurrentlyUsable ()) {
						return true;
					}
				}
				return false;
			});


			yield return Toils_Reserve.Reserve (mendTableTI, 1);
			yield return Toils_Reserve.Reserve (mendObjectTI, 1);
			yield return Toils_Goto.GotoThing (mendObjectTI, PathEndMode.Touch);
			yield return Toils_Haul.StartCarryThing (mendObjectTI);
			yield return Toils_Goto.GotoThing (mendTableTI, PathEndMode.InteractionCell);
			yield return Toils_Haul.PlaceHauledThingInCell (mendTableTI, null, false);
			yield return Mend ();
			yield return Store ();
			yield return Toils_Reserve.Reserve (mendHaulTI, 1);
			yield return Toils_Haul.CarryHauledThingToCell (mendHaulTI);
			yield return Toils_Haul.PlaceHauledThingInCell (mendHaulTI, null, false);
			yield return Toils_Reserve.Release (mendObjectTI);
			yield return Toils_Reserve.Release (mendHaulTI);
			yield return Toils_Reserve.Release (mendTableTI);

			yield break;
		}

		private Toil Mend ()
		{
			Pawn actor = GetActor();
			Job curJob = actor.jobs.curJob;
			Thing mendObject = curJob.GetTarget(mendObjectTI).Thing;
			Building_WorkTable mendTable = curJob.GetTarget(mendTableTI).Thing as Building_WorkTable;

			Toil toil = new Toil ();
			toil.initAction = delegate {
				curJob.bill.Notify_DoBillStarted ();
				this.workCycleProgress = this.workCycle = Math.Max(curJob.bill.recipe.workAmount, 10f);
			};
			toil.tickAction = delegate {
				if (mendObject == null || mendObject.Destroyed) {
					actor.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				workCycleProgress -= StatExtension.GetStatValue (actor, StatDefOf.WorkToMake, true);
				mendTable.BillTick();
				if (!mendTable.UsableNow) {
					actor.jobs.EndCurrentJob (JobCondition.Incompletable);
				}

				if (workCycleProgress <= 0) {
					int remainingHitPoints = mendObject.MaxHitPoints - mendObject.HitPoints;
					if (remainingHitPoints > 0) {
						mendObject.HitPoints += (int) Math.Min(remainingHitPoints, fixedHitPointsPerCycle);
					}

					SkillRecord skill = actor.skills.GetSkill (SkillDefOf.Crafting);
					if (skill != null) {
						skill.Learn (skill.LearningFactor);
					}

					if (mendObject.HitPoints == mendObject.MaxHitPoints) {
						List<Thing> list = new List<Thing> ();
						list.Add(mendObject);
						curJob.bill.Notify_IterationCompleted (actor, list);

						ReadyForNextToil();

					} else if (mendObject.HitPoints > mendObject.MaxHitPoints) {
						Log.Error("Mending :: This should never happen! HitPoints > MaxHitPoints");
						actor.jobs.EndCurrentJob (JobCondition.Incompletable);
					}

					workCycleProgress = workCycle;
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.WithEffect (() => curJob.bill.recipe.effectWorking, mendTableTI);
			toil.WithSustainer (() => toil.actor.CurJob.bill.recipe.soundWorking);
			toil.WithProgressBar(mendTableTI, delegate {
				return (float)mendObject.HitPoints / (float)mendObject.MaxHitPoints;
			}, false, 0.5f);
			toil.FailOn (() => toil.actor.CurJob.bill.suspended);
			return toil;
		}

		private Toil Store() {
			Toil toil = new Toil ();
			toil.initAction = delegate {
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				Thing mendObject = curJob.GetTarget(mendObjectTI).Thing;

				if (curJob.bill.GetStoreMode () != BillStoreMode.DropOnFloor) {
					IntVec3 vec;
					if (StoreUtility.TryFindBestBetterStoreCellFor (mendObject, actor, StoragePriority.Unstored, actor.Faction, out vec, true)) {
						actor.carrier.TryStartCarry (mendObject);
						curJob.SetTarget(mendHaulTI, vec);
						curJob.maxNumToCarry = 99999;
						return;
					}
				}
					
				actor.carrier.TryStartCarry (mendObject);
				actor.carrier.TryDropCarriedThing(actor.Position, ThingPlaceMode.Near, out mendObject);

				actor.jobs.EndCurrentJob (JobCondition.Succeeded);
			};
			return toil;
		}

		public override string GetReport ()
		{
			if (this.pawn.jobs.curJob.RecipeDef != null) {
				return base.ReportStringProcessed (this.pawn.jobs.curJob.RecipeDef.jobString);
			}
			return base.GetReport ();
		}

		private static Toil Finish() {
			Toil toil = new Toil ();
			toil.initAction = delegate {
				toil.actor.jobs.EndCurrentJob (JobCondition.Succeeded);
			};
			return toil;
		}
	}
}
