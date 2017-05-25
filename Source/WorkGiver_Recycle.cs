using System;

namespace Mending
{
	public class WorkGiver_Recycle : WorkGiver_DoBill
	{
		public WorkGiver_Recycle() : base(LocalJobDefOf.Recycle, true) {}
	}
}