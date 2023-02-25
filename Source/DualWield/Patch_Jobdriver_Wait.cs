using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Settings = SumGunFun.ModSettings_SumGunFun;

namespace SumGunFun.DualWield
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
                if (!found && instruction.OperandIs(typeof(Pawn_StanceTracker).GetMethod("get_FullBodyBusy")))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Jobdriver_Wait_CheckForAutoAttack).GetMethod(nameof(FullBodyAndOffHandBusy)));
                }
                else yield return instruction;
            }
        }
        public static bool FullBodyAndOffHandBusy(Pawn_StanceTracker instance)
        {
            if (instance.pawn.equipment != null && instance.pawn.equipment.TryGetOffHandEquipment(out ThingWithComps twc))
            {
                return instance.pawn.GetStanceeTrackerOffHand().FullBodyBusy && instance.FullBodyBusy;
            }
            return instance.FullBodyBusy;
        }
    }
}