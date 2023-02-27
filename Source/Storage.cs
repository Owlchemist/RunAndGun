using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
	static class StorageUtility
	{
		#region Run and Gun
		public static bool RunsAndGuns(this Pawn pawn)
		{
			return Storage._instance.RnG.Contains(pawn);
		}
		public static void SetRunsAndGuns(this Pawn pawn, bool set)
		{
			if (set) Storage._instance.RnG.Add(pawn);
			else Storage._instance.RnG.Remove(pawn);
		}
		#endregion
		#region Search and Destroy
		public static bool SearchesAndDestroys(this Pawn pawn)
		{
			return Storage._instance.SnD.Contains(pawn);
		}
		public static void SetSearchAndDestroy(this Pawn pawn, bool set)
		{
			if (set) Storage._instance.SnD.Add(pawn);
			else Storage._instance.SnD.Remove(pawn);
		}
		#endregion
		#region Dual Wield
		public static bool IsOffHandedWeapon(this ThingWithComps thing)
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
		public static bool CanBeOffHand(this Def def)
		{
			return Settings.offHandersCache.Contains(def.shortHash);
		}
		public static bool GetTacticowlStorage(this Pawn pawn, out PawnStorage pawnStorage, bool setupIfNeeded = false)
		{
			if (!Storage._instance.store.TryGetValue(pawn, out pawnStorage))
			{
				if (!setupIfNeeded) return false;
				pawnStorage = new PawnStorage();
				Storage._instance.store.Add(pawn, pawnStorage);
			}
			return true;
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
		#endregion
	}
	class Storage : GameComponent
	{
		public HashSet<Pawn> RnG = new HashSet<Pawn>();
		public HashSet<Pawn> SnD = new HashSet<Pawn>();
		public HashSet<int> hasOffhandCache = new HashSet<int>();
		public HashSet<Thing> offHands = new HashSet<Thing>(); //Cache of ThingsWithComps that are currently held as an offHand
		public Dictionary<Pawn, PawnStorage> store = new Dictionary<Pawn, PawnStorage>();
		public static Storage _instance;
		public Storage(Game game)
		{ 
			_instance = this;
		}

		static List<Pawn> keysWorkingList = new List<Pawn>();
		static List<PawnStorage> valuesWorkingList = new List<PawnStorage>();
		
		public override void ExposeData()
		   {
            base.ExposeData();
			Scribe_Collections.Look(ref RnG, "RnG", LookMode.Reference);
			Scribe_Collections.Look(ref SnD, "SnD", LookMode.Reference);
			Scribe_Collections.Look(ref offHands, "offHands", LookMode.Reference);
			Scribe_Collections.Look(ref store, "store", LookMode.Reference, LookMode.Deep, ref keysWorkingList, ref valuesWorkingList);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (RnG == null) RnG = new HashSet<Pawn>();
				if (SnD == null) SnD = new HashSet<Pawn>();
				if (offHands == null) offHands = new HashSet<Thing>();
				if (store == null) store = new Dictionary<Pawn, PawnStorage>();

				//Remove invalid keys if there
				RnG.Remove(null);
				SnD.Remove(null);
				offHands.Remove(null);
			}
		}
	}
	public class PawnStorage : IExposable
	{
		public ThingWithComps offHandWeapon;
		public Pawn_StanceTracker stances;
	
		public PawnStorage() {}

		public void ExposeData()
		{
			//No need to save pointless entries
			Scribe_References.Look(ref offHandWeapon, "offHandWeapon");

			if (stances != null)
			{
				object[] array = new object[]
				{
					stances.pawn
				};
				Scribe_Deep.Look<Pawn_StanceTracker>(ref stances, "stances", new object[]
				{
					false,
					array
				});
			}

			if (Scribe.mode == LoadSaveMode.PostLoadInit && offHandWeapon != null) Storage._instance.hasOffhandCache.Add(stances.pawn.thingIDNumber);
		}
	}
}