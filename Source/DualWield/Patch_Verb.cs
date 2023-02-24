using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace RunGunAndDestroy.DualWield
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public class Patch_Verb_TryStartCastOn {
        static void Postfix(Verb __instance, LocalTargetInfo castTarg, ref bool __result)
        {
            if (__instance.caster is Pawn casterPawn)
            {
                //Check if it's an enemy that's attacked, and not a fire or an arguing husband
                if ((!casterPawn.InMentalState && !(castTarg.Thing is Fire)))
                {
                    casterPawn.TryStartOffHandAttack(castTarg, ref __result);
                }
            }
        }
    }
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
    public class Patch_Verb_TryCastNextBurstShot
    {
        [HarmonyPriority(Priority.Low)]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!found && instruction.OperandIs(typeof(Pawn_StanceTracker).GetMethod("SetStance")))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Verb_TryCastNextBurstShot).GetMethod(nameof(SetStanceOffHand)));
                }
                else yield return instruction;
            }
        }
        public static void SetStanceOffHand(Pawn_StanceTracker stanceTracker,  Stance_Cooldown stance)
        {
            ThingWithComps offHandEquip = null;
            CompEquippable compEquippable = null;


            if (stance.verb.EquipmentSource != null && stance.verb.EquipmentSource.IsOffHand())
            {
                offHandEquip = stance.verb.EquipmentSource;
                compEquippable = offHandEquip.TryGetComp<CompEquippable>();
            }
            //Check if verb is one from a offhand weapon. 
            if (compEquippable != null && offHandEquip != stanceTracker.pawn.equipment.Primary) //TODO: check this code 
            {
                stanceTracker.pawn.SetStancesOffHand(stance);
            }
            else if (stanceTracker.curStance.GetType().Name != "Stance_RunAndGun_Cooldown")
            {
                stanceTracker.SetStance(stance);
            }
        }
    }
}