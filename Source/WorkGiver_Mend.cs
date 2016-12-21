using System;

namespace Mending
{
	public class WorkGiver_Mend : WorkGiver_Scanner
	{
		public WorkGiver_Mend () : base(LocalJobDefOf.Mend, false) {}
	}
}