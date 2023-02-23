using System;
using Verse;
using HarmonyLib;
using Settings = RunGunAndDestroy.ModSettings_RunAndDestroy;

namespace RunGunAndDestroy
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TicksPerMove))]
    static class Patch_TicksPerMove
    {
        static int Postfix(int __result, Pawn __instance)
        {
            if (__instance == null || __instance.stances == null)
            {
                return __result;
            }
            var curStance = __instance.stances.curStance.GetType().Name;
            if (curStance == nameof(Stance_RunAndGun) || curStance == nameof(Stance_RunAndGun_Cooldown))
            {   
                float factor = Settings.heavyWeaponsCache.Contains(__instance.equipment?.Primary?.def.shortHash ?? 0)
                     ? Settings.movementModifierHeavy : Settings.movementModifierLight;
                return (int)Math.Floor((float)__result / factor);
            }
            return __result;
        }
    }
}