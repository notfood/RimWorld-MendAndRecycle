using System;

using RimWorld;
using Verse;
using Verse.AI;

namespace Mending
{
	public abstract class WorkGiver_Scanner : RimWorld.WorkGiver_Scanner
	{
		private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange (500, 600);

		private static string MissingSkillTranslated;
		private static string MissingMaterialsTranslated;

		private bool ignoreHitPoints;
		private JobDef jobDef;

		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.Touch;
			}
		}

		public override ThingRequest PotentialWorkThingRequest {
			get {
				if (this.def.fixedBillGiverDefs != null && this.def.fixedBillGiverDefs.Count == 1) {
					return ThingRequest.ForDef (this.def.fixedBillGiverDefs [0]);
				}
				return ThingRequest.ForGroup (ThingRequestGroup.PotentialBillGiver);
			}
		}

		public WorkGiver_Scanner(JobDef job, bool ignoreHitPoints) {
			if (MissingSkillTranslated == null) {
				MissingSkillTranslated = "MissingSkill".Translate ();
			}
			if (MissingMaterialsTranslated == null) {
				MissingMaterialsTranslated = "MissingMaterials".Translate ();
			}
			this.jobDef = job;
			this.ignoreHitPoints = ignoreHitPoints;
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
			billGiver.BillStack.RemoveIncompletableBills ();
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

		private static bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, bool ignoreHitPoints, out Thing chosen) {
			IntVec3 billGiverRootCell = GetBillGiverRootCell (billGiver, pawn);
			Region validRegionAt = pawn.Map.regionGrid.GetValidRegionAt (billGiverRootCell);
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

			Job job2 = new Job (jobDef, (Thing) giver, chosen);
			job2.count = 1;
			job2.haulMode = HaulMode.ToCellNonStorage;
			job2.bill = bill;
			return job2;
		}
	}
}
