using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OctoberJemini.WisplightControl
{
	public sealed class WispEyeGrid : MonoBehaviour
	{
		[HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
		private static class GridPatch
		{
			private static void Postfix(InventoryGrid __instance, Inventory ___m_inventory, List<InventoryElement> ___m_elements)
			{
				if ((bool)Plugin.Instance && ___m_inventory != null && ___m_elements != null)
				{
					WispEyeGrid wispEyeGrid = __instance.GetComponent<WispEyeGrid>();
					if (!wispEyeGrid)
					{
						wispEyeGrid = __instance.gameObject.AddComponent<WispEyeGrid>();
					}
					wispEyeGrid.Sync(___m_inventory, ___m_elements);
				}
			}
		}

		internal static readonly HashSet<WispEyeGrid> Live = new HashSet<WispEyeGrid>();

		private WispEyeButton eye;

		private void Awake()
		{
			Live.Add(this);
		}

		internal void Sync(Inventory inventory, List<InventoryElement> elements)
		{
			Player localPlayer = Player.m_localPlayer;
			ItemDrop.ItemData itemData = null;
			if ((bool)localPlayer && inventory != null)
			{
				foreach (ItemDrop.ItemData allItem in inventory.GetAllItems())
				{
					if (allItem.m_equipped && Plugin.IsWisp(allItem) && localPlayer.GetInventory().GetAllItems().Contains(allItem))
					{
						itemData = allItem;
						break;
					}
				}
			}
			InventoryElement inventoryElement = null;
			if (itemData != null)
			{
				foreach (InventoryElement element in elements)
				{
					if ((bool)element && element.Position.x == itemData.m_gridPos.x && element.Position.y == itemData.m_gridPos.y)
					{
						inventoryElement = element;
						break;
					}
				}
			}
			if (!inventoryElement)
			{
				if ((bool)eye)
				{
					eye.gameObject.SetActive(value: false);
				}
				return;
			}
			if (!eye)
			{
				GameObject gameObject = new GameObject("OCT_WisplightEye", typeof(RectTransform));
				gameObject.layer = inventoryElement.gameObject.layer;
				gameObject.transform.SetParent(inventoryElement.transform, worldPositionStays: false);
				eye = gameObject.AddComponent<WispEyeButton>();
			}
			if (eye.transform.parent != inventoryElement.transform)
			{
				eye.transform.SetParent(inventoryElement.transform, worldPositionStays: false);
			}
			eye.gameObject.SetActive(value: true);
			eye.transform.SetAsLastSibling();
			eye.Bind(localPlayer, itemData, inventoryElement.m_tooltip);
		}

		private void OnDestroy()
		{
			Live.Remove(this);
			if ((bool)eye)
			{
				Object.Destroy(eye.gameObject);
			}
		}
	}
}
