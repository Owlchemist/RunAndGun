using HarmonyLib;
using Verse;

namespace RunGunAndDestroy.DualWield
{
	[HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.FullBodyBusy), MethodType.Getter)]
	class Patch_Pawn_StanceTracker_FullBodyBusy
	{
		static bool Postfix(bool __result, Pawn_StanceTracker __instance)
		{
			if (__result) return __result;
			else
			{
				Pawn pawn = __instance.pawn;
				var stancesOffHand = pawn.GetStancesOffHand();
				if (stancesOffHand is Stance_Cooldown && !pawn.RunsAndGuns()) return stancesOffHand.StanceBusy;
				else return false;
			}
		}
	}
}