using System.Collections.Generic;
using Verse;
using HarmonyLib;
using SumGunFun.DualWield;

namespace SumGunFun
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
    static class Verb_TryCastNextBurstShot
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(AccessTools.Method(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance)),
				AccessTools.Method(typeof(Verb_TryCastNextBurstShot), nameof(Verb_TryCastNextBurstShot.SetStanceRunAndGun)));
        }
        public static void SetStanceRunAndGun(Pawn_StanceTracker stanceTracker, Stance_Cooldown stance)
        {
            var stanceVerb = stance.verb;
            if (stanceVerb.EquipmentSource != null)
		    {
                SetStanceOffHand(stanceTracker, stance);
                if ((stanceTracker.curStance is Stance_RunAndGun || stanceTracker.curStance is Stance_RunAndGun_Cooldown) && stanceTracker.pawn.pather.Moving)
                {
                    stanceTracker.SetStance(new Stance_RunAndGun_Cooldown(stance.ticksLeft, stance.focusTarg, stanceVerb));
                    return;
                }
            }
            stanceTracker.SetStance(stance);
        }

        public static void SetStanceOffHand(Pawn_StanceTracker stanceTracker,  Stance_Cooldown stance)
        {
            if (stance.verb.EquipmentSource.IsOffHand())
            {
                ThingWithComps offHandEquip = stance.verb.EquipmentSource;

                //Check if verb is one from a offhand weapon. 
                if (offHandEquip.GetComp<CompEquippable>() != null && offHandEquip != stanceTracker.pawn.equipment.Primary)
                {
                    stanceTracker.pawn.SetStancesOffHand(stance);
                }
            }
        }
    }
}