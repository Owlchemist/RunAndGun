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
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DualWieldExtensions), nameof(DualWieldExtensions.DrawOffHandStance)));
				}
			}
			if (!found) Log.Error("[Tacticowl] Patch_PawnRenderer_RenderPawnAt transpiler failed to find its target. Did RimWorld update?");
		}
	}
	[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipment))]
	class Patch_PawnRenderer_DrawEquipmentAiming
	{
		static bool Prepare()
		{
			return Settings.dualWieldEnabled;
		}
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming)),
				AccessTools.Method(typeof(Patch_PawnRenderer_DrawEquipmentAiming), nameof(Patch_PawnRenderer_DrawEquipmentAiming.DrawBothEquipmentAiming)));
        }
		public static void DrawBothEquipmentAiming(PawnRenderer __instance, Thing eq, Vector3 drawLoc, float aimAngle)
		{
			Pawn pawn = __instance.pawn;
			if (!pawn.GetOffHander(out ThingWithComps offHandEquip))
			{
				__instance.DrawEquipmentAiming(eq, drawLoc, aimAngle);
				return;
			}
			
			LocalTargetInfo focusTarg = null;
			float mainHandAngle = aimAngle, offHandAngle = aimAngle;
			bool mainHandAiming = false, offHandAiming = false;
			if (pawn.stances.curStance is Stance_Busy mainStance && !mainStance.neverAimWeapon)
			{
				focusTarg = mainStance.focusTarg;
				mainHandAiming = CurrentlyAiming(mainStance);
			}

			if (pawn.GetOffHandStance() is Stance_Busy offHandStance && !offHandStance.neverAimWeapon)
			{
				focusTarg = offHandStance.focusTarg;
				offHandAiming = CurrentlyAiming(offHandStance);
			}

			//When wielding offHand weapon, facing south, and not aiming, draw differently 
			SetAnglesAndOffsets(eq, offHandEquip, pawn, out Vector3 offsetMainHand, out Vector3 offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);

			//This draws the main hand weapon
			if (mainHandAiming || !offHandAiming) __instance.DrawEquipmentAiming(eq, drawLoc + offsetMainHand, mainHandAngle);
			else if (offHandAiming)
			{
				//TODO: This all needs to be cleaned up. It's a copy-paste of the PawnRenderer.DrawEquipment math. Probably a better way to do this
				var num = GetAimingRotation(pawn, focusTarg);
				float equipmentDrawDistanceFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
				var vector = pawn.DrawPos + new Vector3(0f, 0.1f, 0.4f + pawn.equipment.Primary.def.equippedDistanceOffset).RotatedBy(num) * equipmentDrawDistanceFactor;
				__instance.DrawEquipmentAiming(eq, vector, num);
			}
			

			//This draws the offhand while aiming
			if ((offHandAiming || mainHandAiming) && focusTarg != null)
			{
				offHandAngle = GetAimingRotation(pawn, focusTarg);
				Vector3 adjustedDrawPos = pawn.DrawPos + new Vector3(0f, 0.1f, 0.4f).RotatedBy(offHandAngle) + offsetOffHand;
				__instance.DrawEquipmentAiming(offHandEquip, adjustedDrawPos, offHandAngle);
			}
			//This draws the offhand while not aiming
			else __instance.DrawEquipmentAiming(offHandEquip, drawLoc + offsetOffHand, offHandAngle);
		}
		
		//TODO: This should probably be refactored in some way, perhaps splitting it between offhand and main hand?
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