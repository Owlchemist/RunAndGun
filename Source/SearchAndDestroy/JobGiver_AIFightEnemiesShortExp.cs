using RimWorld;
using Verse;
using Verse.AI;

namespace Tacticowl
{
    class JobGiver_AIFightEnemiesShortExp : JobGiver_AIFightEnemies
    {
		public override Job TryGiveJob(Pawn pawn)
		{
			var job = base.TryGiveJob(pawn);
			if (job != null) job.expiryInterval = 30;
			return job;
		}
        public override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            var targetPawn = target as Pawn;
            if (targetPawn == null || (targetPawn.NonHumanlikeOrWildMan() && !targetPawn.IsAttacking()))
            {
                return false;
            }
            return base.ExtraTargetValidator(pawn, target);
        }
    }
}