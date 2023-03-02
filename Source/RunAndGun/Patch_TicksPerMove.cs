using System;
using Verse;
using HarmonyLib;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TicksPerMove))]
    class Patch_TicksPerMove
    {
        static bool Prepare()
        {
            return Settings.runAndGunEnabled;
        }
        static int Postfix(int __result, Pawn __instance)
        {
            if (__instance == null || __instance.stances == null)
            {
                return __result;
            }
            var curStance = __instance.stances.curStance;
            if (curStance is Stance_RunAndGun || curStance is Stance_RunAndGun_Cooldown)
            {   
                float factor = Settings.heavyWeaponsCache.Contains(__instance.equipment?.Primary?.def.shortHash ?? 0)
                     ? Settings.movementModifierHeavy : Settings.movementModifierLight;
                return (int)Math.Floor((float)__result / factor);
            }
            return __result;
        }
    }
}