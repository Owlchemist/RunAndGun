using Verse;
using HarmonyLib;
using Settings = RunAndDestroy.ModSettings_RunAndDestroy;

namespace RunAndDestroy
{
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedAccuracy))]
    static class Patch_AdjustedAccuracy
    {
        static float Postfix(float __result, VerbProperties __instance, ref Thing equipment)
        {
            if (equipment == null || equipment.holdingOwner == null || equipment.holdingOwner.Owner == null || equipment.holdingOwner.Owner is not Pawn_EquipmentTracker eqt || eqt.pawn == null)
            {
                return __result;
            }
            var curStance = eqt.pawn.stances?.curStance.GetType().Name;
            
            if (curStance == nameof(Stance_RunAndGun) || curStance == nameof(Stance_RunAndGun_Cooldown))
            {
                return __result * ((float)(100 - Settings.accuracyPenalty) / 100);
            }
            return __result;
        }
    }
}