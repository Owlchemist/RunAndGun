using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Settings = SumGunFun.ModSettings_SumGunFun;

namespace SumGunFun.DualWield
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
                if (equipment != null && equipment is ThingWithComps twc && twc.IsOffHand())
                {
                    __result = CalcCooldownPenalty(__result, skillRecord, Settings.staticCooldownPOffHand / 100f);
                }
                else if (attacker.equipment != null && attacker.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEq))
                {
                    __result = CalcCooldownPenalty(__result, skillRecord, Settings.staticCooldownPMainHand / 100f);
                }
            }
        }
        static float CalcCooldownPenalty(float __result, SkillRecord skillRecord, float staticPenalty)
        {
            //TODO: make mod settings
            float perLevelPenalty = Settings.dynamicCooldownP / 100f;
            int levelsShort = 20 - skillRecord.levelInt;
            float dynamicPenalty = perLevelPenalty * levelsShort;
            __result *= 1.0f + staticPenalty + dynamicPenalty;
            return __result;
        }
    }
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedAccuracy))]
    class Patch_VerbProperties_AdjustedAccuracy
    {
        static void Postfix(VerbProperties __instance, Thing equipment, ref float __result)
        {
            if (equipment != null && equipment.ParentHolder is Pawn_EquipmentTracker peqt)
            {
                Pawn pawn = peqt.pawn;
                if (pawn.skills == null)
                {
                    return;
                }
                SkillRecord skillRecord = __instance.IsMeleeAttack ? pawn.skills.GetSkill(SkillDefOf.Melee) : pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (equipment is ThingWithComps twc && twc.IsOffHand())
                {
                    __result = CalcAccuracyPenalty(__result, skillRecord, Settings.staticAccPOffHand / 100f);
                }
                else if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEq))
                {
                    __result = CalcAccuracyPenalty(__result, skillRecord, Settings.staticAccPMainHand / 100f);
                }
            }
        }

        static float CalcAccuracyPenalty(float __result, SkillRecord skillRecord, float staticPenalty)
        {
            //TODO: make mod settings
            float perLevelPenalty = Settings.dynamicAccP / 100f;
            int levelsShort = 20 - skillRecord.levelInt;
            float dynamicPenalty = perLevelPenalty * levelsShort;
            __result *= 1.0f - staticPenalty - dynamicPenalty;
            return __result;
        }
    }
}