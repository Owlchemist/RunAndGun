using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldown))]
    [HarmonyPatch(new Type[]{typeof(Tool), typeof(Pawn), typeof(Thing)})]
    class Patch_VerbProperties_AdjustedCooldown
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(VerbProperties __instance, Thing equipment, Pawn attacker, ref float __result)
        {
            if (attacker != null && attacker.skills != null && __instance.category != VerbCategory.BeatFire)
            {
                SkillRecord skillRecord = __instance.IsMeleeAttack ? attacker.skills.GetSkill(SkillDefOf.Melee) : attacker.skills.GetSkill(SkillDefOf.Shooting);
                if (skillRecord == null)
                {
                    return;
                }
                if (equipment != null && equipment is ThingWithComps twc && twc.IsOffHandedWeapon())
                {
                    __result = CalcCooldownPenalty(__result, skillRecord, Settings.staticCooldownPOffHand / 100f);
                }
                else if (attacker.equipment != null && attacker.GetOffHander(out ThingWithComps offHandEq))
                {
                    __result = CalcCooldownPenalty(__result, skillRecord, Settings.staticCooldownPMainHand / 100f);
                }
            }
        }
        static float CalcCooldownPenalty(float __result, SkillRecord skillRecord, float staticPenalty)
        {
            float perLevelPenalty = Settings.dynamicCooldownP / 100f;
            int levelsShort = 20 - skillRecord.levelInt;
            float dynamicPenalty = perLevelPenalty * levelsShort;
            __result *= 1.0f + staticPenalty + dynamicPenalty;
            return __result;
        }
    }
}