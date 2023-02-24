using Verse;
using UnityEngine;

namespace RunGunAndDestroy.DualWield
{
    class Stance_Cooldown_DW : Stance_Cooldown
    {
        public new const float MaxRadius = 0.5f;
        public override bool StanceBusy
        {
            get { return true; }
        }
        public Stance_Cooldown_DW() { }
        public Stance_Cooldown_DW(int ticks, LocalTargetInfo focusTarg, Verb verb) : base(ticks, focusTarg, verb) { }
    }
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
            if (Find.Selector.IsSelected(this.stanceTracker.pawn))
            {
                Pawn shooter = this.stanceTracker.pawn;
                LocalTargetInfo target = this.focusTarg;
                float facing = 0f;
                if (target.Cell != shooter.Position)
                {
                    if (target.Thing != null)
                    {
                        facing = (target.Thing.DrawPos - shooter.Position.ToVector3Shifted()).AngleFlat();
                    }
                    else
                    {
                        facing = (target.Cell - shooter.Position).AngleFlat;
                    }
                }
                float zOffSet = 0f;
                float xOffset = 0f;
                if (shooter.Rotation == Rot4.East)
                {
                    zOffSet = 0.1f;
                }
                else if (shooter.Rotation == Rot4.West)
                {
                    zOffSet = -0.1f;
                }
                else if (shooter.Rotation == Rot4.South)
                {
                    xOffset = 0.1f;
                }
                else
                {
                    xOffset = -0.1f;
                }
                GenDraw.DrawAimPieRaw(shooter.DrawPos + new Vector3(xOffset, 0.2f, zOffSet), facing, (int)((float)this.ticksLeft * this.pieSizeFactor));
            }
        }
        public override void StanceTick()
        {
            base.StanceTick();
            if (!Pawn.RunsAndGuns() && Pawn.pather.MovingNow)
            {
                this.stanceTracker.pawn.SetStancesOffHand(new Stance_Mobile());
            }
        }
        public override void Expire()
        {
            this.verb.WarmupComplete();
            if (this.stanceTracker.curStance == this)
            {
                this.stanceTracker.pawn.SetStancesOffHand(new Stance_Mobile());
            }
        }
    }
}