using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(VerbTracker), nameof(VerbTracker.CreateVerbTargetCommand))]
    class Patch_VerbTracker_CreateVerbTargetCommand
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static Command_VerbTarget Postfix(Command_VerbTarget __result, VerbTracker __instance, Thing ownerThing, Verb verb)
        {
            if (ownerThing is ThingWithComps twc && twc.ParentHolder is Pawn_EquipmentTracker peqt)
            {
                CompEquippable ce = __instance.directOwner as CompEquippable;

                if (peqt.pawn.GetOffHander(out ThingWithComps offHandEquip))
                {
                    if (offHandEquip != twc)
                    {
                        return CreateDualWieldCommand(ownerThing, offHandEquip, verb);
                    }
                }
            }
            return __result;
        }
        static Command_VerbTarget CreateVerbTargetCommand(VerbTracker __instance, Thing ownerThing, Verb verb)
        {
            Command_VerbTarget command_VerbTarget = new Command_VerbTarget()
            {
                defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst(),
                icon = ownerThing.def.uiIcon,
                iconAngle = ownerThing.def.uiIconAngle,
                iconOffset = ownerThing.def.uiIconOffset,
                tutorTag = "VerbTarget",
                verb = verb
            };
            if (verb.caster.Faction != Faction.OfPlayer)
            {
                command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
            }
            else if (verb.CasterPawn is Pawn casterPawn)
            {
                if (casterPawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    command_VerbTarget.Disable("IsIncapableOfViolence".Translate(casterPawn.LabelShort, casterPawn));
                }
                else if (!casterPawn.drafter.Drafted)
                {
                    command_VerbTarget.Disable("IsNotDrafted".Translate(casterPawn.LabelShort, casterPawn));
                }
            }
            return command_VerbTarget;
        }
        static Command_VerbTarget CreateDualWieldCommand(Thing ownerThing, Thing offHandThing, Verb verb)
        {
            Command_DualWield command_VerbTarget = new Command_DualWield(offHandThing)
            {
                defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst(),
                icon = ownerThing.def.uiIcon,
                iconAngle = ownerThing.def.uiIconAngle,
                iconOffset = ownerThing.def.uiIconOffset,
                tutorTag = "VerbTarget",
                verb = verb
            };
            if (verb.caster.Faction != Faction.OfPlayer)
            {
                command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
            }
            else if (verb.CasterPawn is Pawn casterPawn)
            {
                if (casterPawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    command_VerbTarget.Disable("IsIncapableOfViolence".Translate(casterPawn.LabelShort, casterPawn));
                }
                else if (!casterPawn.drafter.Drafted)
                {
                    command_VerbTarget.Disable("IsNotDrafted".Translate(casterPawn.LabelShort, casterPawn));
                }
            }
            return command_VerbTarget;
        }
    }
    [HarmonyPatch(typeof(VerbTracker), nameof(VerbTracker.GetVerbsCommands))]
    class Patch_VerbTracker_GetVerbsCommands_Postfix
    {
        static IEnumerable<Command> Postfix(IEnumerable<Command> values, VerbTracker __instance)
        {
            foreach (Command command in values)
            {
                if (command is Command_VerbTarget cVerbTarget)
                {
                    Verb verb = cVerbTarget.verb;

                    if (verb.EquipmentSource is ThingWithComps twc && twc.ParentHolder is Pawn_EquipmentTracker peqt)
                    {
                        //Remove offHand gizmo when dual wielding
                        //Don't remove offHand gizmo when offHand weapon is the only weapon being carried by the pawn
                        //TODO: look at this closer
                        if (peqt.pawn.GetOffHander(out ThingWithComps offHandEquip) && offHandEquip == twc && offHandEquip != peqt.Primary)
                        {
                            continue;
                        }
                    }
                }
                yield return command;
            }
        }
    }
}