using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using UnityEngine;
using RimWorld;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    class Patch_PawnGetGizmos
    {
        static bool Prepare()
        {
            return Settings.runAndGunEnabled;
        }
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var gizmo in values) yield return gizmo;
            if (__instance == null || !__instance.Drafted || !__instance.Faction.def.isPlayer || !(__instance.equipment?.Primary?.def.IsWeaponUsingProjectiles ?? true)
                 || values == null || !values.Any() || Settings.forbiddenWeaponsCache.Contains(__instance.equipment?.Primary?.def.shortHash ?? 0))
            {
                yield break;
            }

            bool isEnabled = __instance.RunsAndGuns();

            yield return new Command_Toggle
            {
                defaultLabel = "RG_Action_Enable_Label".Translate(),
                defaultDesc = isEnabled ? "RG_Action_Disable_Description".Translate() : "RG_Action_Enable_Description".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/enable_RG"), true),
                isActive = () => isEnabled,
                toggleAction = () => { __instance.SetRunsAndGuns(!isEnabled); } 
            };
        }
    }
}