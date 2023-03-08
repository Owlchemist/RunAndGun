using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddHumanlikeOrders))]
    class Patch_FloatMenuMakerMap_AddHumanlikeOrders
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }

        static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            //Right click yourself to drop your offHand weapon
            foreach (LocalTargetInfo current in GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true))
            {
                if (pawn.GetOffHander(out ThingWithComps eq))
                {
                    opts.Add(new FloatMenuOption("DW_DropOffHand".Translate(eq.LabelShort), new Action(delegate 
                    {
                        pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, eq));
                    })));
                }
            }

            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && pawn.equipment != null)
            {
                List<Thing> thingList = IntVec3.FromVector3(clickPos).GetThingList(pawn.Map);
                for (int i = thingList.Count; i-- > 0;)
                {
                    if (thingList[i] is ThingWithComps equipment && equipment.GetComp<CompEquippable>() != null)
                    {
                        opts.Add(GetEquipOffHandOption(pawn, equipment));
                    }
                }
            }
        }

        static FloatMenuOption GetEquipOffHandOption(Pawn pawn, ThingWithComps equipment)
        {
            string labelShort = equipment.LabelShort;
            string cannotEquipText = "CannotEquip".Translate(labelShort);

            if (!equipment.def.CanBeOffHand())
                return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "DW_CannotBeOffHand".Translate()), null);

            if (equipment.def.IsTwoHanded())
                return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "DW_NoTwoHandedInOffHand".Translate()), null);

            if (pawn.equipment != null && pawn.equipment.Primary != null)
            {
                if (pawn.equipment.Primary.def.IsTwoHanded())
                    return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "DW_WieldingTwoHanded".Translate()), null);
            }
            else return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "DW_MissingPrimary".Translate()), null);
            
            if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
                return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn)), null);
            
            if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false,  false, TraverseMode.ByPawn))
                return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "NoPath".Translate()), null);
            
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "Incapable".Translate()), null);
            
            if (equipment.IsBurning())
                return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "BurningLower".Translate()), null);
            
            if (pawn.HasMissingArmOrHand())
                return new FloatMenuOption("DW_AsOffHand".Translate(cannotEquipText, "DW_MissArmOrHand".Translate()), null);
            
            
            string text = "DW_EquipOffHand".Translate(labelShort);
            if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
            {
                text += "EquipWarningBrawler".Translate();
            }
            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
            {
                FleckMaker.Static(equipment.DrawPos, equipment.Map, FleckDefOf.FeedbackEquip, 1f);
                equipment.SetForbidden(false, true);
                pawn.jobs.TryTakeOrderedJob(new Job(ResourceBank.JobDefOf.DW_EquipOffHand, equipment), JobTag.Misc);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
            }, MenuOptionPriority.High), pawn, equipment);
        }
    }
}