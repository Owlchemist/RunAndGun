using HarmonyLib;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(ThingOwner<Thing>), nameof(ThingOwner<Thing>.Remove))]
    class Patch_ThingOwner_Remove
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(Thing item)
        {
            item.SetWeaponAsOffHanded(false);
        }
    }
}