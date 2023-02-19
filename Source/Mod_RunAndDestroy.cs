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
			var harmony = new HarmonyLib.Harmony("RunAndDestroy");
			harmony.PatchAll();

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

			Rect rect = new Rect(0f, 32f, inRect.width, inRect.height - 32f);
			Widgets.DrawMenuSection(rect);
			TabDrawer.DrawTabs(new Rect(0f, 32f, inRect.width, Text.LineHeight), tabs);

			if (selectedTab == SelectedTab.runAndGun || selectedTab == SelectedTab.heavyWeapons || selectedTab == SelectedTab.forbiddenWeapons) DrawCore();
			GUI.EndGroup();
			
			void DrawCore()
			{
				if (selectedTab == SelectedTab.runAndGun) selectedTab = SelectedTab.heavyWeapons;

				Listing_Standard options = new Listing_Standard();
				options.Begin(inRect.ContractedBy(15f));

				options.CheckboxLabeled("RG_EnableRGForAI_Title".Translate(), ref enableForAI, "RG_EnableRGForAI_Description".Translate());
				
				if (enableForAI)
				{
					options.Label("RG_AccuracyPenalty_Title".Translate("0", "100", "65", Math.Round(accuracyPenalty * 100).ToString()), -1f, "RG_AccuracyPenalty_Description".Translate());
					accuracyPenalty = options.Slider(accuracyPenalty, 0f, 1f);
				}

				options.Label("RG_AccuracyPenaltyPlayer_Title".Translate("0", "100", "65", Math.Round(accuracyPenalty * 100).ToString()), -1f, "RG_AccuracyPenaltyPlayer_Description".Translate());
				accuracyPenaltyPlayer = options.Slider(accuracyPenaltyPlayer, 0f, 1f);
				
				if (enableForAI)
				{
					options.Label("RG_EnableRGForFleeChance_Title".Translate("0", "100", "50", enableForFleeChance.ToString()), -1f, "RG_EnableRGForFleeChance_Description".Translate());
					enableForFleeChance = (int)options.Slider(enableForFleeChance, 0f, 100f);
				}

				options.Label("RG_MovementPenaltyHeavy_Title".Translate("0", "100", "70", movementPenaltyHeavy.ToString()), -1f, "RG_MovementPenaltyHeavy_Description".Translate());
				movementPenaltyHeavy = (int)options.Slider(movementPenaltyHeavy, 0f, 100f);

				options.Label("RG_MovementPenaltyLight_Title".Translate("0", "100", "35", movementPenaltyLight.ToString()), -1f, "RG_MovementPenaltyLight_Description".Translate());
				movementPenaltyLight = (int)options.Slider(movementPenaltyLight, 0f, 100f);
				
				//Record positioning before closing out the lister...
				Rect weaponsFilterRect = inRect.ContractedBy(15f);
				weaponsFilterRect.y = options.curY + 90f;
				weaponsFilterRect.height = inRect.height - options.curY - 105f; //Use remaining space

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
		}
		public override string SettingsCategory()
		{
			return "Run and Destroy";
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
			Scribe_Values.Look(ref accuracyPenalty, "accuracyPenalty", 0.65f);
			Scribe_Values.Look(ref accuracyPenaltyPlayer, "accuracyPenaltyPlayer", 0.65f);
			Scribe_Values.Look(ref movementPenaltyHeavy, "movementPenaltyHeavy", 70);
			Scribe_Values.Look(ref movementPenaltyLight, "movementPenaltyLight", 35);
			Scribe_Collections.Look(ref heavyWeapons, "heavyWeapons", LookMode.Value);
			Scribe_Collections.Look(ref forbiddenWeapons, "forbiddenWeapons", LookMode.Value);
			
			base.ExposeData();
		}
		public static bool enableForAI = true, enableForAnimals;
		public static int movementPenaltyHeavy = 70,
			movementPenaltyLight = 35,
			enableForFleeChance = 50;
		public static float accuracyPenalty = 0.65f,
			accuracyPenaltyPlayer = 0.65f;
        public static HashSet<ushort> heavyWeaponsCache,
			 forbiddenWeaponsCache;
		public static List<string> heavyWeapons,
			 forbiddenWeapons;
        
		#region settings UI
		public static float weightLimitFilter = weightLimitFilterDefault;
		public const float weightLimitFilterDefault = 3.4f;
		public static Vector2 scrollPos;
		public static SelectedTab selectedTab = SelectedTab.runAndGun;
		public enum SelectedTab { runAndGun, heavyWeapons, forbiddenWeapons, searchAndDestroy };
		#endregion
	}
}