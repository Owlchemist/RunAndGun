using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;
using Tacticowl;

namespace Tacticowl.DualWield
{
    class JobDriver_EquipOffHand : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo targetA = this.job.targetA;
            Job job = this.job;
            return pawn.Reserve(targetA, job, 1, -1, null, errorOnFailed);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    ThingWithComps thingWithComps = (ThingWithComps)this.job.targetA.Thing;
                    ThingWithComps offHandWeapon;
                    if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
                    {
                        offHandWeapon = (ThingWithComps)thingWithComps.SplitOff(1);
                    }
                    else
                    {
                        offHandWeapon = thingWithComps;
                        offHandWeapon.DeSpawn(DestroyMode.Vanish);
                    }
                    offHandWeapon.SetWeaponAsOffHanded(true);
                    pawn.equipment.MakeRoomFor(offHandWeapon);
                    pawn.SetOffHander(offHandWeapon);
                    if (thingWithComps.def.soundInteract != null)
                    {
                        thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(this.pawn.Position, this.pawn.Map, false));
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}