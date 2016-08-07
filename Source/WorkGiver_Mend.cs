using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Mending
{
	public class WorkGiver_Mend : WorkGiver_DoBill
	{
		private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange (500, 600);

		private static string MissingSkillTranslated;
		private static string MissingMaterialsTranslated;

		[DefOf]
		public static class LocalJobDefs {
			public static JobDef Mending;
		}

		public WorkGiver_Mend() {
			if (MissingSkillTranslated == null) {
				MissingSkillTranslated = "MissingSkill".Translate ();
			}
			if (MissingMaterialsTranslated == null) {
				MissingMaterialsTranslated = "MissingMaterials".Translate ();
			}
		}

		public override Job JobOnThing (Pawn pawn, Thing thing)
		{
			IBillGiver billGiver = thing as IBillGiver;

			if (billGiver == null || !this.ThingIsUsableBillGiver (thing) || !billGiver.CurrentlyUsable () || !billGiver.BillStack.AnyShouldDoNow || !pawn.CanReserve (thing, 1) || thing.IsBurning () || thing.IsForbidden (pawn)) {
				return null;
			}
			if (!pawn.CanReach (thing.InteractionCell, PathEndMode.OnCell, Danger.Some, false, TraverseMode.ByPawn)) {
				return null;
			}
			billGiver.BillStack.RemoveInvalidBills ();
			return this.StartOrResumeBillJob (pawn, billGiver);
		}

		private bool ThingIsUsableBillGiver(Thing thing) {
			if (this.def.fixedBillGiverDefs != null && this.def.fixedBillGiverDefs.Contains (thing.def)) {
				return true;
			}
			return false;
		}

		private Job StartOrResumeBillJob (Pawn pawn, IBillGiver giver) {
			for (int i = 0; i < giver.BillStack.Count; i++) {
				Bill bill = giver.BillStack [i];

				if (Find.TickManager.TicksGame >= bill.lastIngredientSearchFailTicks + ReCheckFailedBillTicksRange.RandomInRange
				    || FloatMenuMakerMap.making) {
					if (bill.ShouldDoNow ()) {
						if (bill.PawnAllowedToStartAnew (pawn)) {
							if (!bill.recipe.PawnSatisfiesSkillRequirements (pawn)) {
								JobFailReason.Is (MissingSkillTranslated);
							} else {
								Thing chosen;
								if (TryFindBestBillIngredients (bill, pawn, (Thing)giver, out chosen)) {
									return TryStartNewDoBillJob (pawn, bill, giver, chosen);
								}
							}
						}
					}
				}
			}

			return null;
		}

		private static bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, out Thing chosen) {
			IntVec3 billGiverRootCell = GetBillGiverRootCell (billGiver, pawn);
			Region validRegionAt = Find.RegionGrid.GetValidRegionAt (billGiverRootCell);
			if (validRegionAt == null) {
				chosen = null;
				return false;
			}

			FilterRoofedBuildingComp menderBuildingComp = billGiver.TryGetComp<FilterRoofedBuildingComp>();

			Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden (pawn)
				&& t.HitPoints < t.MaxHitPoints && t.HitPoints > 0
				&& (menderBuildingComp == null || !menderBuildingComp.restrictInside || Find.RoofGrid.Roofed (t.Position))
				&& bill.recipe.fixedIngredientFilter.Allows (t) && bill.ingredientFilter.Allows (t)
				&& bill.recipe.ingredients.Any ((IngredientCount ingNeed) => ingNeed.filter.Allows (t))
				&& pawn.CanReserve (t, 1)
				&& (!bill.CheckIngredientsIfSociallyProper || t.IsSociallyProper (pawn));

			chosen = GenClosest.ClosestThingReachable (
				billGiverRootCell,
				ThingRequest.ForGroup (ThingRequestGroup.HaulableEver),
				PathEndMode.Touch,
				TraverseParms.For (pawn, Danger.Deadly, TraverseMode.ByPawn, false),
				bill.ingredientSearchRadius,
				new Predicate<Thing> (baseValidator),
				null, -1, false);

			return chosen != null;
		}

		private static IntVec3 GetBillGiverRootCell (Thing billGiver, Pawn forPawn)
		{
			Building building = billGiver as Building;
			if (building == null) {
				return billGiver.Position;
			}
			if (building.def.hasInteractionCell) {
				return building.InteractionCell;
			}
			Log.Error ("Mending :: Tried to find bill ingredients for " + billGiver + " which has no interaction cell.");
			return forPawn.Position;
		}

		private Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver, Thing chosen) {
			Job job = WorkGiverUtility.HaulStuffOffBillGiverJob (pawn, giver, null);
			if (job != null) {
				return job;
			}

			Job job2 = new Job (LocalJobDefs.Mending, (Thing) giver, chosen);
			job2.maxNumToCarry = 1;
			job2.haulMode = HaulMode.ToCellNonStorage;
			job2.bill = bill;
			return job2;
		}
	}
}
