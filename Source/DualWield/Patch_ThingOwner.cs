using HarmonyLib;
using Verse;

namespace RunGunAndDestroy.DualWield
{
    [HarmonyPatch(typeof(ThingOwner<Thing>), nameof(ThingOwner<Thing>.Remove))]
    class Patch_ThingOwner_Remove
    {
        static void Postfix(Thing item)
        {
            item.SetOffhand(false);
        }
    }
}