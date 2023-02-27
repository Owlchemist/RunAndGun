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
            FloatMenuOption menuItem;

            if (equipment.def.IsWeapon && pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false,  false, TraverseMode.ByPawn))
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (equipment.IsBurning())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "BurningLower".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (pawn.HasMissingArmOrHand())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " +"DW_MissArmOrHand".Translate() + " )", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsTwoHanded())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " + "DW_WieldingTwoHanded".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (equipment.def.IsTwoHanded())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " + "DW_NoTwoHandedInOffHand".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (!equipment.def.CanBeOffHand())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " + "DW_CannotBeOffHand".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else
            {
                string text5 = "DW_EquipOffHand".Translate(labelShort);
                if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                {
                    text5 = text5 + " " + "EquipWarningBrawler".Translate();
                }
                menuItem = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text5, delegate
                {
                    FleckMaker.Static(equipment.DrawPos, equipment.Map, FleckDefOf.FeedbackEquip, 1f);
                    equipment.SetForbidden(false, true);
                    pawn.jobs.TryTakeOrderedJob(new Job(ResourceBank.JobDefOf.DW_EquipOffHand, equipment), JobTag.Misc);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, equipment, "ReservedBy");
            }

            return menuItem;
        }
    }
}