using RimWorld;
using Verse;

namespace Tacticowl.DualWield
{
    public static class Ext_Verb
    {
        public static bool OffHandTryStartCastOn(this Verb instance, LocalTargetInfo castTarg)
        {   
            if (instance.caster == null || 
                !instance.caster.Spawned || 
                instance.state == VerbState.Bursting || 
                !instance.CanHitTarget(castTarg))
            {
                return false;
            }
            
            instance.currentTarget = castTarg;
            var warmupTime = instance.verbProps.warmupTime;
            if (instance.CasterIsPawn && warmupTime > 0f)
            {
                if (!instance.TryFindShootLineFromTo(instance.caster.Position, castTarg, out ShootLine newShootLine))
                {
                    return false;
                }
                Pawn pawn = instance.CasterPawn;
                pawn.Drawer.Notify_WarmingCastAlongLine(newShootLine, instance.caster.Position);
                int ticks = (warmupTime * pawn.GetStatValue(StatDefOf.AimingDelayFactor)).SecondsToTicks();
                pawn.GetOffHandStanceTracker().SetStance(new Stance_Warmup_DW(ticks, castTarg, instance));
            }
            else instance.WarmupComplete();
            return true;
        }
    }
}