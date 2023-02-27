using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Tacticowl
{
	[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
	static class Patch_Drafted
	{
		static void Postfix(Pawn_DraftController __instance)
		{
			if (!__instance.Drafted) __instance.pawn.SetSearchAndDestroy(false);
		}
	}

	[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.GetGizmos))]
	static class Patch_GetGizmos
	{
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn_DraftController __instance)
		{
			foreach (var item in values) yield return item;

			Pawn pawn = __instance.pawn;
			if (pawn.Faction != null && pawn.Faction.def.isPlayer && 
				pawn.equipment != null && pawn.Drafted && (pawn.story == null || !pawn.WorkTagIsDisabled(WorkTags.Violent)))
			{
				if (pawn.equipment.Primary == null || pawn.equipment.Primary.def.IsMeleeWeapon)
				{
					yield return CreateGizmo_SearchAndDestroy_Melee(__instance.pawn);
				}
				else
				{
					yield return CreateGizmo_SearchAndDestroy_Ranged(__instance.pawn);
				}
			}
		}

		static Gizmo CreateGizmo_SearchAndDestroy_Melee(Pawn pawn)
		{			
			string disabledReason;
			bool disabled;
			if (pawn.Downed)
			{
				disabled = true;
				disabledReason = "SD_Reason_Downed".Translate();
			}
			else
			{
				disabledReason = "";
				disabled = false;
			}

			var gizmo = new Command_Toggle
			{
				defaultLabel = "SD_Gizmo_Melee_Label".Translate(),
				defaultDesc = "SD_Gizmo_Melee_Description".Translate(),
				hotKey = KeyBindingDefOf.Command_ItemForbid,
				disabled = disabled,
				disabledReason = disabledReason,
				icon = ContentFinder<Texture2D>.Get(("UI/Buttons/SearchAndDestroy_Gizmo_Melee"), true),
				isActive = () => pawn.SearchesAndDestroys(),
				toggleAction = () =>
				{
					pawn.SetSearchAndDestroy(!pawn.SearchesAndDestroys());
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
				}
			};
			return gizmo;
		}
		static Gizmo CreateGizmo_SearchAndDestroy_Ranged(Pawn pawn)
		{
			string disabledReason;
			bool disabled;
			if (pawn.Downed)
			{
				disabled = true;
				disabledReason = "SD_Reason_Downed".Translate();
			}
			else
			{
				disabledReason = "";
				disabled = false;
			}

			var gizmo = new Command_Toggle
			{
				defaultLabel = "SD_Gizmo_Ranged_Label".Translate(),
				defaultDesc = "SD_Gizmo_Ranged_Description".Translate(),
				hotKey = KeyBindingDefOf.Command_ItemForbid,
				disabled = disabled,
				disabledReason = disabledReason,
				icon = ContentFinder<Texture2D>.Get(("UI/Buttons/SearchAndDestroy_Gizmo_Ranged"), true),
				isActive = () => pawn.SearchesAndDestroys(),
				toggleAction = () =>
				{
					pawn.SetSearchAndDestroy(!pawn.SearchesAndDestroys());
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
				}
			};
			return gizmo;
		}
	}
}