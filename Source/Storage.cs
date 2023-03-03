using Verse;
using System.Collections.Generic;

namespace Tacticowl
{
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

		public override void FinalizeInit()
		{
			foreach (var (key, value) in store) if (value.offHandWeapon != null) hasOffhandCache.Add(key.thingIDNumber);
		}

		static List<Pawn> keysWorkingList = new List<Pawn>();
		static List<PawnStorage> valuesWorkingList = new List<PawnStorage>();
		
		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving) store.RemoveAll(x => x.Key.Destroyed);

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
		}
	}
}