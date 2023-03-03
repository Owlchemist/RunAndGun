using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
	[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
	class Patch_PawnRenderer_RenderPawnAt
	{
		static bool Prepare()
		{
			return Settings.dualWieldEnabled;
		}
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			bool found = false;
			var method = AccessTools.Method(typeof(Pawn_RopeTracker), nameof(Pawn_RopeTracker.RopingDraw));
			foreach (var instruction in instructions)
			{
				yield return instruction;
				if (!found && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(method))
				{
					found = true;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderer), nameof(PawnRenderer.pawn)));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StorageUtility), nameof(StorageUtility.DrawOffHandStance)));
				}
			}
			if (!found) Log.Error("[Tacticowl] Patch_PawnRenderer_RenderPawnAt transpiler failed to find its target. Did RimWorld update?");
		}
	}
	[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming))]
	class Patch_PawnRenderer_DrawEquipmentAiming
	{
		static bool Prepare()
		{
			return Settings.dualWieldEnabled;
		}
		static bool Prefix(PawnRenderer __instance, Thing eq, Vector3 drawLoc, float aimAngle)
		{
			Pawn pawn = __instance.pawn;
			if (!pawn.GetOffHander(out ThingWithComps offHandEquip))
			{
				return true;
			}
			
			float mainHandAngle = aimAngle;
			float offHandAngle = aimAngle;
			Stance_Busy mainStance = pawn.stances.curStance as Stance_Busy;
			
			LocalTargetInfo focusTarg;
			bool mainHandAiming = false;
			bool offHandAiming = false;
			if (mainStance != null && !mainStance.neverAimWeapon)
			{
				focusTarg = mainStance.focusTarg;
				mainHandAiming = CurrentlyAiming(mainStance);
			}
			else if (pawn.GetOffHandStance() is Stance_Busy offHandStance && !offHandStance.neverAimWeapon)
			{
				focusTarg = offHandStance.focusTarg;
				offHandAiming = CurrentlyAiming(offHandStance);
			}
			else focusTarg = null;

			//When wielding offHand weapon, facing south, and not aiming, draw differently 
			SetAnglesAndOffsets(eq, offHandEquip, pawn, out Vector3 offsetMainHand, out Vector3 offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);

			if (offHandEquip != pawn.equipment.Primary) DrawEquipmentAimingOverride(eq, drawLoc + offsetMainHand, mainHandAngle);
			if ((offHandAiming || mainHandAiming) && focusTarg != null)
			{
				offHandAngle = GetAimingRotation(pawn, focusTarg);
				offsetOffHand.y += 0.1f;
				Vector3 adjustedDrawPos = pawn.DrawPos + new Vector3(0f, 0f, 0.4f).RotatedBy(offHandAngle) + offsetOffHand;
				DrawEquipmentAimingOverride(offHandEquip, adjustedDrawPos, offHandAngle);
			}
			else DrawEquipmentAimingOverride(offHandEquip, drawLoc + offsetOffHand, offHandAngle);
			return false;      
		}

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
			Material matSingle = graphic_StackCount != null ? graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle : eq.Graphic.MatSingle;

			Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);
		}
		static void SetAnglesAndOffsets(Thing eq, ThingWithComps offHandEquip, Pawn pawn, out Vector3 offsetMainHand, out Vector3 offsetOffHand, ref float mainHandAngle, ref float offHandAngle, bool mainHandAiming, bool offHandAiming)
		{
			var pawnRotation = pawn.Rotation;
			if (pawnRotation.IsHorizontal)
			{
				if (pawnRotation.rotInt == 1) //East
				{
					offsetMainHand = Vector3.zero;
					offsetOffHand = new Vector3(0f, -1f, 0.1f);
				}
				else //West
				{
					offsetMainHand = new Vector3(0f, -1f, 0f);
					offsetOffHand = new Vector3(0f, 0f, -0.1f);
				}

				if (Settings.customRotationsCache.TryGetValue(offHandEquip.def.shortHash, out int extraRotation))
				offHandAngle += pawnRotation == Rot4.North ? extraRotation : -extraRotation;

				if (Settings.customRotationsCache.TryGetValue(eq.def.shortHash, out extraRotation))
					mainHandAngle += pawnRotation == Rot4.North ? -extraRotation : extraRotation;
			}
			else
			{
				bool offHandIsMelee = offHandEquip.def.IsMeleeWeapon;
				bool mainHandIsMelee = pawn.equipment.Primary.def.IsMeleeWeapon;
				float meleeAngleFlipped = Settings.meleeMirrored ? 360 - Settings.meleeAngle : Settings.meleeAngle;
				float rangedAngleFlipped = Settings.rangedMirrored ? 360 - Settings.rangedAngle : Settings.rangedAngle;

				if (pawnRotation.rotInt == 0) //North
				{
					if (!mainHandAiming && !offHandAiming)
					{
						offsetMainHand = new Vector3(mainHandIsMelee ? Settings.meleeXOffset : Settings.rangedXOffset, 0f, mainHandIsMelee ? Settings.meleeZOffset : Settings.rangedZOffset);
						offsetOffHand = new Vector3(offHandIsMelee ? -Settings.meleeXOffset : -Settings.rangedXOffset, 0f, offHandIsMelee ? -Settings.meleeZOffset : -Settings.rangedZOffset);
						
						offHandAngle = offHandIsMelee ? Settings.meleeAngle : Settings.rangedAngle;
						mainHandAngle = mainHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;

					}
					else
					{
						offsetMainHand = Vector3.zero;
						offsetOffHand = new Vector3(-0.1f, 0f, 0f);
					}
				}
				else
				{
					if (!mainHandAiming && !offHandAiming)
					{
						offsetMainHand = new Vector3(mainHandIsMelee ? -Settings.meleeXOffset : -Settings.rangedXOffset, 1f, mainHandIsMelee ? -Settings.meleeZOffset : -Settings.rangedZOffset);
						offsetOffHand = new Vector3(offHandIsMelee ? Settings.meleeXOffset : Settings.rangedXOffset, 0f, offHandIsMelee ? Settings.meleeZOffset : Settings.rangedZOffset);
						
						offHandAngle = offHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;
						mainHandAngle = mainHandIsMelee ? Settings.meleeAngle : Settings.rangedAngle;
					}
					else
					{
						offsetMainHand = Vector3.zero;
						offsetOffHand = new Vector3(0.1f, 0f, 0f);
					}
				}
			}
		}
		static float GetAimingRotation(Pawn pawn, LocalTargetInfo focusTarg)
		{
			Vector3 a;
			if (focusTarg.HasThing) a = focusTarg.Thing.DrawPos;
			else a = focusTarg.Cell.ToVector3Shifted();
			
			float num;
			var drawPos = a - pawn.DrawPos;
			if (drawPos.MagnitudeHorizontalSquared() > 0.001f) num = drawPos.AngleFlat();
			else num = 0f;

			return num;
		}
		static bool CurrentlyAiming(Stance_Busy stance)
		{
			return stance != null && !stance.neverAimWeapon && stance.focusTarg.IsValid;
		}
	}
}