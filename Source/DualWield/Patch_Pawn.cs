﻿using HarmonyLib;
using RimWorld;
using System.Reflection.Emit;
using System.Collections.Generic;
using Verse;

namespace RunGunAndDestroy.DualWield
{
    //Tick the stance tracker of the offfhand weapon
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Tick))]
    class Patch_PawnTick
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (!found && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(AccessTools.Method(typeof(Pawn_RecordsTracker), nameof(Pawn_RecordsTracker.RecordsTick))))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_PawnTick).GetMethod(nameof(CheckDWStance)));
                }
            }
        }

        public static void CheckDWStance(Pawn pawn)
        {
            if (pawn.Spawned) pawn.GetStanceeTrackerOffHand().StanceTrackerTick();
        }
    }
    //Also try start off hand weapons attack when trystartattack is called
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryStartAttack))]
    class Patch_Pawn_TryStartAttack
    {
        static void Postfix(Pawn __instance, LocalTargetInfo targ, ref bool __result)
        {
            //Check if it's an enemy that's attacked, and not a fire or an arguing husband
            if ((!__instance.InMentalState && !(targ.Thing is Fire)))
            {
                __instance.TryStartOffHandAttack(targ, ref __result);
            }
        }
    }
    //If main weapon has shorter range than off hand weapon, use offhand weapon instead. 
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.CurrentEffectiveVerb), MethodType.Getter)]
    class Patch_Pawn_CurrentEffectiveVerb
    {
        static void Postfix(Pawn __instance, ref Verb __result)
        {
            if (__instance.MannedThing() == null &&
                __instance.equipment != null &&
                __instance.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip) &&
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