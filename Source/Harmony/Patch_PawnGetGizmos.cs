using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using UnityEngine;
using RimWorld;
using Settings = RunAndDestroy.ModSettings_RunAndDestroy;

namespace RunAndDestroy
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public class Patch_PawnGetGizmos
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var gizmo in values) yield return gizmo;
            if (__instance == null || !__instance.Drafted || !__instance.Faction.def.isPlayer || !(__instance.equipment?.Primary?.def.IsWeaponUsingProjectiles ?? true)
                 || values == null || !values.Any())
            {
                yield break;
            }
            CompRunAndGun data = __instance.TryGetComp<CompRunAndGun>();
            if(data == null) yield break;
            
            if (__instance.equipment != null && __instance.equipment.Primary != null && Settings.forbiddenWeaponsCache.Contains(__instance.equipment.Primary.def.shortHash))
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "RG_Action_Enable_Label".Translate(),
                defaultDesc = data.isEnabled ? "RG_Action_Disable_Description".Translate() : "RG_Action_Enable_Description".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/enable_RG"), true),
                isActive = () => data.isEnabled,
                toggleAction = () => { data.isEnabled = !data.isEnabled; } 
            };
        }
    }
}