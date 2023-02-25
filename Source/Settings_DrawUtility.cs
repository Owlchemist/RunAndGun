using Verse;
using UnityEngine;
using Verse.Sound;
using RimWorld;
using System.Collections.Generic;
using static SumGunFun.ModSettings_SumGunFun;
using static SumGunFun.Setup;
 
namespace SumGunFun
{
    public static class OptionsDrawUtility
	{
		public static int lineNumber, cellPosition;
		public const int lineHeight = 22; //Text.LineHeight + options.verticalSpacing;
		public static void DrawList(this Listing_Standard options, Rect rect)
		{
			Rect container = rect;
			lineNumber = cellPosition = 0; //Reset
			//List out all the unremoved defs from the compiled database
			for (int i = 0; i < allWeapons.Length; i++)
			{
				var def = allWeapons[i];
				if (def == null) continue;
				if (selectedTab == Tab.heavyWeapons && def.BaseMass <= weightLimitFilter) continue;
				else if (selectedTab == Tab.offHands && twoHandersCache.Contains(def.shortHash)) continue;

				cellPosition += lineHeight;
				++lineNumber;
				
				//if (cellPosition > scrollPos.y - container.height && cellPosition < scrollPos.y + container.height) DrawListItem(options, def);
				DrawListItem(options, def);
			}
		}

		public static void DrawListItem(Listing_Standard options, ThingDef def)
		{
			//Determine checkbox status...
			bool checkOn;
			ushort hash = def.shortHash;
			if (selectedTab == Tab.heavyWeapons) checkOn = heavyWeaponsCache.Contains(hash);
			else if (selectedTab == Tab.forbiddenWeapons) checkOn = forbiddenWeaponsCache.Contains(hash);
			else if (selectedTab == Tab.offHands) checkOn = offHandersCache.Contains(hash);
			else if (selectedTab == Tab.twoHanders) checkOn = twoHandersCache.Contains(hash);
			else checkOn = customRotationsCache.ContainsKey(hash);
			
			//Fetch bounding rect
			Rect rect = options.GetRect(lineHeight);
			rect.y = cellPosition;

			//Label
			string dataString = def.label + " :: " + def.modContentPack?.Name + " :: " + def.defName;

			//Actually draw the line item
			if (options.BoundingRectCached == null || rect.Overlaps(options.BoundingRectCached.Value))
			{
				CheckboxLabeled(rect, dataString, def.label, ref checkOn, def);
			}

			//Handle row coloring and spacing
			if (lineNumber % 2 != 0) Widgets.DrawLightHighlight(rect);
			Widgets.DrawHighlightIfMouseover(rect);
			
			HashSet<ushort> hashCache;
			if (selectedTab == Tab.heavyWeapons) hashCache = heavyWeaponsCache;
			else if (selectedTab == Tab.forbiddenWeapons) hashCache = forbiddenWeaponsCache;
			else if (selectedTab == Tab.offHands) hashCache = offHandersCache;
			else if (selectedTab == Tab.twoHanders) hashCache = twoHandersCache;
			else return;
			
			if (checkOn)
			{
				if (!hashCache.Contains(hash)) hashCache.Add(hash);
			}
			else if (hashCache.Contains(hash)) hashCache.Remove(hash);
		}

		static void CheckboxLabeled(Rect rect, string data, string label, ref bool checkOn, ThingDef def)
		{
			Rect leftHalf = rect.LeftHalf();
			
			//Is there an icon?
			Rect iconRect = new Rect(leftHalf.x, leftHalf.y, 32f, leftHalf.height);
			Texture2D icon = null;
			if (def is BuildableDef) icon = ((BuildableDef)def).uiIcon;
			if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 1f, Color.white, 0f, 0f);

			//If there is a label, split the cell in half, otherwise use the full cell for data
			if (!label.NullOrEmpty())
			{
				Rect dataRect = new Rect(iconRect.xMax, iconRect.y, leftHalf.width - 32f, leftHalf.height);

				Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
				Rect rightHalf = rect.RightHalf();
				Widgets.Label(rightHalf, label.Truncate(rightHalf.width - 12f, InspectPaneUtility.truncatedLabelsCached));
			}
			else
			{
				Rect dataRect = new Rect(iconRect.xMax, iconRect.y, rect.width - 32f, leftHalf.height);
				Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
			}

			//Checkbox
			if (Widgets.ButtonInvisible(rect, true))
			{
				checkOn = !checkOn;
				if (checkOn) SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
				else SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
			}
			Widgets.CheckboxDraw(rect.xMax - 24f, rect.y, checkOn, false, 24f, null, null);
		}
	}
}