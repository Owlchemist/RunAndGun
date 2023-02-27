using HarmonyLib;
using Verse;
using Verse.AI;

namespace Tacticowl
{
    [HarmonyPatch(typeof(JobGiver_Orders), nameof(JobGiver_Orders.TryGiveJob))]
    static class Patch_TryGiveJob
    {
        static void Postfix(Pawn pawn, Job __result)
        {
            if (__result != null && pawn.SearchesAndDestroys())
            {
                __result.expiryInterval = 60;
            }
        }
    }
}