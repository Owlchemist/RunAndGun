using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using static RunGunAndDestroy.ModSettings_RunAndDestroy;

namespace RunGunAndDestroy
{
	[StaticConstructorOnStartup]
	public static class Setup
	{
		public static ThingDef[] allWeapons;
	
		static Setup()
		{
			var harmony = new HarmonyLib.Harmony("RunGunAndDestroy");
			harmony.PatchAll();

			SetupRnG();
			SetupDW();
		}
		static void SetupRnG()
		{
			heavyWeaponsCache = new HashSet<ushort>();
			forbiddenWeaponsCache = new HashSet<ushort>();
			if (heavyWeapons == null) heavyWeapons = new List<string>();
			if (forbiddenWeapons == null) forbiddenWeapons = new List<string>();

			var workingList = new List<ThingDef>();
			var length = DefDatabase<ThingDef>.DefCount;
			for (int i = 0; i < length; i++)
			{
				var def = DefDatabase<ThingDef>.defsList[i];
				if (def.equipmentType == EquipmentType.Primary && !def.weaponTags.NullOrEmpty<string>() && !def.destroyOnDrop && def.IsWeaponUsingProjectiles)
				{
					workingList.Add(def);
					#region Heavy weapons
					//Check weight (3.4 usually)
					bool setting = def.BaseMass > weightLimitFilterDefault;
					//Check for player rule inversion
					if (heavyWeapons.Contains(def.defName)) setting = !setting;
					//Add if true
					if (setting) heavyWeaponsCache.Add(def.shortHash);
					#endregion

					#region Forbidden weapons
					//Check for extension
					setting = def.HasModExtension<WeaponForbidden>();
					//Check for player rule inversion
					if (forbiddenWeapons.Contains(def.defName)) setting = !setting;
					//Add if true
					if (setting) forbiddenWeaponsCache.Add(def.shortHash);
					#endregion
				}
			}
			workingList.SortBy(x => x.label);
			allWeapons = workingList.ToArray();
		}
		static void SetupDW()
		{
			dualWieldSelectionCache = new HashSet<ushort>();
			twoHandSelectionCache = new HashSet<ushort>();
			if (dualWieldSelection == null) dualWieldSelection = new List<string>();
			if (twoHandSelection == null) twoHandSelection = new List<string>();

			customRotationsCache = new Dictionary<ushort, float>();
			if (customRotations == null) customRotations = new Dictionary<string, float>();
		}
		public static void CheckInvesions()
		{
			//Reset
			heavyWeapons = new List<string>();
			forbiddenWeapons = new List<string>();

			//Check for abnormalities
			foreach (var weapon in allWeapons)
			{
				if (weapon.BaseMass > weightLimitFilterDefault && !heavyWeaponsCache.Contains(weapon.shortHash)) heavyWeapons.Add(weapon.defName);
				else if (weapon.BaseMass <= weightLimitFilterDefault && heavyWeaponsCache.Contains(weapon.shortHash)) heavyWeapons.Add(weapon.defName);

				bool hasComp = weapon.HasModExtension<WeaponForbidden>();
				if (hasComp && !forbiddenWeaponsCache.Contains(weapon.shortHash)) forbiddenWeapons.Add(weapon.defName);
				else if (!hasComp && forbiddenWeaponsCache.Contains(weapon.shortHash)) forbiddenWeapons.Add(weapon.defName);
			}
		}
	}
	
