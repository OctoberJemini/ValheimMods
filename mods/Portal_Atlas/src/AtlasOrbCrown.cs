using System;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;
namespace OctoberJemini.PortalAtlas {
 internal static class AtlasOrbCrown {
  internal const float Scale=.42f, Height=1.54f;
  internal static void Build(GameObject parent) {
   if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
   var source=PrefabManager.Instance.GetPrefab("dverger_demister");
   if(!source)throw new InvalidOperationException("dverger_demister prefab missing");
   var frame=source.transform.Find("New");
   var enabled=source.transform.Find("_enabled");
   var ball=enabled?enabled.Find("demister_ball (1)"):null;
   var sound=enabled?enabled.Find("SFX"):null;
   if(!frame||!ball||!sound)throw new InvalidOperationException("Native demister visual/audio hierarchy changed");
   var nativeAudio=sound.GetComponent<AudioSource>();
   AudioClip clip=nativeAudio?nativeAudio.clip:null;
   if(!clip)foreach(var script in sound.GetComponents<MonoBehaviour>()) {
    if(!script)continue;
    var field=AccessTools.Field(script.GetType(),"m_audioClips");
    var clips=field==null?null:field.GetValue(script) as AudioClip[];
    if(clips!=null&&clips.Length>0){clip=clips[0];break;}
   }
   if(!clip)throw new InvalidOperationException("Native demister looping clip missing");
   var host=new GameObject("AtlasNativeOrbCrown");host.SetActive(false);
   host.transform.SetParent(parent.transform,false);
   host.transform.localPosition=new Vector3(0,Height,0);
   host.transform.localRotation=Quaternion.identity;host.transform.localScale=Vector3.one*Scale;
   try {
    // Only copy authored mesh objects. The long native stem seats inside the stone;
    // the open ring and cup sit just above the cap. No native gameplay scripts run.
    var metal=CopyMesh(frame,host.transform,"AtlasOrbFrame",frame.localPosition);
    var mesh=metal.GetComponent<MeshFilter>().sharedMesh;
    var center=Vector3.Scale(mesh.bounds.center,metal.transform.localScale);
    metal.transform.localPosition-=new Vector3(center.x,0,center.z);
    CopyMesh(ball,host.transform,"AtlasOrbBall",enabled.localPosition+ball.localPosition);
    var lightObject=new GameObject("AtlasOrbLight");lightObject.transform.SetParent(host.transform,false);
    lightObject.transform.localPosition=enabled.localPosition;
    var nativeLight=enabled.GetComponentInChildren<Light>(true);
    var light=lightObject.AddComponent<Light>();light.type=LightType.Point;
    light.color=nativeLight?nativeLight.color:new Color(.48f,.78f,1);light.intensity=.75f;light.range=3;light.shadows=LightShadows.None;
    var audioObject=new GameObject("AtlasOrbHum");audioObject.transform.SetParent(host.transform,false);
    audioObject.transform.localPosition=enabled.localPosition;
    var audio=audioObject.AddComponent<AudioSource>();audio.clip=clip;
    if(nativeAudio)audio.outputAudioMixerGroup=nativeAudio.outputAudioMixerGroup;
    audio.loop=true;audio.playOnAwake=true;audio.spatialBlend=1;audio.volume=.12f;audio.pitch=.65f;
    audio.rolloffMode=AudioRolloffMode.Linear;audio.minDistance=.6f;audio.maxDistance=6;audio.dopplerLevel=0;
    CopySparks(enabled,host.transform);
    host.SetActive(true);
    Debug.Log("[Portal Atlas] Native Dvergr crown ready: centered, level, native sparks, 3m blue light and 6m spatial hum.");
   }catch{Object.DestroyImmediate(host);throw;}
  }
  private static void CopySparks(Transform effects,Transform parent) {
   var source=effects.Find("sparcs_front");
   if(!source){Debug.LogWarning("[Portal Atlas] Native spark visual missing; crown retained.");return;}
   // Clone only the particle child under an inactive parent, never the demister.
   var sparks=Object.Instantiate(source.gameObject,parent,false);
   sparks.name="AtlasOrbSparks";
   sparks.transform.localPosition=effects.localPosition+source.localPosition;
   foreach(var script in sparks.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
   foreach(var collider in sparks.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
   foreach(var particles in sparks.GetComponentsInChildren<ParticleSystem>(true)) {
    var main=particles.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
    main.simulationSpace=ParticleSystemSimulationSpace.Local;main.playOnAwake=true;
    var forces=particles.externalForces;forces.enabled=false;
    var collision=particles.collision;collision.enabled=false;
    var emission=particles.emission;emission.rateOverTimeMultiplier*=.65f;
   }
   sparks.SetActive(true);
  }
  private static GameObject CopyMesh(Transform source,Transform parent,string name,Vector3 position) {
   // Construct a render-only object rather than instantiating a networked prop.
   var filter=source.GetComponent<MeshFilter>();var renderer=source.GetComponent<MeshRenderer>();
   if(!filter||!filter.sharedMesh||!renderer)throw new InvalidOperationException("Missing native mesh: "+source.name);
   var go=new GameObject(name);go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
   go.transform.localPosition=position;go.transform.localRotation=Quaternion.identity;go.transform.localScale=source.localScale;
   go.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
   go.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
   return go;
  }
 }
}
