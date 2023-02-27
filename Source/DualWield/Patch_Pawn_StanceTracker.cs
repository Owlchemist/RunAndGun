using HarmonyLib;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
	[HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.FullBodyBusy), MethodType.Getter)]
	class Patch_Pawn_StanceTracker_FullBodyBusy
	{
		static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
		static bool Postfix(bool __result, Pawn_StanceTracker __instance, Pawn ___pawn)
		{
			if (__result || ___pawn.RaceProps.intelligence == Intelligence.Animal || !___pawn.HasOffHand()) return __result;
			else
			{
				var stancesOffHand = ___pawn.GetOffHandStance();
				if (stancesOffHand is Stance_Cooldown && !___pawn.RunsAndGuns()) return stancesOffHand.StanceBusy;
				else return false;
			}
		}
	}
}