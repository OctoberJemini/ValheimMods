using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OctoberJemini.WisplightControl
{
	[BepInPlugin("OctoberJemini.WisplightControl", "Wisplight Control", "1.0.0")]
	[BepInIncompatibility("com.Azura.wisptoggle")]
	[BepInIncompatibility("mtsukn.upgradeablewisplight")]
	public sealed class Plugin : BaseUnityPlugin
	{
		[HarmonyPatch(typeof(ObjectDB), "Awake")]
		private static class DbAwakePatch
		{
			private static void Postfix(ObjectDB __instance)
			{
				if ((bool)Instance)
				{
					Instance.Configure(__instance);
				}
			}
		}

		[HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
		private static class DbCopyPatch
		{
			private static void Postfix(ObjectDB __instance)
			{
				if ((bool)Instance)
				{
					Instance.Configure(__instance);
				}
			}
		}

		[HarmonyPatch(typeof(SE_Demister), "UpdateStatusEffect")]
		private static class WispEffectPatch
		{
			private static void Postfix(SE_Demister __instance)
			{
				Player player = __instance.m_character as Player;
				if (!player || player != Player.m_localPlayer)
				{
					return;
				}
				GameObject gameObject = Ball(__instance);
				if ((bool)gameObject)
				{
					WispController wispController = gameObject.GetComponent<WispController>();
					if (!wispController)
					{
						wispController = gameObject.AddComponent<WispController>();
					}
					wispController.Owner = player;
				}
			}
		}

		[HarmonyPatch(typeof(Demister), "Awake")]
		private static class BallPatch
		{
			private static void Postfix(Demister __instance)
			{
				ZNetView componentInParent = __instance.GetComponentInParent<ZNetView>();
				if ((bool)componentInParent && (!(componentInParent.gameObject.name != "demister_ball") || !(componentInParent.gameObject.name != "demister_ball(Clone)")) && !componentInParent.GetComponent<WispController>())
				{
					componentInParent.gameObject.AddComponent<WispController>();
				}
			}
		}

		[HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new Type[]
		{
			typeof(ItemDrop.ItemData),
			typeof(int),
			typeof(bool),
			typeof(float),
			typeof(int),
			typeof(bool)
		})]
		private static class TooltipPatch
		{
			private static void Postfix(ItemDrop.ItemData __0, int __1, ref string __result)
			{
				if (IsWisp(__0))
				{
					__result = __result + "\n\n<color=orange>Wisplight Control</color>\nMist radius: " + WispRules.Radius(__1, BaseRadius.Value, RadiusStep.Value).ToString("0.#") + "m\n" + ToggleKey.Value.ToString() + " or equipped slot's eye button: Hide/show wisp\nMist clearing stays on. Character glow only while wisp is hidden.";
				}
			}
		}

		public const string Id = "OctoberJemini.WisplightControl";

		internal const string ModeKey = "OctoberJemini.WisplightControl.VisualMode";

		internal static Plugin Instance;

		internal static ConfigEntry<KeyboardShortcut> ToggleKey;

		internal static ConfigEntry<float> BaseRadius;

		internal static ConfigEntry<float> RadiusStep;

		internal static ConfigEntry<float> LightRange;

		internal static ConfigEntry<float> LightIntensity;

		private Harmony harmony;

		private ObjectDB configuredDb;

		private float nextCheck;

		private readonly Dictionary<ItemDrop.ItemData.SharedData, int> originalQualities = new Dictionary<ItemDrop.ItemData.SharedData, int>();

		private readonly Dictionary<Piece.Requirement, int> originalCosts = new Dictionary<Piece.Requirement, int>();

		private readonly Dictionary<Piece.Requirement, int> originalCraftCosts = new Dictionary<Piece.Requirement, int>();

		private static readonly AccessTools.FieldRef<SE_Demister, GameObject> Ball = AccessTools.FieldRefAccess<SE_Demister, GameObject>("m_ballInstance");

		private static readonly Func<Player, bool> CanTakeInput = AccessTools.MethodDelegate<Func<Player, bool>>(AccessTools.Method(typeof(Player), "TakeInput"));

		private void Awake()
		{
			Instance = this;
			ToggleKey = base.Config.Bind("Controls", "Toggle key", new KeyboardShortcut(KeyCode.F9), "Hide/show the wisp. Mist clearing stays active either way. Character glow replaces the wisp's light only while hidden. Unequip to turn everything off.");
			BaseRadius = base.Config.Bind("Mist clearing", "Level 1 radius", 15f, new ConfigDescription("Clearing radius in metres at quality 1. Installed vanilla value is 15m.", new AcceptableValueRange<float>(1f, 40f)));
			RadiusStep = base.Config.Bind("Mist clearing", "Radius per upgrade", 5f, new ConfigDescription("Additional metres per quality level, up to quality 4.", new AcceptableValueRange<float>(0f, 20f)));
			LightRange = base.Config.Bind("Lighting", "Character light radius", 10f, new ConfigDescription("Radius of the steady white character glow, only while the wisp is hidden. Visible wisps retain their normal movement and light.", new AcceptableValueRange<float>(1f, 25f)));
			LightIntensity = base.Config.Bind("Lighting", "Character light brightness", 1.8f, new ConfigDescription("Steady character-glow intensity. No visible orb or flame. 0 disables its illumination.", new AcceptableValueRange<float>(0f, 3f)));
			harmony = new Harmony("OctoberJemini.WisplightControl");
			harmony.PatchAll(typeof(Plugin).Assembly);
			base.Logger.LogInfo("Wisplight Control loaded: four quality levels, steady character light; F9 hides/shows wisp visuals; mist clearing stays on.");
		}

		private void Update()
		{
			if (Time.unscaledTime >= nextCheck)
			{
				nextCheck = Time.unscaledTime + 1f;
				if ((bool)ObjectDB.instance && configuredDb != ObjectDB.instance)
				{
					Configure(ObjectDB.instance);
				}
				Player localPlayer = Player.m_localPlayer;
				if ((bool)localPlayer)
				{
					foreach (ItemDrop.ItemData allItem in localPlayer.GetInventory().GetAllItems())
					{
						if (IsWisp(allItem))
						{
							AllowUpgrades(allItem.m_shared);
						}
					}
				}
			}
			Player localPlayer2 = Player.m_localPlayer;
			if ((bool)localPlayer2 && ToggleKey.Value.IsDown() && CanTakeInput(localPlayer2) && !InventoryGui.IsVisible() && !TextInput.IsVisible() && !Menu.IsVisible())
			{
				ToggleVisibility(localPlayer2);
			}
		}

		internal static bool ToggleVisibility(Player player)
		{
			if (!player || player != Player.m_localPlayer || player.IsDead() || player.IsTeleporting())
			{
				return false;
			}
			if (Equipped(player) == null)
			{
				player.Message(MessageHud.MessageType.Center, "Equip a Wisplight to switch modes.");
				return false;
			}
			player.m_customData.TryGetValue("OctoberJemini.WisplightControl.VisualMode", out var value);
			string text = WispRules.Toggle(value);
			player.m_customData["OctoberJemini.WisplightControl.VisualMode"] = text;
			player.Message(MessageHud.MessageType.Center, WispRules.IsWispHidden(text) ? "Wisp hidden (mist clearing stays ON)" : "Wisp visible (mist clearing stays ON)");
			WispController[] array = WispController.Live.ToArray();
			foreach (WispController wispController in array)
			{
				if (wispController.Owner == player)
				{
					wispController.Refresh();
				}
			}
			return true;
		}

		internal static bool IsWisp(ItemDrop.ItemData item)
		{
			if (item != null)
			{
				if (!item.m_dropPrefab || !(item.m_dropPrefab.name == "Demister"))
				{
					return item.m_shared.m_name == "$item_demister";
				}
				return true;
			}
			return false;
		}

		internal static ItemDrop.ItemData Equipped(Player player)
		{
			return (from i in player.GetInventory().GetAllItems()
				where i.m_equipped && IsWisp(i)
				orderby i.m_quality descending
				select i).FirstOrDefault();
		}

		private void AllowUpgrades(ItemDrop.ItemData.SharedData shared)
		{
			if (!originalQualities.ContainsKey(shared))
			{
				originalQualities.Add(shared, shared.m_maxQuality);
			}
			shared.m_maxQuality = 4;
		}

		internal void Configure(ObjectDB db)
		{
			GameObject itemPrefab = db.GetItemPrefab("Demister");
			if (!itemPrefab)
			{
				return;
			}
			ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
			if (!component)
			{
				return;
			}
			Recipe recipe = db.m_recipes.FirstOrDefault((Recipe r) => (bool)r && (bool)r.m_item && IsWisp(r.m_item.m_itemData));
			if (!recipe)
			{
				return;
			}
			if (!TieredUpgradeCosts.Configure(db, recipe)) return;
			AllowUpgrades(component.m_itemData.m_shared);
			Piece.Requirement[] resources = recipe.m_resources;
			foreach (Piece.Requirement requirement in resources)
			{
				if (!requirement.m_resItem)
				{
					continue;
				}
				string text = requirement.m_resItem.gameObject.name;
				if (!(text != "Wisp") || !(text != "Silver"))
				{
					if (!originalCosts.ContainsKey(requirement))
					{
						originalCosts.Add(requirement, requirement.m_amountPerLevel);
					}
					if (!originalCraftCosts.ContainsKey(requirement))
					{
						originalCraftCosts.Add(requirement, requirement.m_amount);
					}
					requirement.m_amount = 1;
					requirement.m_amountPerLevel = ((text == "Wisp") ? 0 : WispRules.SilverPerLevel);
				}
			}
			configuredDb = db;
			base.Logger.LogInfo("Wisplight upgrades enabled: 20 Wisps per upgrade; Refined Eitr, Gjall and Seeker Soldier trophy progression. Initial craft remains 1 Wisp + 1 Silver.");
		}

		private void OnDestroy()
		{
			TieredUpgradeCosts.Restore();
			WispEyeGrid[] array = WispEyeGrid.Live.ToArray();
			foreach (WispEyeGrid wispEyeGrid in array)
			{
				if ((bool)wispEyeGrid)
				{
					UnityEngine.Object.DestroyImmediate(wispEyeGrid);
				}
			}
			WispEyeButton.ReleaseSprites();
			WispController[] array2 = WispController.Live.ToArray();
			foreach (WispController wispController in array2)
			{
				if ((bool)wispController)
				{
					UnityEngine.Object.DestroyImmediate(wispController);
				}
			}
			foreach (KeyValuePair<ItemDrop.ItemData.SharedData, int> originalQuality in originalQualities)
			{
				originalQuality.Key.m_maxQuality = originalQuality.Value;
			}
			foreach (KeyValuePair<Piece.Requirement, int> originalCost in originalCosts)
			{
				originalCost.Key.m_amountPerLevel = originalCost.Value;
			}
			foreach (KeyValuePair<Piece.Requirement, int> originalCraftCost in originalCraftCosts)
			{
				originalCraftCost.Key.m_amount = originalCraftCost.Value;
			}
			harmony?.UnpatchSelf();
			Instance = null;
		}
	}
}
