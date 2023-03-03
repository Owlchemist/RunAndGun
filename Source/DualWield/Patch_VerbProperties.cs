using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldown), new Type[] {typeof(Tool), typeof(Pawn), typeof(Thing)})]
    class Patch_VerbProperties_AdjustedCooldown
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static float Postfix(float __result, VerbProperties __instance, Thing equipment, Pawn attacker)
        {
            if (attacker != null && attacker.HasOffHand() && __instance.category != VerbCategory.BeatFire)
            {
                int skillLevel;
                if (attacker.skills == null) skillLevel = 8;
                else skillLevel = __instance.IsMeleeAttack ? attacker.skills.GetSkill(SkillDefOf.Melee).Level : attacker.skills.GetSkill(SkillDefOf.Shooting).Level;
                
                var tmp = CalcCooldownPenalty(__result, skillLevel, (DualWieldExtensions.IsOffHandedWeapon(equipment) ? Settings.staticCooldownPOffHand : Settings.staticCooldownPMainHand) / 100f);
                return tmp;
            }
            return __result;
        }
        static float CalcCooldownPenalty(float __result, int skillLevel, float staticPenalty)
        {
            float perLevelPenalty = Settings.dynamicCooldownP / 100f;
            int levelsShort = 20 - skillLevel;
            float dynamicPenalty = perLevelPenalty * levelsShort;
            __result *= 1.0f + staticPenalty + dynamicPenalty;
            return __result;
        }
    }
}