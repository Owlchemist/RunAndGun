using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace RunGunAndDestroy.DualWield
{

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Launch))]
    [HarmonyPatch(new Type[] { typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef) })]
    static class Patch_Projectile_Launch
    {
        static void Prefix(ref Thing launcher, ref Vector3 origin, Thing equipment)
        {
            if (!(launcher is Pawn pawn) || !(equipment is ThingWithComps twc)) return;
            
            float zOffset = 0.0f;
            float xOffset = 0.0f;

            if (launcher.Rotation == Rot4.East) zOffset = 0.1f;
            else if (launcher.Rotation == Rot4.West) zOffset = -0.1f;
            else if (launcher.Rotation == Rot4.South) xOffset = 0.1f;
            else xOffset = -0.1f;

            if (twc.IsOffHand()) origin += new Vector3(xOffset, 0, zOffset);
        }
    }
}