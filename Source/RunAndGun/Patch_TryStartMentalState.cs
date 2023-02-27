using System;
using Verse;
using HarmonyLib;
using RimWorld;
using Verse.AI;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    static class Patch_TryStartMentalState
    {
        static void Postfix(MentalStateHandler __instance, MentalStateDef stateDef)
        {
            if (stateDef == MentalStateDefOf.PanicFlee && Settings.enableForAI)
            {
                var rndInt = new Random(DateTime.Now.Millisecond).Next(1, 100);
                if (rndInt <= Settings.enableForFleeChance) __instance.pawn.SetRunsAndGuns(true);
            }
        }
    }
}