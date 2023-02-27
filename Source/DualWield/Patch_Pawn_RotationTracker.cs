using HarmonyLib;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
    class Patch_Pawn_RotationTracker_UpdateRotation
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(Pawn_RotationTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn.HasOffHand() && pawn.GetOffHandStance() is Stance_Busy stance_Busy && stance_Busy.focusTarg.IsValid && !pawn.pather.Moving)
            {
                if (stance_Busy.focusTarg.HasThing) __instance.Face(stance_Busy.focusTarg.Thing.DrawPos);
                else __instance.FaceCell(stance_Busy.focusTarg.Cell);
            }
        }
    }
}