using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using static Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl
{
	[StaticConstructorOnStartup]
	public static class Setup
	{
		public static ThingDef[] allWeapons;
	
		static Setup()
		{
			var harmony = new HarmonyLib.Harmony("Tacticowl");
			harmony.PatchAll();

			SetupRnG();
			SetupDW();
			SetupDWCustomRotations();
		}
		static void SetupRnG()
		{
			heavyWeaponsCache = new HashSet<ushort>();
			forbiddenWeaponsCache = new HashSet<ushort>();
			if (heavyWeapons == null) heavyWeapons = new List<string>();
			if (forbiddenWeapons == null) forbiddenWeapons = new List<string>();

			var workingList = new List<ThingDef>();
			for (int i = DefDatabase<ThingDef>.DefCount; i-- > 0;)
			{
				var def = DefDatabase<ThingDef>.defsList[i];
				if (def.equipmentType == EquipmentType.Primary && !def.weaponTags.NullOrEmpty<string>() && !def.destroyOnDrop)
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
		public static void SetupDW(bool processInversions = false)
		{
			HashSet<ushort> modifiedOffHandersCache;
			HashSet<ushort> modifiedTwoHandersCache;
			if (processInversions)
			{
				modifiedOffHandersCache = new HashSet<ushort>(offHandersCache);
				modifiedTwoHandersCache = new HashSet<ushort>(twoHandersCache);
			}
			else modifiedOffHandersCache = modifiedTwoHandersCache = null;

			offHandersCache = new HashSet<ushort>();
			twoHandersCache = new HashSet<ushort>();
			if (offHanders == null) offHanders = new List<string>();
			if (twoHanders == null) twoHanders = new List<string>();

			for (int i = allWeapons.Length; i-- > 0;)
			{
				var def = allWeapons[i];
				var shortHash = def.shortHash;
				
				//Process extensions
				var modExtensions = def.modExtensions;
				if (modExtensions != null)
				{
					for (int j = modExtensions.Count; j-- > 0;)
					{
						var ext = modExtensions[j];
						if (ext is OffHander) offHandersCache.Add(shortHash);
						else if (ext is TwoHander || ext is WeaponForbidden) twoHandersCache.Add(shortHash);
					}
				}
				
				//Sync with run n gun light weapons
				if (!heavyWeaponsCache.Contains(shortHash)) offHandersCache.Add(shortHash);
				else twoHandersCache.Add(shortHash);
				
				if (twoHandersCache.Contains(shortHash)) offHandersCache.Remove(shortHash);
				
				//Process inversions
				if (processInversions)
				{
					if (modifiedOffHandersCache.Contains(shortHash) != offHandersCache.Contains(shortHash)) offHanders.Add(def.defName);
					else if (offHanders.Contains(def.defName)) offHanders.Remove(def.defName);

					if (modifiedTwoHandersCache.Contains(shortHash) != twoHandersCache.Contains(shortHash)) twoHanders.Add(def.defName);
					else if (twoHanders.Contains(def.defName)) twoHanders.Remove(def.defName);
				}
				else
				{
					if (offHanders.Contains(def.defName))
					{
						if (offHandersCache.Contains(shortHash)) offHandersCache.Remove(shortHash);
						else offHandersCache.Add(shortHash);
					}
					if (twoHanders.Contains(def.defName))
					{
						if (twoHandersCache.Contains(shortHash)) twoHandersCache.Remove(shortHash);
						else twoHandersCache.Add(shortHash);
					}
				}
			}

			//Keep modified, no need to re-apply inversions
			if (processInversions)
			{
				offHandersCache = modifiedOffHandersCache;
				twoHandersCache = modifiedTwoHandersCache;
			}
		}
		static void SetupDWCustomRotations()
		{
			customRotationsCache = new Dictionary<ushort, int>();
			if (customRotations == null) customRotations = new Dictionary<string, int>();

			for (int i = allWeapons.Length; i-- > 0;)
			{
				var def = allWeapons[i];
				var modExt = def.GetModExtension<CustomRotation>();
				
				int extraRotation1 = 0;
				if (modExt != null) extraRotation1 = modExt.extraRotation;
				customRotations.TryGetValue(def.defName, out int extraRotation2);
				
				int extraRotation = extraRotation1 + extraRotation2;
				if (extraRotation != 0) customRotationsCache.Add(def.shortHash, extraRotation);
			}
		}
		public static void CheckInvesions()
		{
			//Reset
			heavyWeapons = new List<string>();
			forbiddenWeapons = new List<string>();

			//Check for abnormalities
			for (int i = allWeapons.Length; i-- > 0;)
			{
				var weapon = allWeapons[i];
				if (weapon.BaseMass > weightLimitFilterDefault)
				{
					if (!heavyWeaponsCache.Contains(weapon.shortHash)) heavyWeapons.Add(weapon.defName);
				}
				else if (heavyWeaponsCache.Contains(weapon.shortHash)) heavyWeapons.Add(weapon.defName);
				

				bool hasComp = weapon.HasModExtension<WeaponForbidden>();
				if (hasComp)
				{
					if (!forbiddenWeaponsCache.Contains(weapon.shortHash)) forbiddenWeapons.Add(weapon.defName);
				}
				else if (forbiddenWeaponsCache.Contains(weapon.shortHash)) forbiddenWeapons.Add(weapon.defName);
			}
		}
	}
	
	public class Mod_Tacticowl : Mod
	{
		public Mod_Tacticowl(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_Tacticowl>();
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			//========Setup tabs=========
			GUI.BeginGroup(inRect);
			var tabs = new List<TabRecord>();
			tabs.Add(new TabRecord("RG_Tab_RunAndGun".Translate(), delegate { selectedTab = Tab.runAndGun; }, 
				selectedTab == Tab.runAndGun || selectedTab == Tab.heavyWeapons || selectedTab == Tab.forbiddenWeapons));
			tabs.Add(new TabRecord("RG_Tab_SearchAndDestroy".Translate(), delegate { selectedTab = Tab.searchAndDestroy; }, 
				selectedTab == Tab.searchAndDestroy));
			tabs.Add(new TabRecord("RG_Tab_DualWield".Translate(), delegate { selectedTab = Tab.dualWield; }, 
				selectedTab == Tab.dualWield || selectedTab == Tab.offHands || selectedTab == Tab.twoHanders || selectedTab == Tab.customRotations || selectedTab == Tab.offsets));

			Rect rect = new Rect(0f, 32f, inRect.width, inRect.height - 32f);
			Widgets.DrawMenuSection(rect);
			TabDrawer.DrawTabs(new Rect(0f, 32f, inRect.width, Text.LineHeight), tabs);

			if (selectedTab == Tab.runAndGun || selectedTab == Tab.heavyWeapons || selectedTab == Tab.forbiddenWeapons) DrawRunAndGun();
			else if (selectedTab == Tab.searchAndDestroy) DrawSearchAndDestroy();
			else if (selectedTab == Tab.dualWield || selectedTab == Tab.offHands || 
				selectedTab == Tab.twoHanders || selectedTab == Tab.customRotations || selectedTab == Tab.offsets) DrawDualWield();
			GUI.EndGroup();
			
			void DrawRunAndGun()
			{
				if (selectedTab == Tab.runAndGun) selectedTab = Tab.heavyWeapons;

				Listing_Standard options = new Listing_Standard();
				options.Begin(inRect.ContractedBy(15f));
				options.CheckboxLabeled("RG_EnableRunAndGun_Title".Translate(), ref runAndGunEnabled, "RG_EnableRunAndGun_Description".Translate());
				options.GapLine();
				options.End();
				options.Begin(new Rect(inRect.x + 15, inRect.y + 55, inRect.width - 30, inRect.height - 30));
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
				weaponsFilterRect.y = options.curY + 170f;
				weaponsFilterRect.height = inRect.height - options.curY - 175f; //Use remaining space

				options.ColumnWidth = options.listingRect.width - 30f;
				options.End();

				//========Setup tabs=========
				tabs = new List<TabRecord>();
				tabs.Add(new TabRecord("RG_Tab_HeavyWeapons".Translate(), delegate { selectedTab = Tab.heavyWeapons; }, selectedTab == Tab.heavyWeapons));
				tabs.Add(new TabRecord("RG_Tab_ForbiddenWeapons".Translate(), delegate { selectedTab = Tab.forbiddenWeapons; }, selectedTab == Tab.forbiddenWeapons));
				
				Widgets.DrawMenuSection(weaponsFilterRect); //Used to make the background light grey with white border
				TabDrawer.DrawTabs(new Rect(weaponsFilterRect.x, weaponsFilterRect.y, weaponsFilterRect.width, Text.LineHeight), tabs);

				//========Between tabs and scroll body=========
				options.Begin(new Rect (weaponsFilterRect.x + 10, weaponsFilterRect.y + 10, weaponsFilterRect.width - 10f, weaponsFilterRect.height - 10f));
					if (selectedTab == Tab.heavyWeapons)
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
				if (selectedTab == Tab.dualWield) selectedTab = Tab.offHands;

				Listing_Standard options = new Listing_Standard();
				options.Begin(inRect.ContractedBy(15f));
				options.CheckboxLabeled("DW_EnableDualWield_Title".Translate(), ref dualWieldEnabled, "DW_EnableDualWield_Description".Translate());
				options.GapLine();
				options.End();
				options.Begin(new Rect(inRect.x + 15, inRect.y + 55, inRect.width - 30, inRect.height - 30));
				options.ColumnWidth = (options.listingRect.width - 30f) / 2f;
				
				options.Label("DW_Setting_NPCDualWieldChance_Title".Translate("0", "100", "10", NPCDualWieldChance.ToString()), -1f, "DW_Setting_NPCDualWieldChance_Description".Translate());
				NPCDualWieldChance = (int)options.Slider(NPCDualWieldChance, 0, 100);

				options.Label("DW_Setting_StaticAccPMainHand_Title".Translate("0", "500", "10", Math.Round(staticAccPMainHand).ToString()), -1f, "DW_Setting_StaticAccPMainHand_Description".Translate());
				staticAccPMainHand = options.Slider(staticAccPMainHand, 0f, 50f);
				
				options.Label("DW_Setting_StaticAccPOffHand_Title".Translate("0", "500", "10", Math.Round(staticAccPOffHand).ToString()), -1f, "DW_Setting_StaticAccPOffHand_Description".Translate());
				staticAccPOffHand = options.Slider(staticAccPOffHand, 0f, 50f);

				options.Label("DW_Setting_DynamicAccP_Title".Translate("0", "10", "0.5", Math.Round(dynamicAccP, 1).ToString()), -1f, "DW_Setting_DynamicAccP_Description".Translate());
				dynamicAccP = options.Slider(dynamicAccP, 0f, 10f);

				options.NewColumn();

				options.Label("DW_Setting_StaticCooldownPenOffHand_Title".Translate("0", "500", "20", Math.Round(staticCooldownPOffHand).ToString()), -1f, "DW_Setting_StaticCooldownPenOffHand_Description".Translate());
				staticCooldownPOffHand = options.Slider(staticCooldownPOffHand, 0f, 300f);

				options.Label("DW_Setting_StaticCooldownPMainHand_Title".Translate("0", "500", "10", Math.Round(staticCooldownPMainHand).ToString()), -1f, "DW_Setting_StaticCooldownPMainHand_Description".Translate());
				staticCooldownPMainHand = options.Slider(staticCooldownPMainHand, 0f, 300f);

				options.Label("DW_Setting_DynamicCooldownP_Title".Translate("0", "500", "5", Math.Round(dynamicCooldownP).ToString()), -1f, "DW_Setting_DynamicCooldownP_Description".Translate());
				dynamicCooldownP = options.Slider(dynamicCooldownP, 0f, 100f);
				
				
				//Record positioning before closing out the lister...
				Rect weaponsFilterRect = inRect.ContractedBy(15f);
				weaponsFilterRect.y = options.curY + 170f;
				weaponsFilterRect.height = inRect.height - options.curY - 175f; //Use remaining space

				options.ColumnWidth = options.listingRect.width - 30f;
				options.End();

				//========Setup tabs=========
				tabs = new List<TabRecord>();
				tabs.Add(new TabRecord("DW_Tab_OffHands".Translate(), delegate { selectedTab = Tab.offHands; }, selectedTab == Tab.offHands));
				tabs.Add(new TabRecord("DW_Tab_Twohanders".Translate(), delegate { selectedTab = Tab.twoHanders; }, selectedTab == Tab.twoHanders));
				tabs.Add(new TabRecord("DW_Tab_CustomRotations".Translate(), delegate { selectedTab = Tab.customRotations; }, selectedTab == Tab.customRotations));
				tabs.Add(new TabRecord("DW_Tab_Offsets".Translate(), delegate { selectedTab = Tab.offsets; }, selectedTab == Tab.offsets));
				
				Widgets.DrawMenuSection(weaponsFilterRect); //Used to make the background light grey with white border
				TabDrawer.DrawTabs(new Rect(weaponsFilterRect.x, weaponsFilterRect.y, weaponsFilterRect.width, Text.LineHeight), tabs);

				//========Between tabs and scroll body=========
				options.Begin(new Rect (weaponsFilterRect.x + 10, weaponsFilterRect.y + 10, weaponsFilterRect.width - 10f, weaponsFilterRect.height - 10f));
					if (selectedTab == Tab.offHands)
					{
						options.Label("RG_WeightLimitFilter_Title".Translate("3.4", weightLimitFilter.ToString()), -1f, "RG_WeightLimitFilter_Description".Translate());
						weightLimitFilter = options.Slider((float)Math.Round(weightLimitFilter, 1), 0f, 10f);
					}
					else if (selectedTab == Tab.twoHanders)
					{
						options.Label("DW_Twohanders".Translate());
					}
					else
					{
						options.Label("DW_CustomOffsets".Translate());
					}
				options.End();
				//========Scroll area=========
				weaponsFilterRect.y += 60f;
				weaponsFilterRect.yMax -= 60f;
				Rect weaponsFilterInnerRect = new Rect(0f, 0f, weaponsFilterRect.width - 30f, (OptionsDrawUtility.lineNumber + 2) * 22f);
				Widgets.BeginScrollView(weaponsFilterRect, ref scrollPos, weaponsFilterInnerRect , true);
					options.Begin(weaponsFilterInnerRect);
					if (selectedTab == Tab.offsets)
					{
						OptionsDrawUtility.lineNumber = 14;
						options.CheckboxLabeled("DW_Setting_MeleeMirrored_Title".Translate(), ref meleeMirrored, "DW_Setting_MeleeMirrored_Description".Translate());
						options.CheckboxLabeled("DW_Setting_RangedMirrored_Title".Translate(), ref rangedMirrored, "DW_Setting_RangedMirrored_Description".Translate());
						
						options.Label("DW_Setting_MeleeAngle_Title".Translate("0", "360", "42", Math.Round(meleeAngle).ToString()), -1f, "DW_Setting_MeleeAngle_Description".Translate());
						meleeAngle = options.Slider(meleeAngle, 0f, 360f);

						options.Label("DW_Setting_RangedAngle_Title".Translate("0", "360", "135", Math.Round(rangedAngle).ToString()), -1f, "DW_Setting_RangedAngle_Description".Translate());
						rangedAngle = options.Slider(rangedAngle, 0f, 360f);

						options.Label("DW_Setting_MeleeXOffset_Title".Translate("-1", "1", "0.24", Math.Round(meleeXOffset, 2).ToString()), -1f, "DW_Setting_MeleeXOffset_Description".Translate());
						meleeXOffset = options.Slider(meleeXOffset, -2f, 2f);

						options.Label("DW_Setting_RangedXOffset_Title".Translate("-1", "1", "0.1", Math.Round(rangedXOffset, 2).ToString()), -1f, "DW_Setting_RangedXOffset_Description".Translate());
						rangedXOffset = options.Slider(rangedXOffset, -2f, 2f);

						options.Label("DW_Setting_MeleeZOffset_Title".Translate("-1", "1", "0.09f", Math.Round(meleeZOffset, 2).ToString()), -1f, "DW_Setting_MeleeZOffset_Description".Translate());
						meleeZOffset = options.Slider(meleeZOffset, -2f, 2f);

						options.Label("DW_Setting_RangedZOffset_Title".Translate("-1", "1", "0", Math.Round(rangedZOffset, 2).ToString()), -1f, "DW_Setting_RangedZOffset_Description".Translate());
						rangedZOffset = options.Slider(rangedZOffset, -2f, 2f);
					}
					else options.DrawList(inRect);
					options.End();
				Widgets.EndScrollView();   
			}
			void DrawSearchAndDestroy()
			{
				Listing_Standard options = new Listing_Standard();
				options.Begin(inRect.ContractedBy(15f));
				options.CheckboxLabeled("SD_EnableSearchAndDestroy_Title".Translate(), ref searchAndDestroyEnabled, "SD_EnableSearchAndDestroy_Description".Translate());
				options.End();
			}
		}
		public override string SettingsCategory()
		{
			return "Tacticowl";
		}
		public override void WriteSettings()
		{            
			try
			{
				Setup.CheckInvesions();
				Setup.SetupDW(processInversions: true);
			}
			catch (System.Exception ex)
			{
				Log.Error("[Tacticowl] Error writing settings. Skipping...\n" + ex);   
			}
			base.WriteSettings();
		}   
	}
	public class ModSettings_Tacticowl : ModSettings
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
			Scribe_Collections.Look(ref offHanders, "offHanders", LookMode.Value);
			Scribe_Collections.Look(ref twoHanders, "twoHanders", LookMode.Value);
			Scribe_Collections.Look(ref customRotations, "customRotations", LookMode.Value, LookMode.Value);

			Scribe_Values.Look(ref meleeMirrored, "meleeMirrored", true);
			Scribe_Values.Look(ref rangedMirrored, "rangedMirrored", true);
			Scribe_Values.Look(ref staticCooldownPOffHand, "staticCooldownPOffHand", 20f);
			Scribe_Values.Look(ref staticCooldownPMainHand, "staticCooldownPMainHand", 10f);
			Scribe_Values.Look(ref staticAccPOffHand, "staticAccPOffHand", 10f);
			Scribe_Values.Look(ref staticAccPMainHand, "staticAccPMainHand", 10f);
			Scribe_Values.Look(ref dynamicCooldownP, "dynamicCooldownP", 5f);
			Scribe_Values.Look(ref dynamicAccP, "dynamicAccP", 0.5f);
			Scribe_Values.Look(ref meleeAngle, "meleeAngle", 42f);
			Scribe_Values.Look(ref rangedAngle, "rangedAngle", 135f);
			Scribe_Values.Look(ref meleeXOffset, "meleeXOffset", 0.24f);
			Scribe_Values.Look(ref rangedXOffset, "rangedXOffset", 0.1f);
			Scribe_Values.Look(ref meleeZOffset, "meleeZOffset", 0.09f);
			Scribe_Values.Look(ref rangedZOffset, "rangedZOffset");
			Scribe_Values.Look(ref NPCDualWieldChance, "NPCDualWieldChance", 10);

			Scribe_Values.Look(ref runAndGunEnabled, "runAndGunEnabled", true);
			Scribe_Values.Look(ref searchAndDestroyEnabled, "searchAndDestroyEnabled", true);
			Scribe_Values.Look(ref dualWieldEnabled, "dualWieldEnabled", true);
			
			base.ExposeData();
		}
		public static bool enableForAI = true,
			enableForAnimals,
			runAndGunEnabled = true,
			searchAndDestroyEnabled = true,
			dualWieldEnabled = true,
			logging,
			usingJecsTools,
			meleeMirrored = true,
			rangedMirrored = true;
		public static int enableForFleeChance = 50,
			NPCDualWieldChance = 10;
		public static float accuracyModifier = 0.65f,
			accuracyModifierPlayer = 0.65f,
			accuracyModifierMechs = 0.8f,
			movementModifierHeavy = 0.3f,
			movementModifierLight = 0.65f,
			staticCooldownPOffHand = 20f,
			staticCooldownPMainHand = 10f,
			staticAccPOffHand = 10f,
			staticAccPMainHand = 10f,
			dynamicCooldownP = 5f,
			dynamicAccP = 0.5f,
			meleeAngle = 42f,
			rangedAngle = 135f,
			meleeXOffset = 0.24f,
			rangedXOffset = 0.1f,
			meleeZOffset = 0.09f,
			rangedZOffset;
        public static HashSet<ushort> heavyWeaponsCache,
			 forbiddenWeaponsCache,
			 offHandersCache,
			 twoHandersCache;
		public static Dictionary<ushort, int> customRotationsCache;
		public static List<string> heavyWeapons,
			 forbiddenWeapons,
			 offHanders,
			 twoHanders;
		public static Dictionary<string, int> customRotations;
        
		#region settings UI
		public static float weightLimitFilter = weightLimitFilterDefault;
		public const float weightLimitFilterDefault = 3.4f;
		public static Vector2 scrollPos;
		public static Tab selectedTab = Tab.runAndGun;
		public enum Tab { runAndGun, heavyWeapons, forbiddenWeapons, searchAndDestroy, dualWield, offHands, twoHanders, customRotations, offsets };
		#endregion
	}
}