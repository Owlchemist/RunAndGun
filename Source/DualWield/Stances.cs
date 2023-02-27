using Verse;
using UnityEngine;

namespace Tacticowl.DualWield
{
    class Stance_Warmup_DW : Stance_Warmup
    {
        public override bool StanceBusy
        {
            get { return true; }
        }
        public Stance_Warmup_DW() { }
        public Stance_Warmup_DW(int ticks, LocalTargetInfo focusTarg, Verb verb) : base(ticks, focusTarg, verb) { }
        public override void StanceDraw()
        {
            if (Find.Selector.IsSelected(stanceTracker.pawn))
            {
                Pawn shooter = stanceTracker.pawn;
                LocalTargetInfo target = focusTarg;
                float facing = 0f;
                if (target.Cell != shooter.Position)
                {
                    facing = target.Thing != null ? (target.Thing.DrawPos - shooter.Position.ToVector3Shifted()).AngleFlat() : (target.Cell - shooter.Position).AngleFlat;
                }
                float zOffset = 0f;
                float xOffset = 0f;
                switch (shooter.Rotation.AsInt)
                {
                    case Rot4.EastInt: zOffset = 0.1f; break;
                    case Rot4.WestInt: zOffset = -0.1f; break;
                    case Rot4.SouthInt: xOffset = -0.1f; break;
                    default: xOffset = -0.1f; break;
                }
                GenDraw.DrawAimPieRaw(shooter.DrawPos + new Vector3(xOffset, 0.2f, zOffset), facing, (int)((float)ticksLeft * pieSizeFactor));
            }
        }
        public override void StanceTick()
        {
            base.StanceTick();
            Pawn pawn = stanceTracker.pawn;
            if (!pawn.RunsAndGuns() && pawn.pather.MovingNow)
            {
                pawn.GetOffHandStanceTracker().SetStance(new Stance_Mobile());
            }
        }
        public override void Expire()
        {
            this.verb.WarmupComplete();
            if (stanceTracker.curStance == this)
            {
                stanceTracker.pawn.GetOffHandStanceTracker().SetStance(new Stance_Mobile());
            }
        }
    }
}