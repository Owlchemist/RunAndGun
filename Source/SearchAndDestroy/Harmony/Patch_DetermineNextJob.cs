using HarmonyLib;
using Verse;
using Verse.AI;

namespace SumGunFun
{
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
	static class Patch_DetermineNextJob
	{
		static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
		{
			Pawn pawn = __instance.pawn;
			
			if (pawn.Drafted && pawn.SearchesAndDestroys() && __instance.jobQueue.Count > 0)
			{
				QueuedJob qjob = __instance.jobQueue[__instance.jobQueue.Count - 1];
				__instance.ClearQueuedJobs(false);
				__result = new ThinkResult(qjob.job, __result.SourceNode);
			}
		}
	}
}