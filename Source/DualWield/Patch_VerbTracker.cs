using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Settings = SumGunFun.ModSettings_SumGunFun;

namespace SumGunFun.DualWield
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

                if (peqt.pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
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
            Command_VerbTarget command_VerbTarget = new Command_VerbTarget();
            command_VerbTarget.defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst();
            command_VerbTarget.icon = ownerThing.def.uiIcon;
            command_VerbTarget.iconAngle = ownerThing.def.uiIconAngle;
            command_VerbTarget.iconOffset = ownerThing.def.uiIconOffset;
            command_VerbTarget.tutorTag = "VerbTarget";
            command_VerbTarget.verb = verb;
            if (verb.caster.Faction != Faction.OfPlayer)
            {
                command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
            }
            else if (verb.CasterIsPawn)
            {
                if (verb.CasterPawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    command_VerbTarget.Disable("IsIncapableOfViolence".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                }
                else if (!verb.CasterPawn.drafter.Drafted)
                {
                    command_VerbTarget.Disable("IsNotDrafted".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                }
            }
            return command_VerbTarget;
        }
        static Command_VerbTarget CreateDualWieldCommand(Thing ownerThing, Thing offHandThing, Verb verb)
        {
            Command_DualWield command_VerbTarget = new Command_DualWield(offHandThing);
            command_VerbTarget.defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst();
            command_VerbTarget.icon = ownerThing.def.uiIcon;
            command_VerbTarget.iconAngle = ownerThing.def.uiIconAngle;
            command_VerbTarget.iconOffset = ownerThing.def.uiIconOffset;
            command_VerbTarget.tutorTag = "VerbTarget";
            command_VerbTarget.verb = verb;
            if (verb.caster.Faction != Faction.OfPlayer)
            {
                command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
            }
            else if (verb.CasterIsPawn)
            {
                if (verb.CasterPawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
                {
                    command_VerbTarget.Disable("IsIncapableOfViolence".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                }
                else if (!verb.CasterPawn.drafter.Drafted)
                {
                    command_VerbTarget.Disable("IsNotDrafted".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
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
                        //Remove offhand gizmo when dual wielding
                        //Don't remove offhand gizmo when offhand weapon is the only weapon being carried by the pawn
                        if (peqt.pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip) && offHandEquip == twc && offHandEquip != peqt.Primary)
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