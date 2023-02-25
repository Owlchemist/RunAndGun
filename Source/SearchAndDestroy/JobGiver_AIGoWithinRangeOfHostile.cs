using RimWorld;
using Verse;
using Verse.AI;

namespace SumGunFun
{
    class JobGiver_GoWithinRangeOfHostile : JobGiver_AIGotoNearestHostile
	{
		public override Job TryGiveJob(Pawn pawn)
		{
			var job = base.TryGiveJob(pawn);
			if (job != null)
            {
				job.expiryInterval = 30;
            }
			return job;
		}
	}
}
