using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace RunGunAndDestroy.DualWield
{
    [HarmonyPatch(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.TryCastShot))]
    class Patch_Verb_MeleeAttack_TryCastShot
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!found && instruction.OperandIs(typeof(Pawn_StanceTracker).GetMethod("get_FullBodyBusy")))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Verb_MeleeAttack_TryCastShot).GetMethod(nameof(CurrentHandBusy)));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
        public static bool CurrentHandBusy(Pawn_StanceTracker instance, Verb verb)
        {
            Pawn pawn = instance.pawn;
            if (verb.EquipmentSource == null || !verb.EquipmentSource.IsOffHand()) return pawn.stances.FullBodyBusy;
            else return !verb.Available() || pawn.GetStancesOffHand().StanceBusy;
        }
    }
}