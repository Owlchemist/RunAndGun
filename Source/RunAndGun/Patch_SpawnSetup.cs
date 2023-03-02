using Verse;
using HarmonyLib;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    class Patch_SpawnSetup
    {
        static bool Prepare()
        {
            return Settings.runAndGunEnabled;
        }
        static void Postfix(Pawn __instance)
        {
            if (!__instance.IsColonist && Settings.enableForAI && 
				(__instance.RaceProps.intelligence != Intelligence.Animal || Settings.enableForAnimals )) 
			{
				__instance.SetRunsAndGuns(true);
			}
        }
    }
}