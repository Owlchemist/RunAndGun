using HarmonyLib;
using System.Linq;
using Verse;
using Verse.AI;

namespace RunGunAndDestroy
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
    static class Patch_DetermineNextJob
    {
        static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
        {
            Pawn pawn = __instance.pawn;
            
            if (pawn.Drafted)
            {
                if(pawn.SearchesAndDestroys() && __instance.jobQueue.Count > 0)
                {
                    QueuedJob qjob = __instance.jobQueue.Last();  
                    __instance.ClearQueuedJobs(false);
                    __result = new ThinkResult(qjob.job, __result.SourceNode, null, false);
                }
            }
        }
    }
}