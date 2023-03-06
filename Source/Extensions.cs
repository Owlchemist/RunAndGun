using RimWorld;
using Verse;
using System.Runtime.CompilerServices;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
	static class DualWieldExtensions
	{
		public static bool GetTacticowlStorage(this Pawn pawn, out PawnStorage pawnStorage, bool setupIfNeeded = false)
		{
			if (!Storage._instance.store.TryGetValue(pawn, out pawnStorage))
			{
				if (!setupIfNeeded) return false;
				if (Settings.logging) Log.Message("[Tacticowl] Setting up extended data for " + pawn.Label);
				pawnStorage = new PawnStorage();
				Storage._instance.store.Add(pawn, pawnStorage);
			}
			return true;
		}
		public static bool IsOffHandedWeapon(this Thing thing)
		{
			return Storage._instance.offHands.Contains(thing);
		}
		public static void SetWeaponAsOffHanded(this Thing thing, bool set)
		{
			if (set) Storage._instance.offHands.Add(thing);
			else Storage._instance.offHands.Remove(thing);
		}
		public static bool IsTwoHanded(this Def def)
		{
			return Settings.twoHandersCache.Contains(def.shortHash);
		}
		public static bool HasTwoHander(this Pawn pawn)
		{
			return pawn.equipment?.Primary?.def.IsTwoHanded() ?? false;
		}
		public static bool CanBeOffHand(this Def def)
		{
			return Settings.offHandersCache.Contains(def.shortHash);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasOffHand(this Pawn pawn)
		{
			return pawn.equipment != null && Storage._instance.hasOffhandCache.Contains(pawn.thingIDNumber);
		}
		public static bool GetOffHander(this Pawn pawn, out ThingWithComps thing)
		{
			thing = pawn.GetTacticowlStorage(out PawnStorage pawnStorage) ? pawnStorage.offHandWeapon : null;
			return thing != null;
		}
		public static void SetOffHander(this Pawn pawn, ThingWithComps thing, bool removing = false)
		{
			pawn.GetTacticowlStorage(out PawnStorage pawnStorage, true);
			if (removing)
			{
				thing.SetWeaponAsOffHanded(false);
				Storage._instance.hasOffhandCache.Remove(pawn.thingIDNumber);
				pawnStorage.offHandWeapon = null;
				return;
			}
			pawnStorage.offHandWeapon = thing;

			thing.SetWeaponAsOffHanded(true);
			pawn.equipment.equipment.TryAdd(thing, true);

			Storage._instance.hasOffhandCache.Add(pawn.thingIDNumber);

            LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.DW_Penalties, OpportunityType.GoodToKnow);
            LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.DW_Settings, OpportunityType.GoodToKnow);
		}
		public static Stance GetOffHandStance(this Pawn pawn)
        {
            return GetOffHandStanceTracker(pawn).curStance;
        }
		public static void DrawOffHandStance(this Pawn pawn)
        {
			if (pawn.HasOffHand()) pawn.GetOffHandStanceTracker().StanceTrackerDraw();
        }
        public static Pawn_StanceTracker GetOffHandStanceTracker(this Pawn pawn)
        {
			pawn.GetTacticowlStorage(out PawnStorage pawnStorage, true);
			Pawn_StanceTracker pawn_StanceTracker;
            if (pawnStorage.stances == null)
            {
                pawn_StanceTracker = new Pawn_StanceTracker(pawn);
                pawnStorage.stances = pawn_StanceTracker;
            }
			else pawn_StanceTracker = pawnStorage.stances;
            return pawn_StanceTracker;
        }
		public static bool HasMissingArmOrHand(this Pawn instance)
        {
            var list = instance.health.hediffSet.GetMissingPartsCommonAncestors();
            for (int i = list.Count; i-- > 0;)
            {
                var partDef = list[i].Part.def;
                if (partDef == BodyPartDefOf.Hand || partDef == BodyPartDefOf.Arm) return true;
            }
            
            return false;
        }
	}
	static class RunAndGunExtensions
	{
		public static bool RunsAndGuns(this Pawn pawn)
		{
			return Storage._instance.RnG.Contains(pawn);
		}
		public static void SetRunsAndGuns(this Pawn pawn, bool set)
		{
			if (set) Storage._instance.RnG.Add(pawn);
			else Storage._instance.RnG.Remove(pawn);
		}
	}
	static class SearchAndDestroyExtensions
	{
		public static bool SearchesAndDestroys(this Pawn pawn)
		{
			return Storage._instance.SnD.Contains(pawn);
		}
		public static void SetSearchAndDestroy(this Pawn pawn, bool set)
		{
			if (set) Storage._instance.SnD.Add(pawn);
			else Storage._instance.SnD.Remove(pawn);
		}
	}
}