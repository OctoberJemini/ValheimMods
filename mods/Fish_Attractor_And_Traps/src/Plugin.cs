using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace OctoberJemini.FishAttractor {
 [BepInPlugin(Guid,"Fish Attractor & Traps","2.0.5")]
 [BepInDependency(Jotunn.Main.ModGuid)]
 [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod,VersionStrictness.Minor)]
 public sealed class Plugin:BaseUnityPlugin {
  public const string Guid="OctoberJemini.FishAttractor";
  internal static ConfigEntry<float> Interval,FirstDelay,Depth;
  internal static ConfigEntry<int> Capacity,FishLimit;
  internal static ConfigEntry<string> ExtraMeats;
  private GameObject attractor,trap;
  private bool iconDone;
  private HarmonyLib.Harmony harmony;
  private TrapService service;
  private void Awake(){
   service=new TrapService();service.Register();
   harmony=new HarmonyLib.Harmony(Guid);harmony.PatchAll(typeof(Plugin).Assembly);
   Interval=Config.Bind("Attraction","Seconds between fish",300f,new ConfigDescription("World-time seconds between successful arrivals. No catch-up spawning burst.",new AcceptableValueRange<float>(30,3600)));
   FirstDelay=Config.Bind("Attraction","First fish delay",60f,new ConfigDescription("Seconds after the first bait is loaded before the first fish can arrive.",new AcceptableValueRange<float>(15,600)));
   FishLimit=Config.Bind("Attraction","Nearby fish limit",3,new ConfigDescription("Stops attracting fish when this many fish are within 25 metres. Nearby stations share the same fish count.",new AcceptableValueRange<int>(1,10)));
   Capacity=Config.Bind("Bait","Capacity",20,new ConfigDescription("Maximum pieces of manually loaded raw meat.",new AcceptableValueRange<int>(1,100)));
   Depth=Config.Bind("Attraction","Minimum water depth",1.5f,new ConfigDescription("Required water depth at a candidate arrival point.",new AcceptableValueRange<float>(.8f,5)));
   ExtraMeats=Config.Bind("Bait","Additional raw meat prefabs","","Optional comma-separated modded raw-meat prefab IDs. Cooked meals are not accepted by default.");
   PrefabManager.OnVanillaPrefabsAvailable+=Register;
   PieceManager.OnPiecesRegistered+=Icon;
  }
  private void Update(){if(service!=null)service.Update();}
  private void Register(){
   if(attractor)return;
   try{
    var hive=PrefabManager.Instance.GetPrefab("piece_beehive");
    var wood=PrefabManager.Instance.GetPrefab("wood_beam_1");
    if(!hive||!wood)throw new InvalidOperationException("Vanilla building assets unavailable.");
    var clone=PrefabManager.Instance.CreateClonedPrefab("oct_fish_attractor",hive);
    foreach(var b in clone.GetComponents<Beehive>())DestroyImmediate(b);
    for(int i=clone.transform.childCount-1;i>=0;i--)DestroyImmediate(clone.transform.GetChild(i).gameObject);
    foreach(var c in clone.GetComponents<Collider>())DestroyImmediate(c);
    var template=wood.GetComponent<WearNTear>();
    var renderer=template.m_new.GetComponentInChildren<MeshRenderer>(true);
    var filter=template.m_new.GetComponentInChildren<MeshFilter>(true);
    var mesh=NormalizeBeam(wood,filter);
    var wear=clone.GetComponent<WearNTear>();
    wear.m_new=MakeVisual(clone,"new",mesh,renderer.sharedMaterial);
    wear.m_worn=MakeVisual(clone,"worn",mesh,renderer.sharedMaterial);
    wear.m_broken=MakeVisual(clone,"broken",mesh,renderer.sharedMaterial);
    wear.m_worn.SetActive(false);wear.m_broken.SetActive(false);
    wear.m_fragmentRoots=new[]{wear.m_new};wear.m_comOffset=new Vector3(0,0,0);
    var collider=clone.AddComponent<BoxCollider>();collider.center=new Vector3(-.4f,.55f,0);collider.size=new Vector3(.18f,1.1f,.18f);
    var basketCollider=clone.AddComponent<BoxCollider>();basketCollider.center=new Vector3(.3f,-.75f,0);basketCollider.size=new Vector3(.6f,.5f,.6f);
    clone.transform.localRotation=Quaternion.identity;clone.transform.localScale=Vector3.one;
    clone.AddComponent<Attractor>();
    var piece=clone.GetComponent<Piece>();piece.m_noInWater=false;
    var cfg=new PieceConfig{Name="Fish Attractor",Description="Load raw meat as chum to attract ordinary live fish. Build on a low dock or shoreline beside water at least 1.5m deep. Your companion still needs a fishing rod and normal fishing bait.",PieceTable="Hammer",Category="BuildingWorkbench",CraftingStation="piece_workbench",Requirements=new[]{new RequirementConfig{Item="Wood",Amount=10,Recover=true},new RequirementConfig{Item="Resin",Amount=2,Recover=true}}};
    if(!PieceManager.Instance.AddPiece(new CustomPiece(clone,false,cfg)))throw new InvalidOperationException("Could not register fish attractor.");
    attractor=clone;RegisterTrap();PrefabManager.OnVanillaPrefabsAvailable-=Register;
    Logger.LogInfo("Fish Attractor registered: manually loaded meat, live perch/pike, owner-only spawning.");
   }catch(Exception e){Logger.LogError("Fish Attractor registration failed: "+e);}
  }
  private void RegisterTrap(){
   var source=PrefabManager.Instance.GetPrefab("piece_chest_wood");
   if(!source)throw new InvalidOperationException("Vanilla chest unavailable.");
   var clone=PrefabManager.Instance.CreateClonedPrefab("oct_fish_trap",source);
   var container=clone.GetComponent<Container>();container.m_name="Fish Trap";container.m_width=3;container.m_height=2;
   // Preserve the native chest's animation, interaction, colliders and saved inventory.
   // Hide its solid shell and render a slatted timber cage instead.
   foreach(var r in clone.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
   var wood=PrefabManager.Instance.GetPrefab("wood_beam_1");
   var model=wood.GetComponent<WearNTear>().m_new;
   var mesh=NormalizeBeam(wood,model.GetComponentInChildren<MeshFilter>(true));
   var material=model.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
   var wear=clone.GetComponent<WearNTear>();
   wear.m_new=MakeTrapVisual(clone,"TrapNew",mesh,material);
   wear.m_worn=MakeTrapVisual(clone,"TrapWorn",mesh,material);
   wear.m_broken=MakeTrapVisual(clone,"TrapBroken",mesh,material);
   wear.m_worn.SetActive(false);wear.m_broken.SetActive(false);wear.m_fragmentRoots=new[]{wear.m_new};
   clone.GetComponent<Piece>().m_noInWater=false;
   clone.AddComponent<FishTrap>();
   var cfg=new PieceConfig{Name="Fish Trap",Description="Load fishing bait and collect matching caught fish. Needs nearby water at least 1.5m deep. Discover fish in their native biome. Fishing skill improves speed and bait retention. Traps share fish stocks within 20m.",PieceTable="Hammer",Category="BuildingWorkbench",CraftingStation="piece_workbench",Requirements=new[]{new RequirementConfig{Item="Wood",Amount=20,Recover=true},new RequirementConfig{Item="FineWood",Amount=10,Recover=true},new RequirementConfig{Item="BronzeNails",Amount=10,Recover=true}}};
   if(!PieceManager.Instance.AddPiece(new CustomPiece(clone,false,cfg)))throw new InvalidOperationException("Could not register fish trap.");
   trap=clone;
   Logger.LogInfo("Fish Trap registered; bait matches are read from native fish definitions when the world loads.");
  }
  private static GameObject MakeTrapVisual(GameObject parent,string name,Mesh mesh,Material wood){
   var go=new GameObject(name);go.layer=parent.layer;go.transform.SetParent(parent.transform,false);
   const float x=.7f,z=.45f,bottom=.08f,top=.75f;
   foreach(float y in new[]{bottom,top}){
    Beam(go,mesh,wood,new Vector3(-x,y,-z),new Vector3(x,y,-z),.07f);
    Beam(go,mesh,wood,new Vector3(-x,y,z),new Vector3(x,y,z),.07f);
    Beam(go,mesh,wood,new Vector3(-x,y,-z),new Vector3(-x,y,z),.07f);
    Beam(go,mesh,wood,new Vector3(x,y,-z),new Vector3(x,y,z),.07f);
   }
   for(int i=0;i<=10;i++){
    float px=-x+2*x*i/10;
    foreach(float pz in new[]{-z,z})Beam(go,mesh,wood,new Vector3(px,bottom,pz),new Vector3(px,top,pz),.035f);
    foreach(float y in new[]{bottom,top})Beam(go,mesh,wood,new Vector3(px,y,-z),new Vector3(px,y,z),.035f);
   }
   for(int i=1;i<6;i++){
    float pz=-z+2*z*i/6;
    foreach(float px in new[]{-x,x})Beam(go,mesh,wood,new Vector3(px,bottom,pz),new Vector3(px,top,pz),.035f);
   }
   // Raised carry handle makes the trap distinct from an ordinary storage chest.
   Beam(go,mesh,wood,new Vector3(-.2f,top,0),new Vector3(-.2f,top+.15f,0),.05f);
   Beam(go,mesh,wood,new Vector3(.2f,top,0),new Vector3(.2f,top+.15f,0),.05f);
   Beam(go,mesh,wood,new Vector3(-.2f,top+.15f,0),new Vector3(.2f,top+.15f,0),.05f);
   Combine(go,wood);return go;
  }
  private static Mesh NormalizeBeam(GameObject source,MeshFilter filter){
   var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var v=mesh.vertices;
   var matrix=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
   var bounds=new Bounds(matrix.MultiplyPoint3x4(v[0]),Vector3.zero);
   for(int i=0;i<v.Length;i++){v[i]=matrix.MultiplyPoint3x4(v[i]);bounds.Encapsulate(v[i]);}
   var s=bounds.size;int axis=s.x>=s.y&&s.x>=s.z?0:(s.y>=s.z?1:2);
   var rotation=Quaternion.FromToRotation(axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward,Vector3.forward);
   for(int i=0;i<v.Length;i++){var q=v[i]-bounds.center;v[i]=rotation*new Vector3(q.x/s.x,q.y/s.y,q.z/s.z);}
   mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  private static GameObject MakeVisual(GameObject parent,string name,Mesh mesh,Material wood){
   var go=new GameObject(name);go.layer=parent.layer;go.transform.SetParent(parent.transform,false);
   var post=new GameObject("Post");post.transform.SetParent(go.transform,false);
   var basket=new GameObject("BaitBasket");basket.transform.SetParent(go.transform,false);
   Beam(post,mesh,wood,new Vector3(-.4f,0,0),new Vector3(-.4f,1.1f,0),.14f);
   Beam(post,mesh,wood,new Vector3(-.5f,1.05f,0),new Vector3(.4f,1.05f,0),.1f);
   var rope=Beam(go,mesh,wood,new Vector3(.3f,1.02f,0),new Vector3(.3f,-.5f,0),.025f);rope.name="BaitRope";
   for(int i=0;i<12;i++){
    float a=i*Mathf.PI*2/12,b=(i+1)*Mathf.PI*2/12;
    var bottom=new Vector3(.3f+Mathf.Cos(a)*.28f,-1,Mathf.Sin(a)*.28f);
    var top=new Vector3(.3f+Mathf.Cos(a)*.34f,-.5f,Mathf.Sin(a)*.34f);
    Beam(basket,mesh,wood,bottom,top,.035f);
    Beam(basket,mesh,wood,bottom,new Vector3(.3f+Mathf.Cos(b)*.28f,-1,Mathf.Sin(b)*.28f),.04f);
    Beam(basket,mesh,wood,top,new Vector3(.3f+Mathf.Cos(b)*.34f,-.5f,Mathf.Sin(b)*.34f),.04f);
    if(i%3==0)Beam(basket,mesh,wood,bottom,new Vector3(.3f,-1,0),.035f);
   }
   Combine(post,wood);Combine(basket,wood);return go;
  }
  private static GameObject Beam(GameObject parent,Mesh mesh,Material wood,Vector3 a,Vector3 b,float width){
   var go=new GameObject("Timber");go.layer=parent.layer;go.transform.SetParent(parent.transform,false);
   go.transform.localPosition=(a+b)/2;go.transform.localRotation=Quaternion.LookRotation(b-a);go.transform.localScale=new Vector3(width,width,Vector3.Distance(a,b));
   go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=wood;return go;
  }
  private static void Combine(GameObject group,Material material){
   var filters=group.GetComponentsInChildren<MeshFilter>();var parts=new CombineInstance[filters.Length];
   for(int i=0;i<parts.Length;i++)parts[i]=new CombineInstance{mesh=filters[i].sharedMesh,transform=group.transform.worldToLocalMatrix*filters[i].transform.localToWorldMatrix};
   var mesh=new Mesh();mesh.CombineMeshes(parts,true,true);
   foreach(var filter in filters)UnityEngine.Object.DestroyImmediate(filter.gameObject);
   group.AddComponent<MeshFilter>().sharedMesh=mesh;group.AddComponent<MeshRenderer>().sharedMaterial=material;
  }
  // Render an isolated display model: the native building shader depends on the
  // world render path, while Jotunn's icon camera is a separate forward camera.
  // Keep the authored wood texture and tint; never change the placed-piece materials.
  private Sprite RenderWoodIcon(GameObject prefab){
   var woodItem=PrefabManager.Instance.GetPrefab("Wood"); var itemRenderer=woodItem?woodItem.GetComponentInChildren<MeshRenderer>(true):null;
   if(!itemRenderer || !itemRenderer.sharedMaterial)throw new InvalidOperationException("Native wood item material unavailable");
   var display=new GameObject(prefab.name+"_icon");display.SetActive(false);
   var materials=new List<Material>();
   var oldAmbientMode=RenderSettings.ambientMode;var oldAmbient=RenderSettings.ambientLight;var oldIntensity=RenderSettings.ambientIntensity;var oldFog=RenderSettings.fog;
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.65f,.65f);RenderSettings.ambientIntensity=1;RenderSettings.fog=false;
   try{
    var visual=prefab.GetComponent<WearNTear>().m_new;
    foreach(var source in visual.GetComponentsInChildren<MeshRenderer>(true)){
     if(!source.enabled)continue;
     var filter=source.GetComponent<MeshFilter>();if(!filter || !filter.sharedMesh)continue;
     var part=new GameObject("Timber");part.transform.SetParent(display.transform,false);
     part.transform.localPosition=prefab.transform.InverseTransformPoint(source.transform.position);part.transform.localRotation=Quaternion.Inverse(prefab.transform.rotation)*source.transform.rotation;
     var rootScale=prefab.transform.lossyScale;var sourceScale=source.transform.lossyScale; part.transform.localScale=new Vector3(sourceScale.x/rootScale.x,sourceScale.y/rootScale.y,sourceScale.z/rootScale.z);
     var native=source.sharedMaterial;
     var material=new Material(itemRenderer.sharedMaterial);materials.Add(material);
     material.mainTexture=native.GetTexture("_MainTex");
     material.mainTextureScale=native.GetTextureScale("_MainTex");
     material.mainTextureOffset=native.GetTextureOffset("_MainTex");
     material.color=native.HasProperty("_Color")?native.GetColor("_Color"):Color.white;
     material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",0); material.SetFloat("_BumpScale",0); material.SetFloat("_AddRain",0); material.SetFloat("_ValueNoise",0); material.SetFloat("_ValueNoiseVertex",0); material.DisableKeyword("_NORMALMAP"); material.SetColor("_EmissionColor",Color.black);
     part.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
     part.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    // Move the display as a whole; Jotunn recentres its clone before capture.
    display.transform.position=new Vector3(0,-10000,0);display.SetActive(true);
    var icon=RenderManager.Instance.Render(new RenderManager.RenderRequest(display){
     Rotation=RenderManager.IsometricRotation,Width=512,Height=512,UseCache=false,TargetPlugin=Info.Metadata
    });
    if(!icon || !icon.texture || !icon.texture.GetPixels32().Any(pixel=>pixel.a>16))throw new InvalidOperationException("Icon render is empty; retaining previous icon");
    icon.texture.filterMode=FilterMode.Bilinear;return icon;
   }finally{RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientIntensity=oldIntensity;RenderSettings.fog=oldFog;DestroyImmediate(display);foreach(var material in materials)DestroyImmediate(material);}
  }
  private void Icon(){
   if(!attractor||!trap||iconDone||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
   try{
    var attractorIcon=RenderWoodIcon(attractor);var trapIcon=RenderWoodIcon(trap);
    if(!attractorIcon||!trapIcon)throw new InvalidOperationException("Icon render returned no image");
    attractor.GetComponent<Piece>().m_icon=attractorIcon;trap.GetComponent<Piece>().m_icon=trapIcon;iconDone=true;
   }catch(Exception e){Logger.LogWarning("Fish Attractor icon: "+e.Message);}
  }
  private void OnDestroy(){PrefabManager.OnVanillaPrefabsAvailable-=Register;PieceManager.OnPiecesRegistered-=Icon;if(harmony!=null)harmony.UnpatchSelf();}
 }
 public sealed class Attractor:MonoBehaviour,Hoverable,Interactable {
  private const string BaitKey="oct_fish_bait",DueKey="oct_fish_due",StatusKey="oct_fish_status";
  private ZNetView view;
  private BoxCollider basketCollider;
  private readonly List<Transform> baskets=new List<Transform>(),ropes=new List<Transform>();
  private void Awake(){
   view=GetComponent<ZNetView>();if(!view||!view.IsValid())return;
   basketCollider=GetComponents<BoxCollider>().FirstOrDefault(c=>c.center.x>0);
   view.Register<string>("OCT_AddChum",AddChum);
   var wear=GetComponent<WearNTear>();if(wear){
    wear.m_onDestroyed+=ReturnBait;
    foreach(var state in new[]{wear.m_new,wear.m_worn,wear.m_broken})if(state){baskets.Add(state.transform.Find("BaitBasket"));ropes.Add(state.transform.Find("BaitRope"));}
   }
   InvokeRepeating("Tick",UnityEngine.Random.Range(3f,8f),15f);
  }
  private List<string> Bait(){return BaitPolicy.Read(view.GetZDO().GetString(BaitKey,""));}
  private static string Prefab(ItemDrop.ItemData item){return item!=null&&item.m_dropPrefab?item.m_dropPrefab.name:"";}
  private ItemDrop.ItemData NextMeat(Humanoid user){return user.GetInventory().GetAllItems().FirstOrDefault(i=>BaitPolicy.Allowed(Prefab(i),Plugin.ExtraMeats.Value));}
  public string GetHoverName(){return "Fish Attractor";}
  public float GetHoverOffset(){return 0f;}
  public string GetHoverText(){
   if(!view||!view.IsValid())return "Fish Attractor";
   var meat=Player.m_localPlayer?NextMeat(Player.m_localPlayer):null;
   string action=meat!=null?"Add 1 "+Localization.instance.Localize(meat.m_shared.m_name):"Add raw meat";
   return Localization.instance.Localize("Fish Attractor\nChum: "+Bait().Count+"/"+Plugin.Capacity.Value+"\n"+view.GetZDO().GetString(StatusKey,"Load raw meat to begin")+"\n[<color=yellow><b>$KEY_Use</b></color>] "+action+"\nUse a raw-meat hotbar item while pointing here to choose the meat.");
  }
  public bool Interact(Humanoid user,bool hold,bool alt){if(hold)return false;var item=NextMeat(user);if(item==null){user.Message(MessageHud.MessageType.Center,"Carry raw meat to load chum.");return true;}return UseItem(user,item);}
  public bool UseItem(Humanoid user,ItemDrop.ItemData item){
   if(!BaitPolicy.Allowed(Prefab(item),Plugin.ExtraMeats.Value))return false;
   if(!view||!view.IsValid()||!PrivateArea.CheckAccess(transform.position,0,true,false))return true;
   if(Bait().Count>=Plugin.Capacity.Value){user.Message(MessageHud.MessageType.Center,"The chum basket is full.");return true;}
   string prefab=Prefab(item);if(!user.GetInventory().RemoveItem(item,1))return true;
   view.InvokeRPC("OCT_AddChum",prefab);return true;
  }
  private void AddChum(long sender,string prefab){
   if(!view.IsOwner()||!BaitPolicy.Allowed(prefab,Plugin.ExtraMeats.Value))return;
   var bait=Bait();if(bait.Count>=Plugin.Capacity.Value){Drop(prefab,1);return;}
   bait.Add(prefab);var zdo=view.GetZDO();zdo.Set(BaitKey,BaitPolicy.Write(bait));
   if(zdo.GetLong(DueKey,0)==0)zdo.Set(DueKey,ZNet.instance.GetTime().AddSeconds(Plugin.FirstDelay.Value).Ticks);
   zdo.Set(StatusKey,"Attracting fish");
  }
  private void Tick(){
   if(!view||!view.IsValid()||!ZoneSystem.instance)return;
   UpdateBasket();
   if(!view.IsOwner()||!ZNet.instance)return;
   try{
    var zdo=view.GetZDO();var bait=Bait();if(bait.Count==0){zdo.Set(DueKey,0L);zdo.Set(StatusKey,"Needs raw meat");return;}
    long now=ZNet.instance.GetTime().Ticks,due=zdo.GetLong(DueKey,0);
    if(due==0){zdo.Set(DueKey,ZNet.instance.GetTime().AddSeconds(Plugin.FirstDelay.Value).Ticks);return;}
    if(now<due)return;
    int count=0;foreach(var update in Fish.Instances){var fish=update as Fish;if(fish&&(fish.transform.position-transform.position).sqrMagnitude<25*25)count++;}
    if(!BaitPolicy.Ready(bait.Count,count,Plugin.FishLimit.Value,now,due)){zdo.Set(StatusKey,"Fish already nearby");return;}
    Vector3 point;if(!FindWater(out point)){zdo.Set(StatusKey,"Needs deeper water nearby");return;}
    var prefab=PrefabManager.Instance.GetPrefab(UnityEngine.Random.value<.65f?"Fish1":"Fish2");if(!prefab){zdo.Set(StatusKey,"Fish prefab unavailable");return;}
    var spawned=UnityEngine.Object.Instantiate(prefab,point,Quaternion.Euler(0,UnityEngine.Random.Range(0,360f),0));
    if(!spawned)return;
    bait.RemoveAt(0);zdo.Set(BaitKey,BaitPolicy.Write(bait));zdo.Set(DueKey,ZNet.instance.GetTime().AddSeconds(Plugin.Interval.Value).Ticks);zdo.Set(StatusKey,"Fish arrived nearby");
   }catch(Exception e){Debug.LogWarning("[Fish Attractor] Attraction paused: "+e.Message);CancelInvoke("Tick");}
  }
  private void UpdateBasket(){
   float water=Floating.GetLiquidLevel(transform.position-Vector3.up*4f,0f,LiquidType.Water);
   if(water<-1000||Mathf.Abs(water-transform.position.y)>4)return;
   float offset=water-transform.position.y;
   if(basketCollider)basketCollider.center=new Vector3(.3f,-.75f+offset,0);
   foreach(var basket in baskets)if(basket)basket.localPosition=new Vector3(0,offset,0);
   foreach(var rope in ropes)if(rope){rope.localPosition=new Vector3(.3f,(1.02f+offset-.5f)/2f,0);rope.localScale=new Vector3(.025f,.025f,1.52f-offset);}
  }
  private bool FindWater(out Vector3 point){
   for(int i=0;i<12;i++){
    float angle=UnityEngine.Random.Range(0,Mathf.PI*2),radius=UnityEngine.Random.Range(4f,8f);
    var p=transform.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
    float water=Floating.GetLiquidLevel(new Vector3(p.x,transform.position.y-4f,p.z),0f,LiquidType.Water),ground=ZoneSystem.instance.GetGroundHeight(p);
    if(BaitPolicy.WaterSuitable(water,ground,Plugin.Depth.Value,transform.position.y)){point=new Vector3(p.x,water-.6f,p.z);return true;}
   }
   point=Vector3.zero;return false;
  }
  private void Drop(string prefab,int count){
   var item=PrefabManager.Instance.GetPrefab(prefab);if(!item)return;
   var source=item.GetComponent<ItemDrop>();if(!source)return;
   var data=source.m_itemData.Clone();data.m_dropPrefab=item;
   while(count>0){int amount=Math.Min(count,Math.Max(1,data.m_shared.m_maxStackSize));ItemDrop.DropItem(data,amount,transform.position+Vector3.up*.5f,Quaternion.identity);count-=amount;}
  }
  private void ReturnBait(){if(!view||!view.IsValid()||!view.IsOwner())return;var bait=Bait();view.GetZDO().Set(BaitKey,"");foreach(var group in bait.GroupBy(s=>s))Drop(group.Key,group.Count());}
 }
}
