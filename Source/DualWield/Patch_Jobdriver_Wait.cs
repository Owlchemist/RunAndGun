using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(JobDriver_Wait), nameof(JobDriver_Wait.CheckForAutoAttack))]
    class Patch_Jobdriver_Wait_CheckForAutoAttack
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!found && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(AccessTools.Property(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.FullBodyBusy)).GetGetMethod()))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Jobdriver_Wait_CheckForAutoAttack).GetMethod(nameof(FullBodyAndOffHandBusy)));
                }
                else yield return instruction;
            }
            if (!found) Log.Error("[Tacticowl] Patch_Jobdriver_Wait_CheckForAutoAttack transpiler failed to find its target. Did RimWorld update?");
        }
        public static bool FullBodyAndOffHandBusy(Pawn_StanceTracker instance)
        {
            if (instance.pawn.HasOffHand() && instance.pawn.GetOffHander(out ThingWithComps twc))
            {
                return instance.pawn.GetOffHandStanceTracker().FullBodyBusy && instance.FullBodyBusy;
            }
            return instance.FullBodyBusy;
        }
    }
}