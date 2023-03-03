using HarmonyLib;
using RimWorld;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    //Tick the stance tracker of the offfhand weapon
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) } )]
    class Patch_TryStartCastOn
    {
        static bool Prepare()
        {
            return Settings.runAndGunEnabled || Settings.dualWieldEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var label = generator.DefineLabel();
            bool labelNext = false;
            var method = AccessTools.Method(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance));
            foreach (CodeInstruction instruction in instructions)
            {
                if (labelNext)
                {
                    instruction.labels.Add(label);
                    break;
                }

                if (!labelNext && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(method))
                {
                    labelNext = true;
                }
            }
            
            bool found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (!found && instruction.opcode == OpCodes.Stloc_2)
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_2); //ticks
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //castTarg
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_TryStartCastOn).GetMethod(nameof(CheckTactics)));
                    yield return new CodeInstruction(OpCodes.Brtrue, label); //castTarg
                }
            }
            if (!found) Log.Error("[Tacticowl] Patch_TryStartCastOn transpiler failed to find its target. Did RimWorld update?");
        }

        
        static void Postfix(Verb __instance, LocalTargetInfo castTarg, ref bool __result)
        {
            //Check if it's an enemy that's attacked, and not a fire or an arguing husband
            //TODO: optimize this, this should be gated
            if (Settings.dualWieldEnabled && 
                __instance.EquipmentSource != null && !__instance.EquipmentSource.IsOffHandedWeapon() && 
                __instance.caster is Pawn casterPawn && !casterPawn.InMentalState && castTarg.Thing is not Fire)
            {
                DualWieldUtility.TryStartOffHandAttack(casterPawn, castTarg, ref __result);
            }
        }
        

        public static bool CheckTactics(Verb verb, int ticks, LocalTargetInfo castTarg)
        {
            Pawn pawn = verb.CasterPawn;
            if (pawn == null || pawn.RaceProps.Animal) return false;

            if (Settings.runAndGunEnabled && pawn.CurJobDef == JobDefOf.Goto && pawn.RunsAndGuns())
            {
                var curStance = pawn.stances.curStance;
                if (curStance is Stance_RunAndGun_Cooldown) return true;
                else if (curStance is not Stance_RunAndGun)
                {
                    pawn.stances.SetStance(new Stance_RunAndGun(ticks, castTarg, verb));
                    return true;
                }
            }

            if (Settings.dualWieldEnabled && verb.EquipmentSource != null && verb.EquipmentSource.IsOffHandedWeapon())
            {
                pawn.GetOffHandStanceTracker().SetStance(new Stance_Warmup_DW(ticks, castTarg, verb));
                return true;
            }
            return false;
        }
    }
}