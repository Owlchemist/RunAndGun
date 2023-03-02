using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.TryCastShot))]
    class Patch_Verb_MeleeAttack_TryCastShot
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var method = AccessTools.Property(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.FullBodyBusy)).GetGetMethod();
            foreach (CodeInstruction instruction in instructions)
            {
                if (!found && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(method))
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
            if (!found) Log.Error("[Tacticowl] Patch_Verb_MeleeAttack_TryCastShot transpiler failed to find its target. Did RimWorld update?");
        }
        public static bool CurrentHandBusy(Pawn_StanceTracker instance, Verb verb)
        {
            Pawn pawn = instance.pawn;
            if (verb.EquipmentSource == null || !verb.EquipmentSource.IsOffHandedWeapon()) return pawn.stances.FullBodyBusy;
            else return !verb.Available() || pawn.GetOffHandStance().StanceBusy;
        }
    }
}