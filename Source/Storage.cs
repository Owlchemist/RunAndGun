using Verse;
using System.Linq;
using System.Collections.Generic;

namespace RunGunAndDestroy
{
	static class StorageUtility
	{
		public static bool RunsAndGuns(this Pawn pawn)
		{
			return RNDStorage._instance.registry.Contains(pawn);
		}
		public static void SetRunsAndGuns(this Pawn pawn, bool set)
		{
			if (set) RNDStorage._instance.registry.Add(pawn);
			else RNDStorage._instance.registry.Remove(pawn);
		}
	}
	class RNDStorage : GameComponent
	{
		public HashSet<Pawn> registry = new HashSet<Pawn>();
		public static RNDStorage _instance;
		public RNDStorage(Game game)
		{ 
			_instance = this;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref registry, "registry", LookMode.Reference);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				foreach (var pawn in registry.ToList())
				{
					if (pawn == null) registry.Remove(pawn);
				}
			}
		}
	}
}