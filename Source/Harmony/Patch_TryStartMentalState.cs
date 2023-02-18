using System;
using Verse;
using HarmonyLib;
using RimWorld;
using Verse.AI;
using Settings = RunAndDestroy.ModSettings_RunAndDestroy;

namespace RunAndDestroy
{
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    static class Patch_TryStartMentalState
    {
        static void Postfix(MentalStateHandler __instance, MentalStateDef stateDef, ref Pawn ___pawn)
        {
            if (stateDef != MentalStateDefOf.PanicFlee || !Settings.enableForAI)
            {
                return;
            }
            CompRunAndGun comp = ___pawn.TryGetComp<CompRunAndGun>();
            if (comp != null)
            {
                comp.isEnabled = ShouldRunAndGun();
            }
        }
        static bool ShouldRunAndGun()
        {
            var rndInt = new Random(DateTime.Now.Millisecond).Next(1, 100);
            int chance = Settings.enableForFleeChance;
            if (rndInt <= chance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}