using HarmonyLib;
using Verse;
using RimWorld;
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
    //This is just to patch support for other mods' use of an offhand
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    class Patch_Pawn_ApparelTracker_Wear
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled && Settings.VFECoreEnabled;
        }
        static void Postfix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (Settings.OffHandShield(__instance.pawn) != null) DualWieldUtility.MakeRoomForOffHand(__instance.pawn);
        }
    }
}