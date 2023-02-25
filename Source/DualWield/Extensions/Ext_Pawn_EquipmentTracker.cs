using RimWorld;
using Verse;

namespace SumGunFun.DualWield
{
    static class Ext_Pawn_EquipmentTracker
    {
        //Tag offhand equipment so it can be recognised as offhand equipment during later evaluations. 
        public static void AddOffHandEquipment(this Pawn_EquipmentTracker instance, ThingWithComps newEq)
        {
            ThingOwner<ThingWithComps> equipment = instance.equipment;
           
            newEq.SetOffhand(true);
            LessonAutoActivator.TeachOpportunity(DW_DefOff.DW_Penalties, OpportunityType.GoodToKnow);
            LessonAutoActivator.TeachOpportunity(DW_DefOff.DW_Settings, OpportunityType.GoodToKnow);
            equipment.TryAdd(newEq, true);
        }
        //Only returns true when offhand weapon is used alongside a mainhand weapon. 
        public static bool TryGetOffHandEquipment(this Pawn_EquipmentTracker instance, out ThingWithComps result)
        {
            result = null;
            if (instance.pawn.HasMissingArmOrHand())
            {
                return false;
            }

            foreach (ThingWithComps twc in instance.AllEquipmentListForReading)
            {
                if (twc.IsOffHand())
                {
                    result = twc;
                    return true;
                }
            }
            return false;
        }
        public static void MakeRoomForOffHand(this Pawn_EquipmentTracker instance, ThingWithComps eq)
        {
            instance.TryGetOffHandEquipment(out ThingWithComps currentOffHand);
            if (currentOffHand != null)
            {
                ThingWithComps thingWithComps;
                if (instance.TryDropEquipment(currentOffHand, out thingWithComps, instance.pawn.Position, true))
                {
                    if (thingWithComps != null)
                    {
                        thingWithComps.SetForbidden(false, true);
                    }
                }
                else
                {
                    Log.Error(instance.pawn + " couldn't make room for equipment " + eq);
                }
            }
        }
    }
}