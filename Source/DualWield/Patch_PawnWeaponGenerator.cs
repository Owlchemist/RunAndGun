using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Settings = SumGunFun.ModSettings_SumGunFun;

namespace SumGunFun.DualWield
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
                float randomInRange = pawn.kindDef.weaponMoney.RandomInRange;
                
                if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsTwoHanded())
                {
                    return;
                }
                if (pawn.equipment == null || pawn.equipment.Primary == null)
                {
                    return;
                }
                for (int i = 0; i < PawnWeaponGenerator.allWeaponPairs.Count; i++)
                {
                    ThingStuffPair w = PawnWeaponGenerator.allWeaponPairs[i];
                    if (w.Price <= randomInRange)
                    {
                        if (pawn.kindDef.weaponTags == null || pawn.kindDef.weaponTags.Any((string tag) => w.thing.weaponTags.Contains(tag)))
                        {
                            if (w.thing.generateAllowChance >= 1f || Rand.ChanceSeeded(w.thing.generateAllowChance, pawn.thingIDNumber ^ (int)w.thing.shortHash ^ 28554824))
                            {
                                PawnWeaponGenerator.workingWeapons.Add(w);
                            }
                        }
                    }
                }
                if (PawnWeaponGenerator.workingWeapons.Count == 0)
                {
                    return;
                }
                ThingStuffPair thingStuffPair;
                IEnumerable<ThingStuffPair> matchingWeapons = PawnWeaponGenerator.workingWeapons.Where((ThingStuffPair tsp) =>
                tsp.thing.CanBeOffHand() &&
                !tsp.thing.IsTwoHanded());
                if (matchingWeapons != null && matchingWeapons.TryRandomElementByWeight((ThingStuffPair w) => w.Commonality * w.Price, out thingStuffPair))
                {
                    ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                    PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
                    pawn.equipment.AddOffHandEquipment(thingWithComps);
                }
            }
        }
    }
}