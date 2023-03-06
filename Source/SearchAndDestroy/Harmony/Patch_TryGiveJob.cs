using HarmonyLib;
using Verse;
using Verse.AI;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
    [HarmonyPatch(typeof(JobGiver_Orders), nameof(JobGiver_Orders.TryGiveJob))]
    class Patch_TryGiveJob
    {
        static bool Prepare()
		{
			return Settings.searchAndDestroyEnabled;
		}
        static void Postfix(Pawn pawn, Job __result)
        {
            if (__result != null && pawn.SearchesAndDestroys())
            {
                __result.expiryInterval = 60;
            }
        }
    }
}