using HarmonyLib;
using UnityEngine;
using Verse;
using Settings = SumGunFun.ModSettings_SumGunFun;

namespace SumGunFun.DualWield
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    class Patch_PawnRenderer_RenderPawnAt
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static void Postfix(Pawn ___pawn)
        {
            if (___pawn.Spawned && !___pawn.Dead) ___pawn.GetStanceeTrackerOffHand().StanceTrackerDraw();
        }

    }
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming))]
    class Patch_PawnRenderer_DrawEquipmentAiming
    {
        static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
        static bool Prefix(PawnRenderer __instance, Thing eq, ref Vector3 drawLoc, ref float aimAngle, ref Pawn ___pawn)
        {
            ThingWithComps offHandEquip = null;
            if (___pawn.equipment == null)
            {
                return true;
            }
            if (___pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
            {
                offHandEquip = result;
            }
            if (offHandEquip == null)
            {
                return true;
            }
            float mainHandAngle = aimAngle;
            float offHandAngle = aimAngle;
            Stance_Busy mainStance = ___pawn.stances.curStance as Stance_Busy;
            Stance_Busy offHandStance = null;
            if (___pawn.GetStancesOffHand() != null)
            {
                offHandStance = ___pawn.GetStancesOffHand() as Stance_Busy;
            }
            LocalTargetInfo focusTarg = null;
            if (mainStance != null && !mainStance.neverAimWeapon)
            {
                focusTarg = mainStance.focusTarg;
            }
            else if (offHandStance != null && !offHandStance.neverAimWeapon)
            {
                focusTarg = offHandStance.focusTarg;
            }

            bool mainHandAiming = CurrentlyAiming(mainStance);
            bool offHandAiming = CurrentlyAiming(offHandStance);

            Vector3 offsetMainHand = new Vector3();
            Vector3 offsetOffHand = new Vector3();
            //When wielding offhand weapon, facing south, and not aiming, draw differently 

            SetAnglesAndOffsets(eq, offHandEquip, aimAngle, ___pawn, ref offsetMainHand, ref offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);

            if (offHandEquip != ___pawn.equipment.Primary)
            {
                DrawEquipmentAimingOverride(eq, drawLoc + offsetMainHand, mainHandAngle);
            }
            if ((offHandAiming || mainHandAiming) && focusTarg != null)
            {
                offHandAngle = GetAimingRotation(___pawn, focusTarg);
                offsetOffHand.y += 0.1f;
                Vector3 adjustedDrawPos = ___pawn.DrawPos + new Vector3(0f, 0f, 0.4f).RotatedBy(offHandAngle) + offsetOffHand;
                DrawEquipmentAimingOverride(offHandEquip, adjustedDrawPos, offHandAngle);
            }
            else
            {
                DrawEquipmentAimingOverride(offHandEquip, drawLoc + offsetOffHand, offHandAngle);
            }
            return false;      
        }

        //Copied from vanilla. 
        static void DrawEquipmentAimingOverride(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            float num = aimAngle - 90f;
            Mesh mesh;
            if (aimAngle > 20f && aimAngle < 160f)
            {
                mesh = MeshPool.plane10;
                num += eq.def.equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                mesh = MeshPool.plane10Flip;
                num -= 180f;
                num -= eq.def.equippedAngleOffset;
            }
            else
            {
                mesh = MeshPool.plane10;
                num += eq.def.equippedAngleOffset;
            }
            num %= 360f;
            Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
            Material matSingle;
            if (graphic_StackCount != null)
            {
                matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
            }
            else
            {
                matSingle = eq.Graphic.MatSingle;
            }
            Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);
        }

        static void SetAnglesAndOffsets(Thing eq, ThingWithComps offHandEquip, float aimAngle, Pawn pawn, ref Vector3 offsetMainHand, ref Vector3 offsetOffHand, ref float mainHandAngle, ref float offHandAngle, bool mainHandAiming, bool offHandAiming)
        {
            bool offHandIsMelee = IsMeleeWeapon(offHandEquip);
            bool mainHandIsMelee = IsMeleeWeapon(pawn.equipment.Primary);
            float meleeAngleFlipped = Settings.meleeMirrored ? 360 - Settings.meleeAngle : Settings.meleeAngle;
            float rangedAngleFlipped = Settings.rangedMirrored ? 360 - Settings.rangedAngle : Settings.rangedAngle;

            if (pawn.Rotation == Rot4.East)
            {
                offsetOffHand.y = -1f;
                offsetOffHand.z = 0.1f;
            }
            else if (pawn.Rotation == Rot4.West)
            {
                offsetMainHand.y = -1f;
                //zOffsetMain = 0.25f;
                offsetOffHand.z = -0.1f;
            }
            else if (pawn.Rotation == Rot4.North)
            {
                if (!mainHandAiming && !offHandAiming)
                {
                    offsetMainHand.x = mainHandIsMelee ? Settings.meleeXOffset : Settings.rangedXOffset;
                    offsetOffHand.x = offHandIsMelee ? -Settings.meleeXOffset : -Settings.rangedXOffset;
                    offsetMainHand.z = mainHandIsMelee ? Settings.meleeZOffset : Settings.rangedZOffset;
                    offsetOffHand.z = offHandIsMelee ? -Settings.meleeZOffset : -Settings.rangedZOffset;
                    offHandAngle = offHandIsMelee ? Settings.meleeAngle : Settings.rangedAngle;
                    mainHandAngle = mainHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;

                }
                else
                {
                    offsetOffHand.x = -0.1f;
                }
            }
            else
            {
                if (!mainHandAiming && !offHandAiming)
                {
                    offsetMainHand.y = 1f;
                    offsetMainHand.x = mainHandIsMelee ? -Settings.meleeXOffset : -Settings.rangedXOffset;
                    offsetOffHand.x = offHandIsMelee ? Settings.meleeXOffset : Settings.rangedXOffset;
                    offsetMainHand.z = mainHandIsMelee ? -Settings.meleeZOffset : -Settings.rangedZOffset;
                    offsetOffHand.z = offHandIsMelee ? Settings.meleeZOffset : Settings.rangedZOffset;
                    offHandAngle = offHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;
                    mainHandAngle = mainHandIsMelee ? Settings.meleeAngle : Settings.rangedAngle;
                }
                else
                {
                    offsetOffHand.x = 0.1f;
                }
            }
            if (!pawn.Rotation.IsHorizontal)
            {
                float extraRotation;
                if (Settings.customRotationsCache.TryGetValue(offHandEquip.def.shortHash, out extraRotation))
                {
                    offHandAngle += pawn.Rotation == Rot4.North ? extraRotation : -extraRotation;
                }                
                if (Settings.customRotationsCache.TryGetValue(eq.def.shortHash, out extraRotation))
                {
                    mainHandAngle += pawn.Rotation == Rot4.North ? -extraRotation : extraRotation;
                }
            }
        }

        static float GetAimingRotation(Pawn pawn, LocalTargetInfo focusTarg)
        {
            Vector3 a;
            if (focusTarg.HasThing)
            {
                a = focusTarg.Thing.DrawPos;
            }
            else
            {
                a =focusTarg.Cell.ToVector3Shifted();
            }
            float num = 0f;
            if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (a - pawn.DrawPos).AngleFlat();
            }

            return num;
        }
        static bool CurrentlyAiming(Stance_Busy stance)
        {
            return (stance != null && !stance.neverAimWeapon && stance.focusTarg.IsValid);
        }
        static bool IsMeleeWeapon(ThingWithComps eq)
        {
            if (eq == null)
            {
                return false;
            }
            if (eq.TryGetComp<CompEquippable>() is CompEquippable ceq)
            {
                if (ceq.PrimaryVerb.IsMeleeAttack)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
