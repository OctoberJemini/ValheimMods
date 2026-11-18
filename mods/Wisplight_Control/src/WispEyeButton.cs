using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberJemini.WisplightControl
{
	public sealed class WispEyeButton : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
	{
		private static Sprite openEye;

		private static Sprite closedEye;

		private static Texture2D openTexture;

		private static Texture2D closedTexture;

		private Player player;

		private ItemDrop.ItemData item;

		private Image icon;

		private Button button;

		private UITooltip tooltip;

		private bool painted;

		private bool lastHidden;

		private void Awake()
		{
			RectTransform obj = (RectTransform)base.transform;
			Vector2 anchorMin = (obj.anchorMax = new Vector2(1f, 1f));
			obj.anchorMin = anchorMin;
			obj.pivot = new Vector2(1f, 1f);
			obj.anchoredPosition = new Vector2(-2f, -2f);
			obj.sizeDelta = new Vector2(22f, 22f);
			if (!openEye)
			{
				openEye = MakeSprite(hidden: false, out openTexture);
			}
			if (!closedEye)
			{
				closedEye = MakeSprite(hidden: true, out closedTexture);
			}
			icon = base.gameObject.AddComponent<Image>();
			icon.raycastTarget = true;
			button = base.gameObject.AddComponent<Button>();
			button.targetGraphic = icon;
			button.navigation = new Navigation
			{
				mode = Navigation.Mode.None
			};
			button.onClick.AddListener(Toggle);
			tooltip = base.gameObject.AddComponent<UITooltip>();
		}

		internal void Bind(Player wearer, ItemDrop.ItemData equippedItem, UITooltip originalTooltip)
		{
			player = wearer;
			item = equippedItem;
			if ((bool)originalTooltip)
			{
				tooltip.m_tooltipPrefab = originalTooltip.m_tooltipPrefab;
			}
			tooltip.enabled = (bool)originalTooltip && (bool)originalTooltip.m_tooltipPrefab;
			button.interactable = (bool)player && player == Player.m_localPlayer && item != null && item.m_equipped;
			Paint();
		}

		private void Paint()
		{
			if ((bool)player)
			{
				player.m_customData.TryGetValue("OctoberJemini.WisplightControl.VisualMode", out var value);
				bool flag = WispRules.IsWispHidden(value);
				if (!painted || flag != lastHidden)
				{
					painted = true;
					lastHidden = flag;
					icon.sprite = (flag ? closedEye : openEye);
					tooltip.m_topic = (flag ? "Show wisp" : "Hide wisp");
					tooltip.m_text = "Click to " + (flag ? "show" : "hide") + " the wisp.\nMist clearing stays ON.\nCharacter glow: " + (flag ? "ON" : "OFF");
				}
			}
		}

		private void Toggle()
		{
			if ((bool)player && item != null && item.m_equipped && player.GetInventory().GetAllItems().Contains(item) && Plugin.ToggleVisibility(player))
			{
				Paint();
			}
		}

		public void OnBeginDrag(PointerEventData data)
		{
			data.Use();
		}

		public void OnDrag(PointerEventData data)
		{
			data.Use();
		}

		public void OnEndDrag(PointerEventData data)
		{
			data.Use();
		}

		private static Sprite MakeSprite(bool hidden, out Texture2D texture)
		{
			texture = new Texture2D(64, 64, TextureFormat.RGBA32, mipChain: false);
			texture.name = (hidden ? "OCT_WispEyeClosed" : "OCT_WispEyeOpen");
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.LoadRawTextureData(EyeIconPixels.Render(hidden));
			texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
			return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
		}

		internal static void ReleaseSprites()
		{
			if ((bool)openEye)
			{
				Object.Destroy(openEye);
			}
			if ((bool)closedEye)
			{
				Object.Destroy(closedEye);
			}
			if ((bool)openTexture)
			{
				Object.Destroy(openTexture);
			}
			if ((bool)closedTexture)
			{
				Object.Destroy(closedTexture);
			}
		}
	}
}
