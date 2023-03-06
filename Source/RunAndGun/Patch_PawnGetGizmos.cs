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
        //Used by MP
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var gizmo in values) yield return gizmo;
            if (!__instance.Drafted || !__instance.Faction.def.isPlayer || values.EnumerableNullOrEmpty())
            {
                yield break;
            }

            //Check if weapons are valid
            var mainWeapon = __instance.equipment?.Primary;
            __instance.GetOffHander(out ThingWithComps offHandWeapon);
            var hasProjectileWeapon = (mainWeapon != null && mainWeapon.def.IsWeaponUsingProjectiles) || (offHandWeapon != null && offHandWeapon.def.IsWeaponUsingProjectiles);
            if (!hasProjectileWeapon) yield break;

            //Check if weapons are allowed
            bool forbiddendWeapon = Settings.forbiddenWeaponsCache.Contains(mainWeapon?.def.shortHash ?? 0) || Settings.forbiddenWeaponsCache.Contains(offHandWeapon?.def.shortHash ?? 0);
            if (forbiddendWeapon) yield break;

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