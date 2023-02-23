﻿using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace RunGunAndDestroy
{
    [HarmonyPatch(typeof(AttackTargetsCache), nameof(AttackTargetsCache.GetPotentialTargetsFor))]
    static class Patch_GetPotentialTargetsFor
    {
        static void Postfix(IAttackTargetSearcher th, List<IAttackTarget> __result)
        {    
            if (th is not Pawn searchPawn || !searchPawn.SearchesAndDestroys()) return;

            for (int i= __result.Count - 1; i > -1; i--)
            {
                var target = __result[i];
                if (target is Pawn targetPawn)
                {
                    if (!targetPawn.NonHumanlikeOrWildMan() || targetPawn.IsAttacking()) continue;
                }
                
                __result.Remove(target);
            }
        }
    }
}