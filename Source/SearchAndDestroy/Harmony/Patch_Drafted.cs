using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RunGunAndDestroy
{
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
    public static class Patch_Drafted
    {
        public static void Postfix(Pawn_DraftController __instance)
        {
            if(!__instance.Drafted)
            {
                __instance.pawn.SetSearchAndDestroy(false);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.GetGizmos))]
    public static class Patch_GetGizmos
    {
        public static void Postfix(ref IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
        {
            List<Gizmo> gizmoList = __result.ToList();
            bool isPlayerPawn = __instance.pawn.Faction != null && __instance.pawn.Faction.IsPlayer;

            if (isPlayerPawn && __instance.pawn.equipment != null && __instance.pawn.Drafted && (__instance.pawn.story == null || !__instance.pawn.WorkTagIsDisabled(WorkTags.Violent)))
            {
                if (__instance.pawn.equipment.Primary == null || __instance.pawn.equipment.Primary.def.IsMeleeWeapon)
                {
                    gizmoList.Add(CreateGizmo_SearchAndDestroy_Melee(__instance));
                }
                else
                {
                    gizmoList.Add(CreateGizmo_SearchAndDestroy_Ranged(__instance));
                }
            }
            __result = gizmoList;

        }

        static Gizmo CreateGizmo_SearchAndDestroy_Melee(Pawn_DraftController __instance)
        {
            string disabledReason = "";
            bool disabled = false;
            PawnDuty duty = __instance.pawn.mindState.duty;

            if (__instance.pawn.Downed)
            {
                disabled = true;
                disabledReason = "SD_Reason_Downed".Translate();
            }
            Gizmo gizmo = new Command_Toggle
            {
                defaultLabel = "SD_Gizmo_Melee_Label".Translate(),
                defaultDesc = "SD_Gizmo_Melee_Description".Translate(),
                hotKey = KeyBindingDefOf.Command_ItemForbid,
                disabled = disabled,
                disabledReason = disabledReason,
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/SearchAndDestroy_Gizmo_Melee"), true),
                isActive = () => __instance.pawn.SearchesAndDestroys(),
                toggleAction = () =>
                {
                    __instance.pawn.SetSearchAndDestroy(!__instance.pawn.SearchesAndDestroys());
                    __instance.pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            };
            return gizmo;
        }
        static Gizmo CreateGizmo_SearchAndDestroy_Ranged(Pawn_DraftController __instance)
        {
            string disabledReason = "";
            bool disabled = false;
            PawnDuty duty = __instance.pawn.mindState.duty;

            if (__instance.pawn.Downed)
            {
                disabled = true;
                disabledReason = "SD_Reason_Downed".Translate();
            }
            Gizmo gizmo = new Command_Toggle
            {
                defaultLabel = "SD_Gizmo_Ranged_Label".Translate(),
                defaultDesc = "SD_Gizmo_Ranged_Description".Translate(),
                hotKey = KeyBindingDefOf.Command_ItemForbid,
                disabled = disabled,
                disabledReason = disabledReason,
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/SearchAndDestroy_Gizmo_Ranged"), true),
                isActive = () => __instance.pawn.SearchesAndDestroys(),
                toggleAction = () =>
                {
                    __instance.pawn.SetSearchAndDestroy(!__instance.pawn.SearchesAndDestroys());
                    __instance.pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            };
            return gizmo;
        }
    }
}