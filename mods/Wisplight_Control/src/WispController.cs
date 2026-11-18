using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OctoberJemini.WisplightControl
{
	public sealed class WispController : MonoBehaviour
	{
		internal static readonly HashSet<WispController> Live = new HashSet<WispController>();

		private const string VersionKey = "oct_wisp_control";

		private const string ModeKey = "oct_wisp_hidden";

		private const string RadiusKey = "oct_wisp_radius";

		private const string WearerKey = "oct_wisp_wearer";

		internal Player Owner;

		private ZNetView view;

		private Light characterLight;

		private float nextCheck;

		private bool applied;

		private bool lastHidden;

		private float lastRadius;

		private float lastLightRange;

		private float lastLightIntensity;

		private Demister[] demisters;

		private bool[] demisterEnabled;

		private ParticleSystemForceField[] fields;

		private bool[] fieldEnabled;

		private float[] fieldRanges;

		private Light[] lights;

		private bool[] lightEnabled;

		private LightFlicker[] flickers;

		private bool[] flickerEnabled;

		private LightLod[] lightLods;

		private bool[] lightLodEnabled;

		private AudioSource[] audioSources;

		private bool[] audioMute;

		private Renderer[] renderers;

		private bool[] rendererEnabled;

		private void Awake()
		{
			view = GetComponent<ZNetView>();
			demisters = GetComponentsInChildren<Demister>(includeInactive: true);
			demisterEnabled = demisters.Select((Demister d) => d.enabled).ToArray();
			fields = GetComponentsInChildren<ParticleSystemForceField>(includeInactive: true);
			fieldEnabled = fields.Select((ParticleSystemForceField f) => f.enabled).ToArray();
			fieldRanges = fields.Select((ParticleSystemForceField f) => f.endRange).ToArray();
			lights = GetComponentsInChildren<Light>(includeInactive: true);
			lightEnabled = lights.Select((Light l) => l.enabled).ToArray();
			flickers = GetComponentsInChildren<LightFlicker>(includeInactive: true);
			flickerEnabled = flickers.Select((LightFlicker f) => f.enabled).ToArray();
			lightLods = GetComponentsInChildren<LightLod>(includeInactive: true);
			lightLodEnabled = lightLods.Select((LightLod l) => l.enabled).ToArray();
			audioSources = GetComponentsInChildren<AudioSource>(includeInactive: true);
			audioMute = audioSources.Select((AudioSource a) => a.mute).ToArray();
			renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
			rendererEnabled = renderers.Select((Renderer r) => r.enabled).ToArray();
			Live.Add(this);
		}

		internal void Refresh()
		{
			nextCheck = 0f;
		}

		private void LateUpdate()
		{
			if ((bool)characterLight && (bool)Owner)
			{
				characterLight.transform.position = Owner.GetCenterPoint();
			}
			if (Time.unscaledTime < nextCheck || !Plugin.Instance)
			{
				return;
			}
			nextCheck = Time.unscaledTime + 0.2f;
			if (!view || !view.IsValid())
			{
				return;
			}
			ZDO zDO = view.GetZDO();
			if (!Owner && (bool)ZNetScene.instance)
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(zDO.GetZDOID("oct_wisp_wearer"));
				if ((bool)gameObject)
				{
					Owner = gameObject.GetComponent<Player>();
				}
			}
			if ((bool)Owner && Owner == Player.m_localPlayer && view.IsOwner())
			{
				ItemDrop.ItemData itemData = Plugin.Equipped(Owner);
				if (itemData != null)
				{
					Owner.m_customData.TryGetValue("OctoberJemini.WisplightControl.VisualMode", out var value);
					bool flag = WispRules.IsWispHidden(value);
					float num = WispRules.Radius(itemData.m_quality, Plugin.BaseRadius.Value, Plugin.RadiusStep.Value);
					if (zDO.GetInt("oct_wisp_control") != 1)
					{
						zDO.Set("oct_wisp_control", 1);
					}
					if (zDO.GetZDOID("oct_wisp_wearer") != Owner.GetZDOID())
					{
						zDO.Set("oct_wisp_wearer", Owner.GetZDOID());
					}
					if (zDO.GetBool("oct_wisp_hidden", defaultValue: true) != flag)
					{
						zDO.Set("oct_wisp_hidden", flag);
					}
					if (Math.Abs(zDO.GetFloat("oct_wisp_radius") - num) > 0.01f)
					{
						zDO.Set("oct_wisp_radius", num);
					}
				}
			}
			if (zDO.GetInt("oct_wisp_control") == 1)
			{
				bool flag2 = zDO.GetBool("oct_wisp_hidden", defaultValue: true);
				float num2 = Mathf.Clamp(zDO.GetFloat("oct_wisp_radius", 15f), 1f, 100f);
				if (!applied || flag2 != lastHidden || num2 != lastRadius || Plugin.LightRange.Value != lastLightRange || Plugin.LightIntensity.Value != lastLightIntensity)
				{
					applied = true;
					lastHidden = flag2;
					lastRadius = num2;
					lastLightRange = Plugin.LightRange.Value;
					lastLightIntensity = Plugin.LightIntensity.Value;
					Apply(flag2, num2);
				}
			}
		}

		private void Apply(bool hideWisp, float radius)
		{
			if ((bool)characterLight)
			{
				characterLight.enabled = false;
			}
			for (int i = 0; i < lightLods.Length; i++)
			{
				if ((bool)lightLods[i])
				{
					lightLods[i].enabled = !hideWisp && lightLodEnabled[i];
				}
			}
			for (int j = 0; j < flickers.Length; j++)
			{
				if ((bool)flickers[j])
				{
					flickers[j].enabled = !hideWisp && flickerEnabled[j];
				}
			}
			for (int k = 0; k < demisters.Length; k++)
			{
				if ((bool)demisters[k])
				{
					demisters[k].enabled = demisterEnabled[k];
				}
			}
			for (int l = 0; l < fields.Length; l++)
			{
				if ((bool)fields[l])
				{
					fields[l].endRange = radius;
					fields[l].enabled = fieldEnabled[l];
				}
			}
			for (int m = 0; m < lights.Length; m++)
			{
				if ((bool)lights[m])
				{
					lights[m].enabled = !hideWisp && lightEnabled[m];
				}
			}
			for (int n = 0; n < audioSources.Length; n++)
			{
				if ((bool)audioSources[n])
				{
					audioSources[n].mute = hideWisp || audioMute[n];
				}
			}
			for (int num = 0; num < renderers.Length; num++)
			{
				if ((bool)renderers[num])
				{
					renderers[num].enabled = !hideWisp && rendererEnabled[num];
				}
			}
			if (!characterLight)
			{
				GameObject gameObject = new GameObject("OCT_WisplightCharacterGlow");
				gameObject.transform.SetParent(Owner ? Owner.transform : base.transform, worldPositionStays: false);
				characterLight = gameObject.AddComponent<Light>();
				characterLight.type = LightType.Point;
				characterLight.color = Color.white;
				characterLight.shadows = LightShadows.None;
			}
			characterLight.range = lastLightRange;
			characterLight.intensity = lastLightIntensity;
			characterLight.enabled = WispRules.CharacterGlow(hideWisp, lastLightIntensity);
			characterLight.transform.position = (Owner ? Owner.GetCenterPoint() : base.transform.position);
		}

		private void OnDestroy()
		{
			Live.Remove(this);
			if ((bool)characterLight)
			{
				UnityEngine.Object.Destroy(characterLight.gameObject);
			}
			if (!applied)
			{
				return;
			}
			for (int i = 0; i < demisters.Length; i++)
			{
				if ((bool)demisters[i])
				{
					demisters[i].enabled = demisterEnabled[i];
				}
			}
			for (int j = 0; j < fields.Length; j++)
			{
				if ((bool)fields[j])
				{
					fields[j].enabled = fieldEnabled[j];
					fields[j].endRange = fieldRanges[j];
				}
			}
			for (int k = 0; k < lights.Length; k++)
			{
				if ((bool)lights[k])
				{
					lights[k].enabled = lightEnabled[k];
				}
			}
			for (int l = 0; l < flickers.Length; l++)
			{
				if ((bool)flickers[l])
				{
					flickers[l].enabled = flickerEnabled[l];
				}
			}
			for (int m = 0; m < lightLods.Length; m++)
			{
				if ((bool)lightLods[m])
				{
					lightLods[m].enabled = lightLodEnabled[m];
				}
			}
			for (int n = 0; n < audioSources.Length; n++)
			{
				if ((bool)audioSources[n])
				{
					audioSources[n].mute = audioMute[n];
				}
			}
			for (int num = 0; num < renderers.Length; num++)
			{
				if ((bool)renderers[num])
				{
					renderers[num].enabled = rendererEnabled[num];
				}
			}
		}
	}
}
