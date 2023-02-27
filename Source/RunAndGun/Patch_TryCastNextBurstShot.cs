using System.Collections.Generic;
using Verse;
using HarmonyLib;
using Tacticowl.DualWield;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
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
            ThingWithComps offHandEquip = stanceVerb.EquipmentSource;
            if (offHandEquip != null)
		    {
                Pawn pawn = stanceTracker.pawn;
                //Check if verb is one from a offHand weapon.
                if (Settings.dualWieldEnabled && stance.verb.EquipmentSource.IsOffHandedWeapon())
                {
                    var offhandStance = pawn.GetOffHandStanceTracker();
                    if ((offhandStance.curStance is Stance_RunAndGun || offhandStance.curStance is Stance_RunAndGun_Cooldown) && pawn.pather.Moving)
                    {
                        offhandStance.SetStance(new Stance_RunAndGun_Cooldown(stance.ticksLeft, stance.focusTarg, stanceVerb));
                    }
                    else offhandStance.SetStance(new Stance_Cooldown(stance.ticksLeft, stance.focusTarg, stance.verb));
                    return;
                }
                else if ((stanceTracker.curStance is Stance_RunAndGun || stanceTracker.curStance is Stance_RunAndGun_Cooldown) && pawn.pather.Moving)
                {
                    stanceTracker.SetStance(new Stance_RunAndGun_Cooldown(stance.ticksLeft, stance.focusTarg, stanceVerb));
                    return;
                }
            }
            stanceTracker.SetStance(stance);
        }
    }
}