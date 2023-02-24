using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RunGunAndDestroy.DualWield
{
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList))]
    class Patch_Pawn_MeleeVerbs_GetUpdatedAvailableVerbsList
    {
        static void Postfix(List<VerbEntry> __result)
        {
            //remove all offhand verbs so they're not used by for mainhand melee attacks.
            for (var i = __result.Count; i-- > 0;)
            {
                var ve = __result[i];
                if (ve.verb.EquipmentSource.IsOffHand()) __result.Remove(ve);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    class Patch_Pawn_MeleeVerbs_TryMeleeAttack
    {
        static void Postfix(Pawn_MeleeVerbs __instance, Thing target, Verb verbToUse, bool surpriseAttack, ref bool __result, ref Pawn ___pawn)
        {
            var stance = ___pawn.GetStancesOffHand();
            if (stance is Stance_Warmup_DW || stance is Stance_Cooldown) return;
            if (___pawn.equipment == null || !___pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                return;
            }
            if (offHandEquip == ___pawn.equipment.Primary)
            {
                return;
            }
            if (___pawn.InMentalState)
            {
                return;
            }

            Verb verb = __instance.Pawn.TryGetMeleeVerbOffHand(target);
            if (verb != null)
            {
                bool success = verb.OffhandTryStartCastOn(target);
                __result = __result || (verb != null && success);
            }
        }
    }
}
