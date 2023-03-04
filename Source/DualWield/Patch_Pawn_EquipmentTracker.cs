using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    //This patch prevent an error thrown when a offHand weapon is equipped and the primary weapon is switched. 
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
    class Patch_Pawn_EquipmentTracker_AddEquipment
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var method = AccessTools.Property(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Primary)).GetGetMethod();
            foreach (CodeInstruction instruction in instructions)
            {
                if (!found && instruction.opcode == OpCodes.Call && instruction.OperandIs(method))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Pawn_EquipmentTracker_AddEquipment).GetMethod(nameof(PrimaryNoOffHand)));
                }
                else yield return instruction;
            }
            if (!found) Log.Error("[Tacticowl] Patch_Pawn_EquipmentTracker_AddEquipment transpiler failed to find its target. Did RimWorld update?");
        }
        //Make sure offHand weapons are never stored first in the list. 
        static void Postfix(Pawn_EquipmentTracker __instance, ThingOwner<ThingWithComps> ___equipment)
        {
            ThingWithComps primary = __instance.Primary;
            if (___equipment != null && DualWieldExtensions.IsOffHandedWeapon(primary))
            {
                ___equipment.Remove(primary);
                __instance.pawn.SetOffHander(primary);   
            }
        }
        public static ThingWithComps PrimaryNoOffHand(Pawn_EquipmentTracker instance)
        {
            ThingWithComps result = null;
            //When there's no offHand weapon equipped, use vanilla behaviour and throw the error when needed. Otherwise, make sure the error is never thrown. 
            if (!instance.pawn.GetOffHander(out ThingWithComps r))
            {
                return instance.Primary;
            }
            return result;
        }
    }
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.MakeRoomFor))]
    class Patch_Pawn_EquipmentTracker_MakeRoomFor
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            Pawn pawn = __instance.pawn;
            bool pawnHasOffhand = pawn.HasOffHand();
            ThingDef def = eq.def;

            if (Settings.IsShield(def) && !pawnHasOffhand) return false; //This is a shield and there's no offhand, return false because it was already handled by VEF
            if ((!pawnHasOffhand && !eq.IsOffHandedWeapon()) || //We don't have an offhand and this is not an offhand. Anything we're equipping can be handled normally
                (pawnHasOffhand && !eq.IsOffHandedWeapon() && !def.IsTwoHanded() && !Settings.IsShield(def)) //We are dual wielding but swapping out the main hand weapon, it can be swapped normally
            ) return true;

            if (!pawn.GetOffHander(out ThingWithComps currentOffHand))
            {
                if (Settings.VFECoreEnabled) currentOffHand = Settings.OffHandShield(pawn);
                if (currentOffHand != null && !eq.IsOffHandedWeapon() && !def.IsTwoHanded() && !pawn.HasOffHand()) return false; //Don't do anything, VE already handled it and we have nothing to do
            }

            if (currentOffHand != null)
            {
                bool success = false;
                if (currentOffHand is not Apparel)
                {
                    success = pawn.equipment.TryDropEquipment(currentOffHand, out currentOffHand, pawn.Position, true);
                    if (def.IsTwoHanded()) return true; //Need to let the normal removal handle the primary weapon
                }
                else if (Settings.VFECoreEnabled && currentOffHand is Apparel apparel)
                {
                    success = pawn.apparel.TryDrop(apparel, out apparel, pawn.Position, true);
                }
                if (!success) Log.Error("[Tacticowl] " + pawn.Label + " couldn't make room for equipment");
            }
            return false;
        }
    }
}