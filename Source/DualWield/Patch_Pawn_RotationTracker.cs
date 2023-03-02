using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
    class Patch_Pawn_RotationTracker_UpdateRotation
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            bool found = false;
            Label label = generator.DefineLabel(), label2 = generator.DefineLabel();
            CodeInstruction injectTarget = null, returnTarget = null;
            var method = AccessTools.Method(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.FaceCell));
            int offsetSeek = 0, returnsToChange = 0;

            var tmp = new CodeInstruction(OpCodes.Ldarg_0);
            tmp.labels.Add(label2);

            foreach (var instruction in instructions)
            {
                if (offsetSeek > 0 && ++offsetSeek == 3) injectTarget = instruction;

                if (instruction.opcode == OpCodes.Ret) returnTarget = instruction;
                
                if (offsetSeek == 0 && instruction.opcode == OpCodes.Call && instruction.OperandIs(method))
                {
                    offsetSeek++;
                }

                if (returnsToChange > 0 && instruction.opcode == OpCodes.Brfalse_S)
                {
                    instruction.operand = label2;
                    returnsToChange--;
                }

                if (offsetSeek == 0 && instruction.opcode == OpCodes.Isinst) returnsToChange = 2;
            }

            returnTarget.labels.Add(label);

            foreach (var instruction in instructions)
            {
                if (instruction == injectTarget)
                {
                    found = true;
                    yield return tmp;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Pawn_RotationTracker_UpdateRotation), nameof(Patch_Pawn_RotationTracker_UpdateRotation.FaceOffHandTarget)));
                    yield return new CodeInstruction(OpCodes.Brtrue_S, label);
                }
                yield return instruction;
            }
            if (!found) Log.Error("[Tacticowl] Patch_Pawn_StanceTracker_FullBodyBusy transpiler failed to find its target. Did RimWorld update?");
        }
        public static bool FaceOffHandTarget(Pawn_RotationTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn.HasOffHand() && pawn.GetOffHandStance() is Stance_Busy stance_Busy && stance_Busy.focusTarg.IsValid && (!pawn.pather.Moving || pawn.RunsAndGuns()))
            {
                if (stance_Busy.focusTarg.HasThing) __instance.Face(stance_Busy.focusTarg.Thing.DrawPos);
                else __instance.FaceCell(stance_Busy.focusTarg.Cell);
                return true;
            }
            return false;
        }
    }
}