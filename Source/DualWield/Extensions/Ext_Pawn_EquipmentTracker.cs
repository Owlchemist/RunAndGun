using RimWorld;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    static class Ext_Pawn_EquipmentTracker
    {
       
        public static void MakeRoomForOffHand(this Pawn pawn, ThingWithComps eq)
        {
            if (pawn.GetOffHander(out ThingWithComps currentOffHand))
            {
                if (pawn.equipment.TryDropEquipment(currentOffHand, out ThingWithComps thingWithComps, pawn.Position, true))
                {
                    if (thingWithComps != null) thingWithComps.SetForbidden(false, false);
                }
                else if (Settings.logging) Log.Error("[Tacticowl] " + pawn.Label + " couldn't make room for equipment " + eq);
            }
        }
    }
}