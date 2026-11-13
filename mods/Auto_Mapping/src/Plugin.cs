using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace OctoberJemini.QuietMap {
 [BepInPlugin(Guid,"Auto Mapping","1.1.4")]
 [BepInDependency("digi.valheim.automappins",BepInDependency.DependencyFlags.SoftDependency)]
 [BepInDependency("com.noodle.autopoi",BepInDependency.DependencyFlags.SoftDependency)]
 [BepInDependency("com.oathorse.DWMP",BepInDependency.DependencyFlags.SoftDependency)]
 public sealed partial class Plugin:BaseUnityPlugin {
  public const string Guid="OctoberJemini.QuietMap";
  internal static Plugin Instance;
  ConfigEntry<bool> enabledSetting,dungeons,portals,markVisited;
  ConfigEntry<float> portalScale,landmarkScale,visitedOpacity;
  ConfigEntry<string> extraDungeons;
  readonly Dictionary<string,Record> records=new Dictionary<string,Record>();
  readonly Dictionary<string,Pin> pins=new Dictionary<string,Pin>();
  readonly Dictionary<string,int> missing=new Dictionary<string,int>();
  readonly HashSet<string> warned=new HashSet<string>();
  readonly HashSet<string> insideLocations=new HashSet<string>();
  static readonly FieldInfo Instances=AccessTools.Field(typeof(ZNetScene),"m_instances");
  static readonly FieldInfo Locations=AccessTools.Field(typeof(Location),"s_allLocations");
  static readonly FieldInfo GamePins=AccessTools.Field(typeof(Minimap),"m_pins");
  static readonly FieldInfo LocationPins=AccessTools.Field(typeof(Minimap),"m_locationPins");
  readonly HashSet<Minimap.PinData> vendorPins=new HashSet<Minimap.PinData>();
  readonly Dictionary<Transform,Vector3> nativeScales=new Dictionary<Transform,Vector3>();
  float nextLandmarkCheck;
  static readonly MethodInfo MapData=AccessTools.Method(typeof(Minimap),"GetMapData");
  Harmony harmony;
  string session,path;
  Minimap map;
  float nextTick,nextSave;
  float effectiveRadius;
  bool localExploration;
  float DiscoveryRadius=>effectiveRadius>0?effectiveRadius:(map?map.m_exploreRadius:0);
  static bool WithinDiscovery(Vector3 point,Vector3 player,float distance){
   if(Character.InInterior(point)!=Character.InInterior(player))return false;
   float dx=point.x-player.x,dz=point.z-player.z;
   return distance>0&&dx*dx+dz*dz<=distance*distance;
  }
  // Observe the actual radius after exploration modifiers have run. Scope to the
  // normal local-player reveal so map-table imports and reveal cheats cannot set it.
  [HarmonyPatch(typeof(Minimap),"UpdateExplore")]
  static class ExplorationScopePatch {
   static void Prefix(Player __1){if(Instance!=null)Instance.localExploration=__1&&__1==Player.m_localPlayer;}
   static void Finalizer(){if(Instance!=null)Instance.localExploration=false;}
  }
  [HarmonyPatch(typeof(Minimap),"Explore",typeof(Vector3),typeof(float))]
  static class ExplorationRadiusPatch {
   static void Postfix(float __1){var self=Instance;if(self!=null&&self.localExploration&&__1>0&&!float.IsNaN(__1)&&!float.IsInfinity(__1))self.effectiveRadius=__1;}
  }
  bool dirty,internalRemove,ready,loadFailed;
  sealed class Pin {public Minimap.PinData Data;public List<string> Keys;public string Kind;public bool Borrowed;}
  public void Awake(){
   Instance=this;
   enabledSetting=Config.Bind("General","Enabled",true,"Automatically maintain discovered map markers. No other map mod is required.");
   dungeons=Config.Bind("Tracking","Dungeons",true,"Pin encountered dungeons, outdoor landmarks and More World Locations POIs. No resource scans.");
   portals=Config.Bind("Tracking","Portals",true,"Automatically pin placed or discovered portals, update their tag, and remove destroyed portals.");
   markVisited=Config.Bind("Tracking","Cross off visited locations",true,"Apply the normal map X when entering a dungeon or outdoor landmark. Visited, not fully looted. Portals, bosses, traders and ports are not auto-crossed.");
   visitedOpacity=Config.Bind("Appearance","Visited pin opacity",.4f,new ConfigDescription("Opacity of crossed-out location icons and labels; the X stays visible.",new AcceptableValueRange<float>(.1f,1f)));
   portalScale=Config.Bind("Appearance","Portal icon scale",0.85f,new ConfigDescription("Size of the game's familiar portal symbol.",new AcceptableValueRange<float>(.4f,1.5f)));
   landmarkScale=Config.Bind("Appearance","Boss and vendor icon scale",1f,new ConfigDescription("Final icon size relative to a standard map pin, compensating for oversized vanilla markers. Default is slightly larger than Quiet Map's 0.85 portal icons. Artwork and pin labels are preserved.",new AcceptableValueRange<float>(.5f,1.5f)));
   extraDungeons=Config.Bind("Additional objects","Dungeon prefabs","","Comma-separated location prefab names to recognize as dungeons.");
   foreach(string other in new[]{"digi.valheim.automappins","com.noodle.autopoi","com.oathorse.DWMP"})
    if(Chainloader.PluginInfos.ContainsKey(other)){Logger.LogError("Quiet Map inactive to prevent duplicate pins. Disable "+other+" and restart.");return;}
   try{
    if(Instances==null||Locations==null||GamePins==null)throw new MissingMemberException("Game map/object tracking API changed");
    harmony=new Harmony(Guid);harmony.PatchAll(typeof(Plugin).Assembly);ready=true;
    Logger.LogInfo("Auto Mapping landmark mode ready. No chest, ore, pickup or dungeon-loot scanning.");
   }catch(Exception e){harmony?.UnpatchSelf();Logger.LogError("Quiet Map inactive: "+e);}
  }
  static string Clean(string name)=>(name??"").Replace("(Clone)","").Trim();
  static string Key(ZDOID id)=>"z:"+id.UserID.ToString(CultureInfo.InvariantCulture)+":"+id.ID.ToString(CultureInfo.InvariantCulture);
  static Vector3 Position(Record r)=>new Vector3(r.X,r.Y,r.Z);
  static bool Extra(ConfigEntry<string> entry,string name)=>entry.Value.Split(',').Any(s=>string.Equals(s.Trim(),name,StringComparison.OrdinalIgnoreCase));
  static string Display(string raw){
   if(string.IsNullOrEmpty(raw))return "Unknown";
   string result=Localization.instance!=null?Localization.instance.Localize(raw):raw;
   if(!result.StartsWith("$"))return result;
   return Regex.Replace(result.TrimStart('$').Replace("piece_","").Replace("item_","").Replace('_',' '),"([a-z])([A-Z])","$1 $2");
  }
  static string Readable(string raw)=>Regex.Replace(Clean(raw).Replace('_',' '),"([a-z])([A-Z])","$1 $2");
  static string DungeonName(string raw){
   string n=raw.ToLowerInvariant();
   if(n.StartsWith("sunkencrypt"))return "Sunken Crypt";
   if(n.StartsWith("crypt"))return "Burial Chambers";
   if(n.StartsWith("trollcave"))return "Troll Cave";
   if(n.StartsWith("mountaincave"))return "Frost Cave";
   if(n.Contains("dvergrtownentrance"))return "Infested Mine";
   if(n.StartsWith("hildir_crypt"))return "Smouldering Tomb";
   if(n.StartsWith("hildir_cave"))return "Howling Cavern";
   if(n.StartsWith("hildir_plainsfortress"))return "Sealed Tower";
   if(n.StartsWith("morgenhole"))return "Putrid Hole";
   if(n.StartsWith("morkborg")||n.Contains("morkhalla"))return "Mörkhalla";
   if(n.StartsWith("thehole")||n.StartsWith("the_hole"))return "Winding Tunnels";
   if(n.StartsWith("bearcave"))return "Bear Cave";
   return Readable(raw);
  }
  void Update(){
   if(!ready)return;
   try{
    var player=Player.m_localPlayer;
    if(!player||!Minimap.instance||!ZNet.instance){if(session!=null)EndSession();return;}
    string key=ZNet.instance.GetWorldUID().ToString(CultureInfo.InvariantCulture)+"-"+player.GetPlayerID().ToString(CultureInfo.InvariantCulture);
    if(session!=key||map!=Minimap.instance){EndSession();StartSession(key);}
    if(loadFailed)return;
    if(Time.unscaledTime<nextTick)return;
    nextTick=Time.unscaledTime+2;
    if(enabledSetting.Value){Scan(player.transform.position);Render(player.transform.position);}else ClearPins();
    if(Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+10;Save();}
   }catch(Exception e){Warn("tick",e);}
  }
  void StartSession(string key){
   session=key;map=Minimap.instance;path=Path.Combine(Paths.ConfigPath,"QuietMap",key+".json");loadFailed=false;
   if(File.Exists(path))try{
    var data=JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path));
    if(data==null||data.Version!=1)throw new InvalidDataException("Unsupported or empty map save");
    foreach(var r in data.Records)if(r!=null&&!string.IsNullOrEmpty(r.Key))records[r.Key]=r;
   }catch(Exception e){loadFailed=true;Warn("load",e);return;}
   else ImportPriorDiscoveries();
   foreach(var r in records.Values.ToArray()){
    if(!LandmarkPolicy.KeepRecord(r.Kind)){records.Remove(r.Key);dirty=true;continue;}
    if(r.Kind!="Portal"&&!string.IsNullOrEmpty(r.CompletionReason)){r.Available=true;r.Visited=true;r.CompletionReason=null;dirty=true;}
   }
   Save();nextTick=0;nextSave=Time.unscaledTime+10;
   Logger.LogInfo("Quiet Map loaded "+records.Count+" known markers for world/character "+key);
  }
  static string FileKey(string value)=>new string((value??"").Select(c=>char.IsLetterOrDigit(c)||c=='_'||c=='-'?c:'_').ToArray());
  void ImportPriorDiscoveries(){
   try{
    var profile=Game.instance?Game.instance.GetPlayerProfile():null;if(profile==null)return;
    string file=Path.Combine(Paths.ConfigPath,"DigiAutoMapPins",FileKey(ZNet.instance.GetWorldName())+"-"+ZNet.instance.GetWorldUID()+"."+FileKey(profile.GetFilename())+".txt");
    if(!File.Exists(file))return;
    foreach(string line in File.ReadAllLines(file)){
     var r=Migration.Read(line);
     if(r==null)continue;
     if(r.Kind=="Dungeon")r.Name=DungeonName(r.Prefab);
     records[r.Key]=r;dirty=true;
    }
    Save();Logger.LogInfo("Imported "+records.Count+" known portal/dungeon markers from this character's former map tracker; original file retained.");
   }catch(Exception e){Warn("prior map import",e);}
  }
  void EndSession(){effectiveRadius=0;localExploration=false;Save();ClearPins();RestoreNativeSizes();vendorPins.Clear();nextLandmarkCheck=0;records.Clear();missing.Clear();insideLocations.Clear();session=null;path=null;map=null;dirty=false;loadFailed=false;}
  void Save(){
   if(!dirty||path==null||loadFailed)return;
   try{
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    string tmp=path+".tmp";
    File.WriteAllText(tmp,JsonConvert.SerializeObject(new SaveData{Records=records.Values.ToList()},Formatting.Indented));
    if(File.Exists(path))File.Replace(tmp,path,path+".bak");else File.Move(tmp,path);
    dirty=false;
   }catch(Exception e){Warn("save",e);}
  }
  void Warn(string context,Exception e){if(warned.Add(context))Logger.LogError("Quiet Map "+context+": "+e);}
  void Observe(string key,string kind,string name,string prefab,Vector3 pos,bool available,ZDO zdo=null){
   if(!records.TryGetValue(key,out var r)){
    var prior=records.Values.FirstOrDefault(p=>p.Key.StartsWith("legacy:",StringComparison.Ordinal)&&p.Kind==kind&&(Position(p)-pos).sqrMagnitude<4);
    if(prior!=null){records.Remove(prior.Key);prior.Key=key;r=prior;records[key]=r;if(zdo!=null){r.User=zdo.m_uid.UserID;r.Id=zdo.m_uid.ID;}dirty=true;}
   }
   if(r==null){
    r=new Record{Key=key,Kind=kind,Name=name,Prefab=prefab,X=pos.x,Y=pos.y,Z=pos.z};
    if(zdo!=null){r.User=zdo.m_uid.UserID;r.Id=zdo.m_uid.ID;}
    records.Add(key,r);dirty=true;
   }
   if(r.Available!=available||r.Name!=name||(Position(r)-pos).sqrMagnitude>.01f){r.Available=available;r.Name=name;r.X=pos.x;r.Y=pos.y;r.Z=pos.z;dirty=true;}
   missing.Remove(key);
  }
  void Scan(Vector3 player){
   var scene=ZNetScene.instance;if(!scene)return;
   var instances=Instances.GetValue(scene) as Dictionary<ZDO,ZNetView>;
   if(instances==null)return;
   if(portals.Value)foreach(var pair in instances.ToArray()){
    var view=pair.Value;if(!view||!view.IsValid()||!WithinDiscovery(view.transform.position,player,DiscoveryRadius))continue;
    var portal=view.GetComponent<TeleportWorld>();if(portal)TrackPortal(portal,false);
   }
   ScanLocations(player);
   foreach(var r in records.Values.Where(r=>r.Kind=="Portal"&&r.Id!=0&&r.Available).ToArray()){
    bool near=(Position(r)-player).sqrMagnitude<1600;
    if(!near||!ZoneSystem.instance||!ZoneSystem.instance.IsZoneLoaded(Position(r)))continue;
    var savedObject=ZDOMan.instance?.GetZDO(new ZDOID(r.User,r.Id));
    // A recycled network ID must not keep an unrelated portal marker alive.
    bool exists=savedObject!=null &&
     (r.Prefab=="portal" || savedObject.GetPrefab()==r.Prefab.GetStableHashCode()) &&
     (savedObject.GetPosition()-Position(r)).sqrMagnitude<4f;
    int misses=exists?0:(missing.TryGetValue(r.Key,out var count)?count+1:1);missing[r.Key]=misses;
    if(Policy.MissingMeansDestroyed(near,true,exists,misses)){r.Available=false;dirty=true;}
   }
  }
  // Network destruction is distinct from unloading an object when travelling away.
  [HarmonyPatch(typeof(ZDOMan), "HandleDestroyedZDO")]
  static class PortalDestroyedPatch {
   static void Postfix(ZDOID __0) {
    var self=Instance;
    if(self==null||!self.ready||self.loadFailed||self.session==null)return;
    try {
     if(ZDOMan.instance!=null&&ZDOMan.instance.GetZDO(__0)!=null)return;
     if(!self.records.TryGetValue(Key(__0),out var record)||record.Kind!="Portal")return;
     record.Available=false;
     self.missing.Remove(record.Key);
     self.dirty=true;
     // Render immediately so grouped markers are rebuilt from the surviving portals.
     if(self.map&&Player.m_localPlayer)self.Render(Player.m_localPlayer.transform.position);
     self.Save();
    }catch(Exception error){self.Warn("destroyed portal marker",error);}
   }
  }
  void TrackPortal(TeleportWorld portal,bool interacted){
   if(!ready||loadFailed||session==null||!enabledSetting.Value||!portals.Value||!portal)return;
   var view=portal.GetComponent<ZNetView>();if(!view||!view.IsValid())return;
   var zdo=view.GetZDO();string key=Key(zdo.m_uid),prefab=Clean(view.gameObject.name);
   string tag=zdo.GetString(ZDOVars.s_tag,"");
   Observe(key,"Portal",string.IsNullOrWhiteSpace(tag)?"Portal":tag,prefab,view.transform.position,true,zdo);
   var record=records[key];
   if(Policy.RestorePortal(record,interacted)){record.Deleted=false;dirty=true;}
   // Legacy imports use the placeholder "portal". Replace it with the actual
   // prefab after rediscovery so a later manual deletion remains respected.
   if(record.Prefab!=prefab){record.Prefab=prefab;dirty=true;}
   if(interacted){nextTick=0;Save();if(map&&Player.m_localPlayer)Render(Player.m_localPlayer.transform.position);}
  }
  void PortalInteraction(TeleportWorld portal,Humanoid user){
   if(!user||user!=Player.m_localPlayer)return;
   try{TrackPortal(portal,true);}catch(Exception e){Warn("portal interaction",e);}
  }
  [HarmonyPatch(typeof(TeleportWorld),"Interact")]
  static class PortalInteractPatch {static void Postfix(TeleportWorld __instance,Humanoid __0,bool __1){if(!__1)Instance?.PortalInteraction(__instance,__0);}}
  [HarmonyPatch(typeof(TeleportWorld),"Teleport")]
  static class PortalUsePatch {static void Prefix(TeleportWorld __instance,Player __0){Instance?.PortalInteraction(__instance,__0);}}
  [HarmonyPatch(typeof(TeleportWorld),"SetText")]
  static class PortalRenamePatch {static void Postfix(TeleportWorld __instance){Instance?.PortalInteraction(__instance,Player.m_localPlayer);}}
  string LocationName(string name){
   if(LandmarkPolicy.Aio.TryGetValue(name??"",out var entry))return entry.Name;
   return Policy.IsDungeonName(name)?DungeonName(name):LandmarkPolicy.OutdoorName(name)??DungeonName(name);
  }
  bool Recognized(string name)=>LandmarkPolicy.Aio.ContainsKey(name??"")||LandmarkPolicy.OutdoorName(name)!=null||Policy.IsDungeonName(name)||Extra(extraDungeons,name);
  Record ObserveLandmark(string name,Vector3 pos){
   var existing=records.Values.FirstOrDefault(r=>r.Kind!="Portal"&&(Position(r)-pos).sqrMagnitude<100);
   if(existing!=null){string label=LocationName(name);if(existing.Name!=label){existing.Name=label;dirty=true;}return existing;}
   string key="place:"+Mathf.RoundToInt(pos.x)+":"+Mathf.RoundToInt(pos.z);
   Observe(key,"Landmark",LocationName(name),name,pos,true);
   return records[key];
  }
  void SetVisited(Record record){
   if(!markVisited.Value||record==null||LandmarkPolicy.IsService(record.Prefab))return;
   if(!record.Visited){record.Visited=true;dirty=true;}
   foreach(var pin in pins.Values)if(pin.Keys.Contains(record.Key))pin.Data.m_checked=true;
  }
  void ScanLocations(Vector3 player){
   if(!dungeons.Value||!ZoneSystem.instance){insideLocations.Clear();return;}
   bool interior=Character.InInterior(player);
   var current=interior?Location.GetLocation(player,true):null;
   var loaded=Locations.GetValue(null) as List<Location>;
   var entered=new HashSet<string>();
   foreach(var location in ZoneSystem.instance.GetLocationList()){
    if(!location.m_placed||location.m_location==null)continue;
    string name=location.m_location.m_prefabName;if(!Recognized(name))continue;
    var pos=location.m_position;
    bool sameInterior=current&&(current.transform.position-pos).sqrMagnitude<100;
    if(!sameInterior&&!WithinDiscovery(pos,player,DiscoveryRadius))continue;
    var record=ObserveLandmark(name,pos);
    var live=loaded?.FirstOrDefault(l=>l&&(l.transform.position-pos).sqrMagnitude<100);
    bool hasInterior=live?live.m_hasInterior:location.m_location.m_interiorRadius>0;
    float dx=player.x-pos.x,dz=player.z-pos.z;
    float entryRadius=LandmarkPolicy.EnterRadius(location.m_location.m_exteriorRadius);
    if(LandmarkPolicy.Entered(hasInterior,interior,sameInterior,dx*dx+dz*dz,entryRadius,player.y-pos.y)){
     entered.Add(record.Key);if(!insideLocations.Contains(record.Key))SetVisited(record);
    }
   }
   // Loaded modded interiors not registered under a known location name.
   if(current&&current.m_hasInterior){
    var record=ObserveLandmark(Clean(current.gameObject.name),current.transform.position);
    entered.Add(record.Key);if(!insideLocations.Contains(record.Key))SetVisited(record);
   }
   insideLocations.Clear();foreach(string key in entered)insideLocations.Add(key);
  }
  void EnterDungeon(Teleport entrance,Humanoid user,bool succeeded){
   if(!ready||!enabledSetting.Value||!dungeons.Value||!markVisited.Value||session==null||loadFailed||!succeeded||user!=Player.m_localPlayer||!entrance||!entrance.m_targetPoint)return;
   if(Character.InInterior(entrance.transform.position)||!Character.InInterior(entrance.m_targetPoint.transform.position))return;
   try{
    var location=Location.GetLocation(entrance.transform.position,true);
    if(location&&location.m_hasInterior){SetVisited(ObserveLandmark(Clean(location.gameObject.name),location.transform.position));Save();}
   }catch(Exception e){Warn("dungeon entry",e);}
  }
  [HarmonyPatch(typeof(Teleport),"Interact")]
  static class DungeonEntryPatch {static void Postfix(Teleport __instance,Humanoid __0,bool __result){Instance?.EnterDungeon(__instance,__0,__result);}}
  bool CategoryOn(string kind)=>kind=="Portal"?portals.Value:(kind=="Dungeon"||kind=="Landmark")&&dungeons.Value;
  void Render(Vector3 player){
   if(!map)return;
   var groups=ResourceGrouping.Build(records.Values.Where(r=>CategoryOn(r.Kind)&&Policy.Show(r.Kind,r.Available,r.Deleted,r.Y>3000,player.y>3000)));
   foreach(var key in pins.Keys.ToArray())if(!groups.ContainsKey(key))RemovePin(key);
   var gamePins=GamePins.GetValue(map) as List<Minimap.PinData>;
   foreach(var group in groups){
    var list=group.Value;var first=list[0];var pos=new Vector3(list.Average(r=>r.X),list.Average(r=>r.Y),list.Average(r=>r.Z));
    string label=first.Name;
    if(pins.TryGetValue(group.Key,out var stale)&&stale.Borrowed&&gamePins!=null&&!gamePins.Contains(stale.Data))RemovePin(group.Key);
    if(!pins.TryGetValue(group.Key,out var pin)){
     // Keep manual and pre-existing dungeon pins; do not create an overlapping second entrance marker.
     var native=LocationPins?.GetValue(map) as Dictionary<Vector3,Minimap.PinData>;
     var borrowed=first.Kind!="Portal"&&native!=null?native.Values.FirstOrDefault(p=>p.m_type!=Minimap.PinType.Boss&&(p.m_pos-pos).sqrMagnitude<100):null;
     // Keep saved pins intact but synchronize matching location checkmarks.
     if(first.Kind!="Portal"&&borrowed==null&&gamePins!=null)
      borrowed=gamePins.FirstOrDefault(p=>p.m_save&&p.m_type!=Minimap.PinType.Boss&&p.m_type!=Minimap.PinType.Icon4&&
       (p.m_pos-pos).sqrMagnitude<225&&LandmarkPolicy.MatchingSavedPin(p.m_name,label,first.Prefab));
     if(first.Kind!="Portal"&&borrowed==null&&gamePins!=null&&gamePins.Any(p=>p.m_save&&(p.m_pos-pos).sqrMagnitude<225))continue;
     // Migrate exact named portal matches from the former portal tracker after a map backup.
     // Other manual pins and dungeon pins remain untouched.
     var existing=first.Kind=="Portal" && gamePins!=null?gamePins.FirstOrDefault(p=>p.m_save&&p.m_type==Minimap.PinType.Icon4&&(p.m_pos-pos).sqrMagnitude<2*2&&p.m_name==label):null;
     var type=first.Kind=="Portal"?Minimap.PinType.Icon4:Minimap.PinType.Icon3;
     if(existing!=null){
      if(!BackupMap())continue;
      Save();if(dirty)continue;
      existing.m_save=false;
     }
     pin=new Pin{Borrowed=borrowed!=null,Data=borrowed??existing??map.AddPin(pos,type,label,false,first.Visited),Keys=list.Select(r=>r.Key).ToList(),Kind=first.Kind};
     pins.Add(group.Key,pin);
     if(borrowed!=null){if(borrowed.m_checked&&!first.Visited){first.Visited=true;dirty=true;}pin.Data.m_checked=first.Visited;}
    }else{
     pin.Keys=list.Select(r=>r.Key).ToList();
     if(!pin.Borrowed&&pin.Data.m_name!=label){RemovePin(group.Key);continue;}
     if(!pin.Borrowed)pin.Data.m_pos=pos;
     pin.Data.m_checked=first.Visited;
    }
   }
  }
  bool BackupMap(){
   try{
    string backup=path+".native-map-before-portals.bin";
    if(File.Exists(backup))return true;
    if(MapData==null)return false;
    Directory.CreateDirectory(Path.GetDirectoryName(backup));
    File.WriteAllBytes(backup,(byte[])MapData.Invoke(map,null));return true;
   }catch(Exception e){Warn("portal migration backup",e);return false;}
  }
  void RemovePin(string key){
   if(!pins.TryGetValue(key,out var pin))return;
   if(pin.Borrowed){
    if(pin.Data.m_iconElement){var c=pin.Data.m_iconElement.color;c.a=1;pin.Data.m_iconElement.color=c;}
    var text=pin.Data.m_NamePinData?.PinNameText;if(text){var c=text.color;c.a=1;text.color=c;}
   }
   internalRemove=true;try{if(map&&!pin.Borrowed)map.RemovePin(pin.Data);}finally{internalRemove=false;pins.Remove(key);}
  }
  void ClearPins(){foreach(var key in pins.Keys.ToArray())RemovePin(key);}
  void Style(){
   if(!enabledSetting.Value){RestoreNativeSizes();return;}
   StyleLandmarks();
   foreach(var pin in pins.Values){
    float scale=pin.Kind=="Portal"?portalScale.Value:landmarkScale.Value/(pin.Data.m_doubleSize?2f:1f);
    if(pin.Data.m_uiElement)pin.Data.m_uiElement.localScale=new Vector3(scale,scale,1);
    float alpha=LandmarkPolicy.Alpha(pin.Data.m_checked&&pin.Kind!="Portal",visitedOpacity.Value);
    if(pin.Data.m_iconElement){var c=pin.Data.m_iconElement.color;c.a=alpha;pin.Data.m_iconElement.color=c;}
    var text=pin.Data.m_NamePinData?.PinNameText;
    if(text){text.transform.localScale=Vector3.one;var c=text.color;c.a=alpha;text.color=c;}
    foreach(string key in pin.Keys)if(records.TryGetValue(key,out var r)&&r.Visited!=pin.Data.m_checked){r.Visited=pin.Data.m_checked;dirty=true;}
   }
  }
  void StyleLandmarks(){
   if(!map)return;
   if(Time.unscaledTime>=nextLandmarkCheck){
    nextLandmarkCheck=Time.unscaledTime+2;vendorPins.Clear();
    var known=new Dictionary<Vector3,string>();
    if(ZoneSystem.instance)ZoneSystem.instance.GetLocationIcons(known);
    var locations=LocationPins?.GetValue(map) as Dictionary<Vector3,Minimap.PinData>;
    if(locations!=null)foreach(var pair in locations){
     if(!known.TryGetValue(pair.Key,out var name))continue;
     string n=(name??"").ToLowerInvariant();
     // Native POI/location artwork uses the same final size as boss/vendor pins.
     if(!string.IsNullOrEmpty(n))vendorPins.Add(pair.Value);
    }
    // MWL's port selection map also owns temporary pins outside m_locationPins.
    var portType=AccessTools.TypeByName("More_World_Locations_AIO.PortMap");
    var portEntries=portType==null?null:AccessTools.Field(portType,"s_entries")?.GetValue(null) as System.Collections.IEnumerable;
    if(portEntries!=null)foreach(var entry in portEntries){
     if(entry==null)continue;
     var pin=AccessTools.Field(entry.GetType(),"Pin")?.GetValue(entry) as Minimap.PinData;
     if(pin!=null)vendorPins.Add(pin);
    }
    foreach(var dead in nativeScales.Keys.Where(t=>!t).ToArray())nativeScales.Remove(dead);
   }
   var all=GamePins.GetValue(map) as List<Minimap.PinData>;
   if(all==null)return;
   foreach(var pin in all){
    if(pin.m_type!=Minimap.PinType.Boss&&!vendorPins.Contains(pin))continue;
    if(!pin.m_uiElement||pin.m_worldSize>0)continue;
    Transform transform=pin.m_uiElement;
    if(!nativeScales.ContainsKey(transform))nativeScales.Add(transform,transform.localScale);
    float scale=landmarkScale.Value/(pin.m_doubleSize?2f:1f);
    transform.localScale=new Vector3(scale,scale,1);
   }
  }
  void RestoreNativeSizes(){foreach(var pair in nativeScales)if(pair.Key)pair.Key.localScale=pair.Value;nativeScales.Clear();}
  [HarmonyPatch(typeof(Minimap),"UpdatePins")]
  static class StylePatch {static void Postfix(){if(Instance!=null&&Instance.ready)Instance.Style();}}
  [HarmonyPatch(typeof(Minimap),"RemovePin",typeof(Minimap.PinData))]
  static class RemovePatch {static void Prefix(Minimap.PinData __0){
   var self=Instance;if(self==null||self.internalRemove)return;
   var pin=self.pins.Values.FirstOrDefault(p=>ReferenceEquals(p.Data,__0));
   if(pin==null||pin.Borrowed)return;
   foreach(string key in pin.Keys)if(self.records.TryGetValue(key,out var r)){r.Deleted=true;self.dirty=true;}
  }}
  [HarmonyPatch(typeof(Minimap),"SaveMapData")]
  static class SavePatch {static void Prefix(){Instance?.Save();}}
  void OnDestroy(){EndSession();harmony?.UnpatchSelf();if(Instance==this)Instance=null;}
 }
}
