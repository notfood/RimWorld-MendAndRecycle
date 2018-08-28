namespace Mending
{
    public class WorkGiver_Mend : WorkGiver_DoBill
    {
        public WorkGiver_Mend () : base (LocalDefOf.Job.Mend, false) { }
    }
}