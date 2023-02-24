using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace RunGunAndDestroy.DualWield
{
    //This patch prevent an error thrown when a offhand weapon is equipped and the primary weapon is switched. 
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
    class Patch_Pawn_EquipmentTracker_AddEquipment
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!found && instruction.OperandIs(typeof(Pawn_EquipmentTracker).GetMethod("get_Primary")))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Pawn_EquipmentTracker_AddEquipment).GetMethod(nameof(PrimaryNoOffHand)));
                }
                else yield return instruction;
            }
        }
        //Make sure offhand weapons are never stored first in the list. 
        static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps newEq, ref ThingOwner<ThingWithComps> ___equipment)
        {
            ThingWithComps primary = __instance.Primary;
            if (primary != null && primary.IsOffHand())
            {
                if (___equipment != null)
                {
                    ___equipment.Remove(primary);
                    __instance.AddOffHandEquipment(primary);
                }
            }
        }
        public static ThingWithComps PrimaryNoOffHand(Pawn_EquipmentTracker instance)
        {
            ThingWithComps result = null;
            //When there's no offhand weapon equipped, use vanilla behaviour and throw the error when needed. Otherwise, make sure the error is never thrown. 
            if (!instance.TryGetOffHandEquipment(out ThingWithComps r))
            {
                return instance.Primary;
            }
            return result;
        }
    }
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.MakeRoomFor))]
    class Patch_Pawn_EquipmentTracker_MakeRoomFor
    {
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            bool offHandEquipped = __instance.TryGetOffHandEquipment(out ThingWithComps offHand);
            if (offHandEquipped && offHand == __instance.Primary && !eq.def.IsTwoHanded())
            {
                return false;
            }
            else
            {
                if (eq.def.IsTwoHanded() && offHandEquipped)
                {
                    DropOffHand(__instance, eq, offHand);
                    string herHis = __instance.pawn.story.bodyType == BodyTypeDefOf.Male ? "DW_HerHis_Male".Translate() : "DW_HerHis_Female".Translate();
                    Messages.Message("DW_Message_UnequippedOffHand".Translate(__instance.pawn.Name.ToStringShort, herHis), new LookTargets(__instance.pawn), MessageTypeDefOf.CautionInput);
                }
                return true;
            }
        }

        static void DropOffHand(Pawn_EquipmentTracker __instance, ThingWithComps eq, ThingWithComps offHand)
        {
            if (__instance.TryDropEquipment(offHand, out ThingWithComps resultingEq, __instance.pawn.Position))
            {
                resultingEq?.SetForbidden(value: false);
            }
            else
            {
                Log.Error(__instance.pawn + " couldn't make room for equipment " + eq);
            }
        }
    }
}