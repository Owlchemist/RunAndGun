using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace RunAndDestroy
{
	[HarmonyPatch(typeof(JobDriver), nameof(JobDriver.SetupToils))]
	static class Patch_SetupToils
	{
		static TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat;
		static void Postfix(JobDriver __instance, List<Toil> ___toils)
		{
			if(__instance is not JobDriver_Goto jobDriver)
			{
				return;
			}
			if (___toils.Count > 0)
			{
				Toil toil = ___toils[0];
				toil.AddPreTickAction(delegate
				{
					if (jobDriver.pawn != null && 
						jobDriver.pawn.IsHashIntervalTick(10) &&  
						(jobDriver.pawn.Drafted || !jobDriver.pawn.IsColonist) && !jobDriver.pawn.Downed && 
						!jobDriver.pawn.HasAttachment(ThingDefOf.Fire))
					{
						CheckForAutoAttack(jobDriver.pawn);
					}
				});

			}
		}
		static void CheckForAutoAttack(Pawn pawn)
		{
			if ((pawn.story == null || !pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
				&& pawn.Faction != null
				&& !(pawn.stances.curStance is Stance_RunAndGun)
				&& (pawn.drafter == null || pawn.drafter.FireAtWill))
			{
				CompRunAndGun comp = pawn.TryGetComp<CompRunAndGun>();
				if (comp == null || comp.isEnabled == false)
				{
					return;
				}
				Verb verb = pawn.TryGetAttackVerb(null);
								
				if (verb != null)
				{
					if (verb.IsIncendiary_Ranged()) targetScanFlags |= TargetScanFlags.NeedNonBurning;
					
					Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(pawn, targetScanFlags, null, 0f, 9999f);
					if (thing != null && !(verb.IsMeleeAttack && pawn.CanReachImmediate(thing, PathEndMode.Touch))) //Don't allow melee attacks, but take into account ranged animals or dual wield users
					{
						pawn.TryStartAttack(thing);
						return;
					}
				}
			}
		}
	}
}