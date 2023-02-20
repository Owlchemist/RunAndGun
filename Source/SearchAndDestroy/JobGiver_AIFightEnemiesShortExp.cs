using RimWorld;
using Verse;
using Verse.AI;

namespace RunGunAndDestroy
{
    class JobGiver_AIFightEnemiesShortExp : JobGiver_AIFightEnemies
    {
		public override Job TryGiveJob(Pawn pawn)
		{
			var job = base.TryGiveJob(pawn);
			if(job != null)
            {
				job.expiryInterval = 30;
            }
			return job;
		}
        public override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            var targetPawn = target as Pawn;
            if(targetPawn == null)
            {
                return false;
            }
            if(targetPawn.NonHumanlikeOrWildMan() && !targetPawn.IsAttacking())
            {
                return false;
            }
            return base.ExtraTargetValidator(pawn, target);
        }
    }
}