using HarmonyLib;
using Verse;

namespace RunGunAndDestroy.DualWield
{
    [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
    class Patch_Pawn_RotationTracker_UpdateRotation
    {
        static void Postfix(Pawn_RotationTracker __instance, ref Pawn ___pawn)
        {
            if (___pawn.GetStancesOffHand() is Stance_Busy stance_Busy && stance_Busy.focusTarg.IsValid && !___pawn.pather.Moving)
            {
                if (stance_Busy.focusTarg.HasThing)
                {
                    __instance.Face(stance_Busy.focusTarg.Thing.DrawPos);
                }
                else
                {
                    __instance.FaceCell(stance_Busy.focusTarg.Cell);
                }
            }
        }
    }
}
