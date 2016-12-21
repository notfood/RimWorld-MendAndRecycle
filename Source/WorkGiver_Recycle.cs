using System;

namespace Mending
{
	public class WorkGiver_Recycle : WorkGiver_Scanner
	{
		public WorkGiver_Recycle() : base(LocalJobDefOf.Recycle, true) {}
	}
}