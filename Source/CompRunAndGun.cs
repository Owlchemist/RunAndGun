using Verse;
using Settings = RunAndDestroy.ModSettings_RunAndDestroy;

namespace RunAndDestroy
{
    class CompProperties_RunAndGun : CompProperties
    {
        public CompProperties_RunAndGun()
        {
            compClass = typeof(CompRunAndGun);
        }
    }
    public class CompRunAndGun : ThingComp
    {
        public bool isEnabled = false;
        Pawn pawn
        {
            get
            {
                Pawn pawn = parent as Pawn;
                if (pawn == null) Log.Error("pawn is null");
                return pawn;
            }
        }

        //This can be misused to read isEnabled from other mods without using (expensive) reflection. 
        public override string GetDescriptionPart()
        {   
            return isEnabled.ToString();
        }

        public override void CompTickRare()
        {
            if (Settings.forbiddenWeaponsCache.Contains(pawn.equipment?.Primary?.def.shortHash ?? 0))
            {
                isEnabled = false;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Pawn pawn = parent as Pawn;
            if (!pawn.IsColonist && Settings.enableForAI) isEnabled = true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isEnabled, "isEnabled");
        }
    }
}