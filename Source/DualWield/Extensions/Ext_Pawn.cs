using RimWorld;
using System.Collections.Generic;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    public static class Ext_Pawn
    {
        public static void TryStartOffHandAttack(this Pawn pawn, LocalTargetInfo targ, ref bool __result)
        {
            if (pawn.equipment == null || !pawn.GetOffHander(out ThingWithComps offHandEquip))
            {
                return;
            }
            
            var offHandStance = pawn.GetOffHandStance();
            if (offHandStance is Stance_Warmup_DW || offHandStance is Stance_Cooldown || pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return;
            }
            //Support for JecsTools
            //TODO: look into making making this XML-exposed via mod extensions?
            if (Settings.usingJecsTools && pawn.CurJobDef != null && 
                (pawn.CurJobDef.driverClass == ResourceBank.CastAbilitySelf.driverClass || pawn.CurJobDef.driverClass == ResourceBank.CastAbilityVerb.driverClass))
            {
                return;
            }

            if (pawn.TryGetOffHandAttackVerb(targ.Thing, out Verb verb, true))
            {
                if (__result) return;
                __result = verb.TryStartCastOn(targ);
            }
        }
        static bool TryGetOffHandAttackVerb(this Pawn instance, Thing target, out Verb verb, bool allowManualCastWeapons = false)
        {
            verb = null;
            if (instance.GetOffHander(out ThingWithComps offHandEquip))
            {
                CompEquippable compEquippable = offHandEquip.GetComp<CompEquippable>();
                
                if (compEquippable != null && compEquippable.PrimaryVerb.Available() && 
                (!compEquippable.PrimaryVerb.verbProps.onlyManualCast || instance.CurJobDef != JobDefOf.Wait_Combat || allowManualCastWeapons))
                {
                    verb = compEquippable.PrimaryVerb;
                }
            }
            else instance.TryGetMeleeVerbOffHand(target, out verb);
            return verb != null;
        }
        public static bool HasMissingArmOrHand(this Pawn instance)
        {
            var list = instance.health.hediffSet.GetMissingPartsCommonAncestors();
            for (int i = list.Count; i-- > 0;)
            {
                var partDef = list[i].Part.def;
                if (partDef == BodyPartDefOf.Hand || partDef == BodyPartDefOf.Arm) return true;
            }
            
            return false;
        }
        public static bool TryGetMeleeVerbOffHand(this Pawn instance, Thing target, out Verb verb)
        {
            verb = null;
            if (instance.GetOffHander(out ThingWithComps offHandEquip))
            {              
                List<Verb> allVerbs = offHandEquip.GetComp<CompEquippable>()?.AllVerbs;
                if (allVerbs != null)
                {
                    List<VerbEntry> usableVerbs = new List<VerbEntry>();
                    for (int k = allVerbs.Count; k-- > 0;)
                    {
                        Verb v = allVerbs[k];
                        if (v.IsStillUsableBy(instance)) usableVerbs.Add(new VerbEntry(v, instance));
                    }
                    if (usableVerbs.TryRandomElementByWeight(ve => ve.GetSelectionWeight(target), out VerbEntry result))
                    {
                        verb = result.verb;
                    }
                }
            }
            return verb != null;
        }
    }
}