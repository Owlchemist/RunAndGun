﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList))]
    class Patch_Pawn_MeleeVerbs_GetUpdatedAvailableVerbsList
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(List<VerbEntry> __result)
        {
            //remove all offHand verbs so they're not used by for mainhand melee attacks.
            for (var i = __result.Count; i-- > 0;)
            {
                var ve = __result[i];
                if (ve.verb.EquipmentSource.IsOffHandedWeapon()) __result.Remove(ve);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    class Patch_Pawn_MeleeVerbs_TryMeleeAttack
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static bool Postfix(bool __result, Pawn_MeleeVerbs __instance, Thing target, Verb verbToUse, bool surpriseAttack)
        {
            Pawn pawn = __instance.pawn;
            var stance = pawn.GetOffHandStance();
            if (stance is Stance_Warmup_DW || stance is Stance_Cooldown || 
                pawn.equipment == null || !pawn.GetOffHander(out ThingWithComps offHandEquip) || 
                offHandEquip == pawn.equipment.Primary || pawn.InMentalState)
            {
                return __result;
            }

            if (__instance.Pawn.TryGetMeleeVerbOffHand(target, out Verb verb))
            {
                if (__result) return __result;
                return verb.TryStartCastOn(target);
            }
            return __result;
        }
    }
}