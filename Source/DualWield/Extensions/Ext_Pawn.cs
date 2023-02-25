using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SumGunFun.DualWield
{
    public static class Ext_Pawn
    {
        public static Stance GetStancesOffHand(this Pawn instance)
        {
            return GetStanceeTrackerOffHand(instance).curStance;
        }
        public static Pawn_StanceTracker GetStanceeTrackerOffHand(this Pawn instance)
        {
            if (!RNDStorage._instance.stancesOffhand.TryGetValue(instance, out Pawn_StanceTracker pawn_StanceTracker))
            {
                pawn_StanceTracker = new Pawn_StanceTracker(instance);
                RNDStorage._instance.stancesOffhand.Add(instance, pawn_StanceTracker);
            }
            return pawn_StanceTracker;
        }
        public static void SetStancesOffHand(this Pawn instance, Stance newStance)
        {
            Pawn_StanceTracker stanceTracker;
            if (!RNDStorage._instance.stancesOffhand.TryGetValue(instance, out stanceTracker))
            {
                stanceTracker = new Pawn_StanceTracker(instance);
                RNDStorage._instance.stancesOffhand.Add(instance, stanceTracker);
            }
            stanceTracker.SetStance(newStance);
        }
        public static void TryStartOffHandAttack(this Pawn __instance, LocalTargetInfo targ, ref bool __result)
        {
            if (__instance.equipment == null || !__instance.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                return;
            }
            var offhandStance = __instance.GetStancesOffHand();
            if (offhandStance is Stance_Warmup_DW || offhandStance is Stance_Cooldown)
            {
                return;
            }
            if (__instance.story != null && __instance.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
            {
                return;
            }
            if (__instance.jobs.curDriver.GetType().Name.Contains("Ability"))//Compatbility for Jecstools' abilities.
            {
                return;
            }
            bool allowManualCastWeapons = !__instance.IsColonist;
            Verb verb = __instance.TryGetOffhandAttackVerb(targ.Thing, true);
            
            if (verb != null)
            {
                bool success = verb.OffhandTryStartCastOn(targ);
                __result = __result || (verb != null && success);
            }
        }
        public static Verb TryGetOffhandAttackVerb(this Pawn instance, Thing target, bool allowManualCastWeapons = false)
        {
            Pawn_EquipmentTracker equipment = instance.equipment;
            ThingWithComps offHandEquip = null;
            CompEquippable compEquippable = null;
            if (equipment != null && equipment.TryGetOffHandEquipment(out ThingWithComps result) && result != equipment.Primary)
            {
                offHandEquip = result; //TODO: replace this temp code.
                compEquippable = offHandEquip.TryGetComp<CompEquippable>();
            }
            if (compEquippable != null && compEquippable.PrimaryVerb.Available() && (!compEquippable.PrimaryVerb.verbProps.onlyManualCast || (instance.CurJob != null && instance.CurJob.def != JobDefOf.Wait_Combat) || allowManualCastWeapons))
            {
                return compEquippable.PrimaryVerb;
            }
            else
            {
                return instance.TryGetMeleeVerbOffHand(target);
            }
        }
        public static bool HasMissingArmOrHand(this Pawn instance)
        {
            bool hasMissingHand = false;
            foreach (Hediff_MissingPart missingPart in instance.health.hediffSet.GetMissingPartsCommonAncestors())
            {
                if (missingPart.Part.def == BodyPartDefOf.Hand || missingPart.Part.def == BodyPartDefOf.Arm)
                {
                    hasMissingHand = true;
                }
            }
            return hasMissingHand;
        }
        public static Verb TryGetMeleeVerbOffHand(this Pawn instance, Thing target)
        {

            List<VerbEntry> usableVerbs = new List<VerbEntry>();
            if (instance.equipment != null && instance.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {              
                CompEquippable comp = offHandEquip.GetComp<CompEquippable>();
                //if (comp.AllVerbs.First((Verb verb) => verb.bu
                if (comp != null)
                {
                    List<Verb> allVerbs = comp.AllVerbs;
                    if (allVerbs != null)
                    {
                        for (int k = 0; k < allVerbs.Count; k++)
                        {
                            if (allVerbs[k].IsStillUsableBy(instance))
                            {
                                usableVerbs.Add(new VerbEntry(allVerbs[k], instance));
                            }
                        }
                    }
                }           
            }
            if (usableVerbs.TryRandomElementByWeight((VerbEntry ve) => ve.GetSelectionWeight(target), out VerbEntry result))
            {
                return result.verb;
            }
            return null;       
        }

    }
}
