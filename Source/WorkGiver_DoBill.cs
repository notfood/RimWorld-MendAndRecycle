using RimWorld;
using Verse;
using Verse.AI;

namespace MendAndRecycle
{
    public class WorkGiver_DoBill : RimWorld.WorkGiver_DoBill
    {
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            var job = base.JobOnThing(pawn, thing, forced);

            if (job != null && job.def == JobDefOf.DoBill && job.RecipeDef.Worker is RecipeWorkerWithJob worker)
            {
                return new Job(worker.Job, job.targetA)
                {
                    targetQueueB = job.targetQueueB,
                    countQueue = job.countQueue,
                    haulMode = job.haulMode,
                    bill = job.bill
                };
            }

            return job;
        }
    }
}
