using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;

namespace RunAndDestroy
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
    static class Patch_TryCastNextBurstShot
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(AccessTools.Method(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance)),
				AccessTools.Method(typeof(Patch_TryCastNextBurstShot), nameof(Patch_TryCastNextBurstShot.SetStanceRunAndGun)));
        }
        public static void SetStanceRunAndGun(Pawn_StanceTracker stanceTracker, Stance_Cooldown stance)
        {
            if (stanceTracker.pawn.equipment == null)
            {
                stanceTracker.SetStance(stance);
                return;
            }
            var stanceVerb = stance.verb;
            if (stanceTracker.pawn.equipment.Primary == stanceVerb.EquipmentSource || stanceVerb.EquipmentSource == null || stanceVerb.EquipmentSource.def.thingClass == typeof(Apparel))
            {
                var curStance = stanceTracker.curStance.GetType().Name;
                if ((curStance == nameof(Stance_RunAndGun) || curStance == nameof(Stance_RunAndGun_Cooldown)) && stanceTracker.pawn.pather.Moving)
                {
                    stanceTracker.SetStance(new Stance_RunAndGun_Cooldown(stance.ticksLeft, stance.focusTarg, stanceVerb));
                }
                else
                {
                    stanceTracker.SetStance(stance);
                }
            }
        }
    }
}