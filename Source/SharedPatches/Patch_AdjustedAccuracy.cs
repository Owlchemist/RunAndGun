using Verse;
using RimWorld;
using HarmonyLib;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedAccuracy))]
    class Patch_AdjustedAccuracy
    {
        static bool Prepare()
        {
            return Settings.runAndGunEnabled || Settings.dualWieldEnabled;
        }
        static float Postfix(float __result, VerbProperties __instance, Thing equipment)
        {
            if (equipment == null || equipment.ParentHolder is not Pawn_EquipmentTracker tracker) goto SkipAll;
            Pawn pawn = tracker.pawn;
            if (pawn == null) goto SkipAll;

            if (Settings.runAndGunEnabled)
            {
                var curStance = pawn.stances?.curStance;
                if (curStance is Stance_RunAndGun || curStance is Stance_RunAndGun_Cooldown)
                {
                    __result *= (pawn.IsColonist ? Settings.accuracyModifierPlayer : Settings.accuracyModifier);
                }
            }
            if (Settings.dualWieldEnabled)
            {
                int skillLevel;
                if (pawn.skills == null) skillLevel = 8;
                else
                {
                    SkillRecord skillRecord = __instance.IsMeleeAttack ? pawn.skills.GetSkill(SkillDefOf.Melee) : pawn.skills.GetSkill(SkillDefOf.Shooting);
                    skillLevel = skillRecord.levelInt;
                }
                var staticPenalty = (equipment.IsOffHandedWeapon() ? Settings.staticAccPOffHand : Settings.staticAccPMainHand) / 100f;
                __result *= 1.0f - (staticPenalty - ((Settings.dynamicAccP / 100f) * (20 - skillLevel)));
            }
            SkipAll:
            return __result;
        }
    }
}