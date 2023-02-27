using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
	[HarmonyPatch(typeof(JobDriver), nameof(JobDriver.SetupToils))]
	static class Patch_SetupToils
	{
		static TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
		static void Postfix(JobDriver __instance)
		{
			if (__instance is not JobDriver_Goto jobDriver || !__instance.pawn.RunsAndGuns() || __instance.toils.Count == 0)
			{
				return;
			}
			
			__instance.toils[0].AddPreTickAction(delegate
			{
				int ticker = 10;
				Pawn pawn = jobDriver.pawn;
				if (ticker-- == 0)
				{
					ticker = 10;
					if ((pawn.Drafted || ((pawn.Faction == null || !pawn.Faction.def.isPlayer) && Settings.enableForAI) || (pawn.RaceProps.Animal && Settings.enableForAnimals)) && !pawn.Downed && 
						!pawn.HasAttachment(ThingDefOf.Fire))
					{
						CheckForAutoAttack(pawn, __instance);
					}
				}
			});
		}
		static void CheckForAutoAttack(Pawn pawn, JobDriver jobDriver)
		{
			if ((pawn.story == null || !pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent)) &&
				pawn.Faction != null &&
				pawn.stances.curStance is not Stance_RunAndGun &&
				(pawn.drafter == null || pawn.drafter.FireAtWill))
			{
				Verb verb = pawn.TryGetAttackVerb(null);
				if (verb != null)
				{
					if (verb.IsIncendiary_Ranged()) targetScanFlags |= TargetScanFlags.NeedNonBurning;
					
					Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(pawn, targetScanFlags);
					if (thing != null && !(verb.IsMeleeAttack && pawn.CanReachImmediate(thing, PathEndMode.Touch))) //Don't allow melee attacks, but take into account ranged animals or dual wield users
					{
						pawn.TryStartAttack(thing);
						jobDriver.collideWithPawns = true;
						return;
					}
				}
			}
		}
	}
}