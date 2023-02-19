using Verse;
using HarmonyLib;
using Settings = RunGunAndDestroy.ModSettings_RunAndDestroy;

namespace RunGunAndDestroy
{
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedAccuracy))]
    static class Patch_AdjustedAccuracy
    {
        static float Postfix(float __result, Thing equipment)
        {
            if (equipment == null || equipment.holdingOwner == null || equipment.holdingOwner.Owner == null || equipment.holdingOwner.Owner is not Pawn_EquipmentTracker tracker || tracker.pawn == null)
            {
                return __result;
            }
            var curStance = tracker.pawn.stances?.curStance.GetType().Name;
            if (curStance == nameof(Stance_RunAndGun) || curStance == nameof(Stance_RunAndGun_Cooldown))
            {
                return __result * (tracker.pawn.IsColonist ? Settings.accuracyPenaltyPlayer : Settings.accuracyPenalty);
            }
            return __result;
        }
    }
}