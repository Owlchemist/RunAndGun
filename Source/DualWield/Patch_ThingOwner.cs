using HarmonyLib;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    class Patch_ThingOwner_Remove
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq, bool __result)
        {
            if (!__result) return;
            if (eq.IsOffHandedWeapon()) __instance.pawn.SetOffHander(eq, true);
        }
    }
}