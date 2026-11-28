using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace OctoberJemini.SkuldsThreadRope {
 [BepInPlugin("OctoberJemini.SkuldsThreadRope", "Skuld's Thread Rope", "0.1.0")]
 [BepInDependency("randyknapp.mods.epicloot", "0.16.0")]
 public sealed class Plugin : BaseUnityPlugin {
  static Plugin instance; static Sprite icon; static Mesh rope; static Material material;
  const string VisualName="OCT_SkuldsThreadRope";
  Harmony harmony;
  void Awake() {
   instance=this;
   try {
    var type=AccessTools.TypeByName("EpicLoot.EpicLoot");
    var setup=AccessTools.Method(type,"SetupSkuldsThread");
    if(setup==null)throw new MissingMethodException("EpicLoot.SetupSkuldsThread");
    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("rope.png")) {
     var bytes=new byte[stream.Length]; int read=0,n;
     while(read<bytes.Length && (n=stream.Read(bytes,read,bytes.Length-read))>0)read+=n;
     var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
     if(!ImageConversion.LoadImage(texture,bytes))throw new InvalidDataException("Rope icon could not load");
     texture.name="OCT_SkuldsThreadIcon";
     icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
    }
    rope=RopeMesh.Build();
    harmony=new Harmony("OctoberJemini.SkuldsThreadRope");
    harmony.Patch(setup,postfix:new HarmonyMethod(typeof(Plugin),nameof(AfterSetup)));
    harmony.Patch(AccessTools.Method(typeof(ItemDrop),"Awake"),postfix:new HarmonyMethod(typeof(Plugin),nameof(AfterItemAwake)));
    Logger.LogInfo("Skuld's Thread rope appearance ready.");
   } catch(Exception e){if(harmony!=null)harmony.UnpatchSelf();Logger.LogError(e);}
  }
  static void AfterSetup() {
   try {var custom=Jotunn.Managers.ItemManager.Instance.GetItem("SkuldsThread");
    var go=custom!=null?custom.ItemPrefab:(ObjectDB.instance?ObjectDB.instance.GetItemPrefab("SkuldsThread"):null);
    if(!go){instance.Logger.LogWarning("SkuldsThread prefab unavailable after setup");return;}
    Apply(go.GetComponent<ItemDrop>());
   }catch(Exception e){instance.Logger.LogError("Rope appearance: "+e);}
  }
  static void AfterItemAwake(ItemDrop __instance) {
   if(__instance && (__instance.name=="SkuldsThread" || __instance.name=="SkuldsThread(Clone)")) {
    try{Apply(__instance);}catch(Exception e){instance.Logger.LogError("Rope appearance: "+e);}
   }
  }
  static void Apply(ItemDrop item) {
   if(!item || icon==null || rope==null)return;
   // Only this item's shared presentation is changed; keep its identity and tracker.
   item.m_itemData.m_shared.m_icons=new[]{icon};
   var effect=item.m_itemData.m_shared.m_equipStatusEffect;
   if(effect && effect.name=="SkuldsThread")effect.m_icon=icon;
   var parent=item.transform.Find("attach") ?? item.transform;
   if(parent.Find(VisualName))return;
   var renderers=parent.GetComponentsInChildren<MeshRenderer>(true);
   if(material==null) {
    Material original=null;
    foreach(var r in renderers)if(r.sharedMaterial){original=r.sharedMaterial;break;}
    if(original==null)throw new InvalidOperationException("No native thread material found");
    material=new Material(original); material.name="OCT_NaturalRope";
    if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",Texture2D.whiteTexture);
    if(material.HasProperty("_Color"))material.SetColor("_Color",new Color(.78f,.69f,.52f,1));
    if(material.HasProperty("_BumpMap"))material.SetTexture("_BumpMap",null);
    if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",0);
    if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0);
   }
   var visual=new GameObject(VisualName);visual.layer=item.gameObject.layer;
   visual.transform.SetParent(parent,false);
   visual.AddComponent<MeshFilter>().sharedMesh=rope;
   visual.AddComponent<MeshRenderer>().sharedMaterial=material;
   foreach(var renderer in renderers)renderer.enabled=false;
  }
  void OnDestroy(){if(harmony!=null)harmony.UnpatchSelf();}
 }
 internal static class RopeMesh {
  static readonly List<Vector3> vertices=new List<Vector3>();
  static readonly List<Vector2> uv=new List<Vector2>();
  static readonly List<int> triangles=new List<int>();
  internal static Mesh Build() {
   vertices.Clear();uv.Clear();triangles.Clear();
   // Five oval turns, each formed from three visibly twisted strands.
   for(int loop=0;loop<5;loop++) {
    float rx=.092f+loop*.009f, rz=.125f+loop*.009f, height=.014f+loop*.002f;
    Rope(t=>{float a=t*Mathf.PI*2;return new Vector3(rx*Mathf.Cos(a),height,rz*Mathf.Sin(a));},180,.006f,24,true);
   }
   // Binding around the gathered bottom of the coil.
   Rope(t=>{float a=t*Mathf.PI*6;return new Vector3(.014f*Mathf.Cos(a),.02f+.018f*Mathf.Sin(a),.14f+(t-.5f)*.035f);},90,.0045f,9,false);
   // Two hanging ends and a small finishing knot.
   for(int side=-1;side<=1;side+=2) {
    int direction=side;
    Rope(t=>new Vector3(direction*(.005f+.025f*t)+.005f*Mathf.Sin(t*8),.012f,.15f+.13f*t),64,.005f,9,false);
    for(int strand=0;strand<3;strand++) {
     int s=strand;
     Rope(t=>new Vector3(direction*.03f+(s-1)*.004f*t,.012f+.003f*s*t,.28f+.024f*t),12,.0014f,2,false);
    }
   }
   Rope(t=>{float a=t*Mathf.PI*4;return new Vector3(.024f*Mathf.Cos(a),.016f+.012f*Mathf.Sin(a),.235f+(t-.5f)*.018f);},64,.004f,6,false);
   var mesh=new Mesh();mesh.name="OCT_CoiledRope";
   mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
   mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  static void Rope(Func<float,Vector3> path,int steps,float radius,int twists,bool closed) {
   for(int strand=0;strand<3;strand++) {
    int offset=vertices.Count;
    for(int i=0;i<=steps;i++) {
     float t=(float)i/steps;
     float before=closed?(t-.001f+1)%1:Mathf.Max(0,t-.001f);
     float after=closed?(t+.001f)%1:Mathf.Min(1,t+.001f);
     var tangent=(path(after)-path(before)).normalized;
     var normal=Vector3.Cross(tangent,Vector3.up).normalized;
     if(normal.sqrMagnitude<.01f)normal=Vector3.right;
     var binormal=Vector3.Cross(tangent,normal).normalized;
     float twist=2*Mathf.PI*(t*twists+strand/3f);
     var center=path(t)+radius*.47f*(normal*Mathf.Cos(twist)+binormal*Mathf.Sin(twist));
     for(int j=0;j<6;j++) {
      float angle=j*Mathf.PI/3;
      vertices.Add(center+radius*.58f*(normal*Mathf.Cos(angle)+binormal*Mathf.Sin(angle)));
      uv.Add(new Vector2(j/6f,t*twists));
      if(i==steps)continue;
      int a=offset+i*6+j,b=offset+i*6+(j+1)%6,c=a+6,d=b+6;
      triangles.Add(a);triangles.Add(b);triangles.Add(c);
      triangles.Add(b);triangles.Add(d);triangles.Add(c);
     }
    }
   }
  }
 }
}