	public class Mod_RunAndDestroy : Mod
	{
		public Mod_RunAndDestroy(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_RunAndDestroy>();
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			//========Setup tabs=========
			GUI.BeginGroup(inRect);
			var tabs = new List<TabRecord>();
			tabs.Add(new TabRecord("RG_Tab_RunAndGun".Translate(), delegate { selectedTab = SelectedTab.runAndGun; }, selectedTab == SelectedTab.runAndGun || selectedTab == SelectedTab.heavyWeapons || selectedTab == SelectedTab.forbiddenWeapons));
			tabs.Add(new TabRecord("RG_Tab_SearchAndDestroy".Translate(), delegate { selectedTab = SelectedTab.searchAndDestroy; }, selectedTab == SelectedTab.searchAndDestroy));
			tabs.Add(new TabRecord("RG_Tab_DualWield".Translate(), delegate { selectedTab = SelectedTab.dualWield; }, selectedTab == SelectedTab.dualWield));

			Rect rect = new Rect(0f, 32f, inRect.width, inRect.height - 32f);
			Widgets.DrawMenuSection(rect);
			TabDrawer.DrawTabs(new Rect(0f, 32f, inRect.width, Text.LineHeight), tabs);

			if (selectedTab == SelectedTab.runAndGun || selectedTab == SelectedTab.heavyWeapons || selectedTab == SelectedTab.forbiddenWeapons) DrawCore();
			else if (selectedTab == SelectedTab.dualWield) DrawDualWield();
			GUI.EndGroup();
			
			void DrawCore()
			{
				if (selectedTab == SelectedTab.runAndGun) selectedTab = SelectedTab.heavyWeapons;

				Listing_Standard options = new Listing_Standard();
				options.Begin(inRect.ContractedBy(15f));
				options.ColumnWidth = (options.listingRect.width - 30f) / 2f;

				options.CheckboxLabeled("RG_EnableRGForAI_Title".Translate(), ref enableForAI, "RG_EnableRGForAI_Description".Translate());
				options.CheckboxLabeled("RG_EnableRGForAnimals_Title".Translate(), ref enableForAnimals, "RG_EnableRGForAnimals_Description".Translate());
				
				if (enableForAI)
				{
					options.Label("RG_AccuracyPenalty_Title".Translate("0", "100", "65", Math.Round(accuracyModifier * 100).ToString()), -1f, "RG_AccuracyPenalty_Description".Translate());
					accuracyModifier = options.Slider(accuracyModifier, 0f, 1f);
				}

				options.Label("RG_AccuracyPenaltyPlayer_Title".Translate("0", "100", "65", Math.Round(accuracyModifierPlayer * 100).ToString()), -1f, "RG_AccuracyPenaltyPlayer_Description".Translate());
				accuracyModifierPlayer = options.Slider(accuracyModifierPlayer, 0f, 1f);

				options.Label("RG_AccuracyPenaltyMechs_Title".Translate("0", "100", "80", Math.Round(accuracyModifierMechs * 100).ToString()), -1f, "RG_AccuracyPenaltyMechs_Description".Translate());
				accuracyModifierMechs = options.Slider(accuracyModifierMechs, 0f, 1f);

				options.NewColumn();
				
				if (enableForAI)
				{
					options.Label("RG_EnableRGForFleeChance_Title".Translate("0", "100", "50", enableForFleeChance.ToString()), -1f, "RG_EnableRGForFleeChance_Description".Translate());
					enableForFleeChance = (int)options.Slider(enableForFleeChance, 0f, 100f);
				}

				options.Label("RG_MovementPenaltyHeavy_Title".Translate("0", "100", "30", Math.Round(movementModifierHeavy * 100).ToString()), -1f, "RG_MovementPenaltyHeavy_Description".Translate());
				movementModifierHeavy = options.Slider(movementModifierHeavy, 0f, 1f);

				options.Label("RG_MovementPenaltyLight_Title".Translate("0", "100", "65", Math.Round(movementModifierLight * 100).ToString()), -1f, "RG_MovementPenaltyLight_Description".Translate());
				movementModifierLight = options.Slider(movementModifierLight, 0f, 1f);
				
				//Record positioning before closing out the lister...
				Rect weaponsFilterRect = inRect.ContractedBy(15f);
				weaponsFilterRect.y = options.curY + 130f;
				weaponsFilterRect.height = inRect.height - options.curY - 135f; //Use remaining space

				options.ColumnWidth = options.listingRect.width - 30f;
				options.End();

				//========Setup tabs=========
				tabs = new List<TabRecord>();
				tabs.Add(new TabRecord("RG_Tab_HeavyWeapons".Translate(), delegate { selectedTab = SelectedTab.heavyWeapons; }, selectedTab == SelectedTab.heavyWeapons));
				tabs.Add(new TabRecord("RG_Tab_ForbiddenWeapons".Translate(), delegate { selectedTab = SelectedTab.forbiddenWeapons; }, selectedTab == SelectedTab.forbiddenWeapons));
				
				Widgets.DrawMenuSection(weaponsFilterRect); //Used to make the background light grey with white border
				TabDrawer.DrawTabs(new Rect(weaponsFilterRect.x, weaponsFilterRect.y, weaponsFilterRect.width, Text.LineHeight), tabs);

				//========Between tabs and scroll body=========
				options.Begin(new Rect (weaponsFilterRect.x + 10, weaponsFilterRect.y + 10, weaponsFilterRect.width - 10f, weaponsFilterRect.height - 10f));
					if (selectedTab == SelectedTab.heavyWeapons)
					{
						options.Label("RG_WeightLimitFilter_Title".Translate("3.4", weightLimitFilter.ToString()), -1f, "RG_WeightLimitFilter_Description".Translate());
						weightLimitFilter = options.Slider((float)Math.Round(weightLimitFilter, 1), 0f, 10f);
					}
					else
					{
						options.Label("RG_Forbidden_Weapons".Translate());
					}
				options.End();
				//========Scroll area=========
				weaponsFilterRect.y += 60f;
				weaponsFilterRect.yMax -= 60f;
				Rect weaponsFilterInnerRect = new Rect(0f, 0f, weaponsFilterRect.width - 30f, (OptionsDrawUtility.lineNumber + 2) * 22f);
				Widgets.BeginScrollView(weaponsFilterRect, ref scrollPos, weaponsFilterInnerRect , true);
					options.Begin(weaponsFilterInnerRect);
					options.DrawList(inRect);
					options.End();
				Widgets.EndScrollView();   
			}
			void DrawDualWield()
			{

			}
		}
		public override string SettingsCategory()
		{
			return "Run Gun and Destroy";
		}
		public override void WriteSettings()
		{            
			try
			{
				Setup.CheckInvesions();
			}
			catch (System.Exception ex)
			{
				Log.Error("[Run and Destroy] Error writing Run and Destroy settings. Skipping...\n" + ex);   
			}
			base.WriteSettings();
		}   
	}
	public class ModSettings_RunAndDestroy : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look(ref enableForAI, "enableForAI", true);
			Scribe_Values.Look(ref enableForAnimals, "enableForAnimals");
			Scribe_Values.Look(ref enableForFleeChance, "enableForFleeChance", 50);
			Scribe_Values.Look(ref accuracyModifier, "accuracyPenalty", 0.65f);
			Scribe_Values.Look(ref accuracyModifierPlayer, "accuracyPenaltyPlayer", 0.65f);
			Scribe_Values.Look(ref accuracyModifierMechs, "accuracyModifierMechs", 0.8f);
			Scribe_Values.Look(ref movementModifierHeavy, "movementModifierHeavy", 0.3f);
			Scribe_Values.Look(ref movementModifierLight, "movementModifierLight", 0.65f);
			Scribe_Collections.Look(ref heavyWeapons, "heavyWeapons", LookMode.Value);
			Scribe_Collections.Look(ref forbiddenWeapons, "forbiddenWeapons", LookMode.Value);
			
