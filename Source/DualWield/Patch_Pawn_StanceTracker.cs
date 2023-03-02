using HarmonyLib;
using Verse;
using System.Reflection.Emit;
using System.Collections.Generic;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
	[HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.FullBodyBusy), MethodType.Getter)]
	class Patch_Pawn_StanceTracker_FullBodyBusy
	{
		static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
			bool found = false;
			var label = generator.DefineLabel();
			var method = AccessTools.Property(typeof(Stance), nameof(Stance.StanceBusy)).GetGetMethod();

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_I4_1) 
				{
					instruction.labels.Add(label);
					break;
				}
			}

			foreach (var instruction in instructions)
			{
				yield return instruction;
				if (!found && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(method))
				{
					found = true;
					yield return new CodeInstruction(OpCodes.Brtrue_S, label);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Pawn_StanceTracker_FullBodyBusy), nameof(Patch_Pawn_StanceTracker_FullBodyBusy.OffHandStanceBusy)));
				}
			}
			if (!found) Log.Error("[Tacticowl] Patch_Pawn_StanceTracker_FullBodyBusy transpiler failed to find its target. Did RimWorld update?");
		}
		public static bool OffHandStanceBusy(Pawn_StanceTracker __instance)
		{
			Pawn pawn = __instance.pawn;
			if (pawn.HasOffHand())
			{
				var stancesOffHand = pawn.GetOffHandStance();
				if (stancesOffHand is Stance_Cooldown && !pawn.RunsAndGuns()) return stancesOffHand.StanceBusy;
			}
			return false;
		}
	}
}