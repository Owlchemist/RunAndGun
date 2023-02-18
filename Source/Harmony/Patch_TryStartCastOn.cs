using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace RunAndDestroy
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    static class Patch_TryStartCastOn
    {
        static bool Prefix(Verb __instance, LocalTargetInfo castTarg, bool surpriseAttack, bool canHitNonTargetPawns)
        {
            Pawn pawn = __instance.CasterPawn;
            var caster = __instance.caster;
            var casterIsPawn = __instance.CasterIsPawn;

            if (caster == null || 
                !caster.Spawned || 
                __instance.state == VerbState.Bursting || !__instance.CanHitTarget(castTarg))
            {
                return false;
            }
            
            if (__instance.verbProps.CausesTimeSlowdown && castTarg.HasThing && (castTarg.Thing.def.category == ThingCategory.Pawn || (castTarg.Thing.def.building != null && 
                castTarg.Thing.def.building.IsTurret)) && castTarg.Thing.Faction.def.isPlayer && caster.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer))
            {
                Find.TickManager.slower.SignalForceNormalSpeed();
            }
            if (casterIsPawn)
            {
                CompRunAndGun comp = pawn.TryGetComp<CompRunAndGun>();
                if (comp == null || !comp.isEnabled || pawn.CurJobDef != JobDefOf.Goto)
                {
                    return true;
                }
            }
            if (!casterIsPawn)
            {
                return true;
            }

            var curStance = pawn.stances.curStance.GetType().Name;
            if (curStance == nameof(Stance_RunAndGun) || curStance == nameof(Stance_RunAndGun_Cooldown))
            {
                return false;
            }

            __instance.surpriseAttack = surpriseAttack;
            __instance.canHitNonTargetPawnsNow = canHitNonTargetPawns;
            __instance.currentTarget = castTarg;

            if (casterIsPawn && __instance.verbProps.warmupTime > 0f)
            {
                ShootLine newShootLine;
                if (!__instance.TryFindShootLineFromTo(caster.Position, castTarg, out newShootLine))
                {
                    return false;
                }
                pawn.Drawer.Notify_WarmingCastAlongLine(newShootLine, caster.Position);
                float statValue = pawn.GetStatValue(StatDefOf.AimingDelayFactor, true);
                int ticks = (__instance.verbProps.warmupTime * statValue).SecondsToTicks();
                pawn.stances.SetStance(new Stance_RunAndGun(ticks, castTarg, __instance));
            }
            else __instance.WarmupComplete();
            return false;
        }
    }
}