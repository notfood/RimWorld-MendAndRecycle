using Verse;

namespace MendAndRecycle
{
    public abstract class RecipeWorkerWithJob : RecipeWorker
    {
        public abstract JobDef Job { get; }
    }

    public class RecipeWorkerWithJob_Mend : RecipeWorkerWithJob 
    {
        public override JobDef Job => ResourceBank.Job.Mend;
    }

    public class RecipeWorkerWithJob_Recycle : RecipeWorkerWithJob
    {
        public override JobDef Job => ResourceBank.Job.Recycle;
    }
}

