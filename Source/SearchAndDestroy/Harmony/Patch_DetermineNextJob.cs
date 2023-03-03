using HarmonyLib;
using Verse;
using Verse.AI;

namespace Tacticowl
{
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
	class Patch_DetermineNextJob
	{
		static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
		{
			Pawn pawn = __instance.pawn;
			
			if (pawn.Drafted && __instance.jobQueue.Count > 0 && pawn.SearchesAndDestroys())
			{
				QueuedJob qjob = __instance.jobQueue[__instance.jobQueue.Count - 1];
				__instance.ClearQueuedJobs(false);
				__result = new ThinkResult(qjob.job, __result.SourceNode);
			}
		}
	}
}