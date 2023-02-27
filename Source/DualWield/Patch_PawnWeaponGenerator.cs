using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
    [HarmonyPatch(typeof(PawnWeaponGenerator), nameof(PawnWeaponGenerator.TryGenerateWeaponFor))]
    class Patch_PawnWeaponGenerator_TryGenerateWeaponFor
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && Rand.Chance(((float)Settings.NPCDualWieldChance)/100f))
            {
                if (pawn.equipment == null && pawn.equipment.Primary == null && pawn.equipment.Primary.def.IsTwoHanded())
                {
                    return;
                }
                float randomInRange = pawn.kindDef.weaponMoney.RandomInRange;
                var workingWeapons = PawnWeaponGenerator.workingWeapons;
                for (int i = PawnWeaponGenerator.allWeaponPairs.Count; i-- > 0;)
                {
                    ThingStuffPair weapon = PawnWeaponGenerator.allWeaponPairs[i];
                    if (weapon.Price <= randomInRange && //Can accord the price?
                        (pawn.kindDef.weaponTags == null || pawn.kindDef.weaponTags.Any(tag => weapon.thing.weaponTags.Contains(tag))) && //Valid tagging?
                        (weapon.thing.generateAllowChance >= 1f || Rand.ChanceSeeded(weapon.thing.generateAllowChance, pawn.thingIDNumber ^ (int)weapon.thing.shortHash ^ 28554824))) //Pass chance?
                    {
                        workingWeapons.Add(weapon);
                    }
                }

                if (workingWeapons.Count > 0 && workingWeapons.Where(tsp => tsp.thing.CanBeOffHand() && !tsp.thing.IsTwoHanded()).
                    TryRandomElementByWeight(w => w.Commonality * w.Price, out ThingStuffPair thingStuffPair))
                {
                    ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                    PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
                    pawn.SetOffHander(thingWithComps);
                }
            }
        }
    }
}