using Verse;
using Verse.AI;

namespace Tacticowl
{
    public class ThinkNode_ConditionalSearchAndDestroy : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            return pawn.Drafted && pawn.SearchesAndDestroys();
        }
    }
}