			base.ExposeData();
		}
		public static bool enableForAI = true, enableForAnimals, runAndGunEnabled;
		public static int enableForFleeChance = 50;
		public static float accuracyModifier = 0.65f,
			accuracyModifierPlayer = 0.65f,
			accuracyModifierMechs = 0.8f,
			movementModifierHeavy = 0.3f,
			movementModifierLight = 0.65f;
        public static HashSet<ushort> heavyWeaponsCache,
			 forbiddenWeaponsCache,
			 dualWieldSelectionCache,
			 twoHandSelectionCache;
		public static Dictionary<ushort, float> customRotationsCache;
		public static List<string> heavyWeapons,
			 forbiddenWeapons,
			 dualWieldSelection,
			 twoHandSelection;
		public static Dictionary<string, float> customRotations;
        
		#region settings UI
		public static float weightLimitFilter = weightLimitFilterDefault;
		public const float weightLimitFilterDefault = 3.4f;
		public static Vector2 scrollPos;
		public static SelectedTab selectedTab = SelectedTab.runAndGun;
		public enum SelectedTab { runAndGun, heavyWeapons, forbiddenWeapons, searchAndDestroy, dualWield };
		#endregion
	
		#region dual wield
		public static bool settingsGroup_Drawing,
			settingsGroup_DualWield,
			settingsGroup_TwoHand,
			settingsGroup_Penalties,
			meleeMirrored,
			rangedMirrored;

        public static float staticCooldownPOffHand,
			staticCooldownPMainHand,
			staticAccPOffHand,
			staticAccPMainHand,
			dynamicCooldownP,
			dynamicAccP,
			meleeAngle,
			rangedAngle,
			meleeXOffset,
			rangedXOffset,
			meleeZOffset,
			rangedZOffset;

        public static int NPCDualWieldChance;
		#endregion
	}
}