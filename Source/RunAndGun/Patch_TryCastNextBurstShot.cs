using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System;
using System.Reflection;
using MonoMod.Utils;

namespace RunGunAndDestroy
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
    static class Verb_TryCastNextBurstShot
    {
        public static Func<ThingWithComps, bool> isOffHand;
        static void Prepare()
        {
            Type type = AccessTools.TypeByName("DualWield.Ext_ThingWithComps");
			if (type != null && isOffHand == null)
			{
				MethodInfo methodInfo = AccessTools.Method(type, "IsOffHand", null, null);
				isOffHand = methodInfo != null ? methodInfo.CreateDelegate<Func<ThingWithComps, bool>>() : null;
			}
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(AccessTools.Method(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance)),
				AccessTools.Method(typeof(Verb_TryCastNextBurstShot), nameof(Verb_TryCastNextBurstShot.SetStanceRunAndGun)));
        }
        public static void SetStanceRunAndGun(Pawn_StanceTracker stanceTracker, Stance_Cooldown stance)
        {
            var stanceVerb = stance.verb;
            if (stanceVerb.EquipmentSource == null || isOffHand == null || !isOffHand(stanceVerb.EquipmentSource))
		    {
                var curStance = stanceTracker.curStance.GetType().Name;
                DualWield.Patch_Verb_TryCastNextBurstShot.SetStanceOffHand(stanceTracker, stance);
                if ((curStance == nameof(Stance_RunAndGun) || curStance == nameof(Stance_RunAndGun_Cooldown)) && stanceTracker.pawn.pather.Moving)
                {
                    stanceTracker.SetStance(new Stance_RunAndGun_Cooldown(stance.ticksLeft, stance.focusTarg, stanceVerb));
                }
                else stanceTracker.SetStance(stance);
            }
        }
    }
}