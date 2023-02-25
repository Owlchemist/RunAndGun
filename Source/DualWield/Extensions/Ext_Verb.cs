using RimWorld;
using Verse;

namespace SumGunFun.DualWield
{
    public static class Ext_Verb
    {
        public static bool OffhandTryStartCastOn(this Verb instance, LocalTargetInfo castTarg)
        {   
            if (instance.caster == null || 
                !instance.caster.Spawned || 
                instance.state == VerbState.Bursting || 
                !instance.CanHitTarget(castTarg))
            {
                return false;
            }
            
            instance.currentTarget = castTarg;
            if (instance.CasterIsPawn && instance.verbProps.warmupTime > 0f)
            {
                ShootLine newShootLine;
                if (!instance.TryFindShootLineFromTo(instance.caster.Position, castTarg, out newShootLine))
                {
                    return false;
                }
                instance.CasterPawn.Drawer.Notify_WarmingCastAlongLine(newShootLine, instance.caster.Position);
                float statValue = instance.CasterPawn.GetStatValue(StatDefOf.AimingDelayFactor, true);
                int ticks = (instance.verbProps.warmupTime * statValue).SecondsToTicks();
                instance.CasterPawn.SetStancesOffHand(new Stance_Warmup_DW(ticks, castTarg, instance));
            }
            else instance.WarmupComplete();
            return true;
        }
    }
}