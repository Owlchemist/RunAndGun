using Verse;
using System.Linq;
using System.Collections.Generic;

namespace RunGunAndDestroy
{
	static class StorageUtility
	{
		public static bool RunsAndGuns(this Pawn pawn)
		{
			return RNDStorage._instance.RnG.Contains(pawn);
		}
		public static void SetRunsAndGuns(this Pawn pawn, bool set)
		{
			if (set) RNDStorage._instance.RnG.Add(pawn);
			else RNDStorage._instance.RnG.Remove(pawn);
		}
		public static bool SearchesAndDestroys(this Pawn pawn)
		{
			return RNDStorage._instance.SnD.Contains(pawn);
		}
		public static void SetSearchAndDestroy(this Pawn pawn, bool set)
		{
			if (set) RNDStorage._instance.SnD.Add(pawn);
			else RNDStorage._instance.SnD.Remove(pawn);
		}
	}
	class RNDStorage : GameComponent
	{
		public HashSet<Pawn> RnG = new HashSet<Pawn>();
		public HashSet<Pawn> SnD = new HashSet<Pawn>();
		public static RNDStorage _instance;
		public RNDStorage(Game game)
		{ 
			_instance = this;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref RnG, "RnG", LookMode.Reference);
			Scribe_Collections.Look(ref SnD, "SnD", LookMode.Reference);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				foreach (var pawn in RnG.ToList())
				{
					if (pawn == null) RnG.Remove(pawn);
				}
				foreach (var pawn in SnD.ToList())
				{
					if (pawn == null) SnD.Remove(pawn);
				}
			}
		}
	}
}