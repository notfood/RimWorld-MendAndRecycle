using System;

using Verse;

namespace Mending
{
	public class SpecialThingFilterWorker_Unroofed : SpecialThingFilterWorker
	{
		public override bool Matches (Thing t) {
			return !t.Map.roofGrid.Roofed (t.Position);
		}
	}
}