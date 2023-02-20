using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace RunGunAndDestroy
{
    [HarmonyPatch(typeof(AttackTargetsCache), nameof(AttackTargetsCache.GetPotentialTargetsFor))]
    static class Patch_GetPotentialTargetsFor
    {
        static void Postfix(IAttackTargetSearcher th, ref List<IAttackTarget> __result)
        {
            List<IAttackTarget> shouldRemove = new List<IAttackTarget>();
            var searchPawn = th as Pawn;
            if (searchPawn == null)
            {
                return;
            }
                
            if (!searchPawn.SearchesAndDestroys()) //only apply patch for SD pawns
            {
                return;
            }
            
            foreach (var target in __result)
            {
                var targetPawn = target as Pawn;
                if (targetPawn != null && targetPawn.NonHumanlikeOrWildMan() && !targetPawn.IsAttacking())
                {
                    shouldRemove.Add(target);
                }
                if(targetPawn == null)//thing is building
                {
                    shouldRemove.Add(target);
                }
            }
            __result = __result.Except(shouldRemove).ToList();
        }
    }
}