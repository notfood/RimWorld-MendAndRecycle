using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
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
			this.FailOnDestroyedNullOrForbidden(tableTI);
			this.FailOnBurningImmobile(objectTI);
			this.FailOnDestroyedNullOrForbidden(objectTI);
			this.FailOnBurningImmobile(objectTI);
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

		private Toil Store() {
			Toil toil = new Toil ();
			toil.initAction = delegate {
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				Thing objectThing = curJob.GetTarget(objectTI).Thing;

				if (curJob.bill.GetStoreMode () != BillStoreModeDefOf.DropOnFloor) {
					IntVec3 vec;
					if (StoreUtility.TryFindBestBetterStoreCellFor (objectThing, actor, actor.Map, StoragePriority.Unstored, actor.Faction, out vec, true)) {
						actor.carryTracker.TryStartCarry (objectThing, objectThing.stackCount);
						curJob.SetTarget(haulTI, vec);
						curJob.count = 99999;
						return;
					}
				}
				actor.carryTracker.TryStartCarry (objectThing, objectThing.stackCount);
				actor.carryTracker.TryDropCarriedThing(actor.Position, ThingPlaceMode.Near, out objectThing);

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
	}
}

