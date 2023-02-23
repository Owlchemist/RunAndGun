using Verse;
using Verse.AI;

namespace RunGunAndDestroy
{
    public class ThinkNode_ConditionalSearchAndDestroy : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            return pawn.Drafted && pawn.SearchesAndDestroys();
        }
    }
}