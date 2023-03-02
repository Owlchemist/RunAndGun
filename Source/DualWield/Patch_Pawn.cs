using HarmonyLib;
using RimWorld;
using System.Reflection.Emit;
using System.Collections.Generic;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    //Tick the stance tracker of the offfhand weapon
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Tick))]
    class Patch_PawnTick
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var method = AccessTools.Method(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.StanceTrackerTick));
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (!found && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(method))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_PawnTick).GetMethod(nameof(CheckDWStance)));
                }
            }
            if (!found) Log.Error("[Tacticowl] Patch_PawnTick transpiler failed to find its target. Did RimWorld update?");
        }

        public static void CheckDWStance(Pawn pawn)
        {
            if (pawn.HasOffHand()) pawn.GetOffHandStanceTracker().StanceTrackerTick();
        }
    }
    //Also try start off hand weapons attack when trystartattack is called
    //Only used by attacking when just standing around, or direct commands
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryStartAttack))]
    class Patch_Pawn_TryStartAttack
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(Pawn __instance, LocalTargetInfo targ, ref bool __result)
        {
            //Check if it's an enemy that's attacked, and not a fire or an arguing husband
            if ((!__instance.InMentalState && targ.Thing is not Fire))
            {
                __instance.TryStartOffHandAttack(targ, ref __result);
            }
        }
    }
    
    //If main weapon has shorter range than off hand weapon, use offHand weapon instead. 
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.CurrentEffectiveVerb), MethodType.Getter)]
    class Patch_Pawn_CurrentEffectiveVerb
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(Pawn __instance, ref Verb __result)
        {
            if (__instance.MannedThing() == null &&
                __instance.equipment != null &&
                __instance.GetOffHander(out ThingWithComps offHandEquip) &&
                !offHandEquip.def.IsMeleeWeapon &&
                offHandEquip.TryGetComp<CompEquippable>() is CompEquippable compEquip)
            {
                Verb verb = compEquip.PrimaryVerb;
                if (__result.IsMeleeAttack || __result.verbProps.range < verb.verbProps.range)
                {
                    __result = verb;
                }
            }
        }
    }
}