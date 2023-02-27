using Verse;
using HarmonyLib;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
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
            var curStance = tracker.pawn.stances?.curStance;
            if (curStance is Stance_RunAndGun || curStance is Stance_RunAndGun_Cooldown)
            {
                return __result * (tracker.pawn.IsColonist ? Settings.accuracyModifierPlayer : Settings.accuracyModifier);
            }
            return __result;
        }
    }
}