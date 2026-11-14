using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace OctoberJemini.QuietMap {
 internal static class LandmarkPolicy {
  internal sealed class Entry {internal string Name,Category;internal bool Service=>Category=="Ports"||Category=="Traders"||Category=="Trainers";}
  internal static readonly Dictionary<string,Entry> Aio=Load();
  static Dictionary<string,Entry> Load(){
   var result=new Dictionary<string,Entry>(StringComparer.OrdinalIgnoreCase);
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("QuietMap.AioLandmarks")){
    if(stream==null)throw new InvalidDataException("AIO landmark catalog missing");
    using(var reader=new StreamReader(stream)){string line;while((line=reader.ReadLine())!=null){var parts=line.Split('\t');if(parts.Length==3)result.Add(parts[1],new Entry{Category=parts[0],Name=parts[2]});}}
   }
   return result;
  }
  internal static string OutdoorName(string prefab){
   string n=(prefab??"").ToLowerInvariant();
   if(n.StartsWith("tarpit"))return "Tar Pit";
   if(n.StartsWith("goblincamp"))return "Fuling Village";
   if(n.StartsWith("woodvillage")||n.StartsWith("draugrvillage"))return "Draugr Village";
   if(n.StartsWith("greydwarf_camp"))return "Greydwarf Nest";
   if(n.StartsWith("firehole"))return "Surtling Geyser";
   if(n.StartsWith("mistlands_dvergrtown")&&!n.Contains("entrance"))return "Dvergr Settlement";
   if(n.StartsWith("mistlands_guardtower")||n.StartsWith("mistlands_dvergrguardtower"))return "Dvergr Tower";
   if(n.StartsWith("mistlands_harbour")||n.StartsWith("mistlands_dvergrharbour"))return "Dvergr Harbour";
   if(n.StartsWith("mistlands_viaduct"))return "Ruined Aqueduct";
   if(n.StartsWith("shipwreck"))return "Shipwreck";
   return VanillaName(prefab);
  }
  static string VanillaName(string name){
   if(string.IsNullOrEmpty(name))return null;
   if(name.StartsWith("Mistlands_GuardTower1_ruined_new",StringComparison.OrdinalIgnoreCase))return "Guard tower 1 ruined new";
   if(name.StartsWith("Mistlands_GuardTower3_ruined_new",StringComparison.OrdinalIgnoreCase))return "Guard tower 3 ruined new";
   if(name.StartsWith("Mistlands_DvergrTownEntrance",StringComparison.OrdinalIgnoreCase))return "Infested Mine";
   if(name.StartsWith("Mistlands_GuardTower1_new",StringComparison.OrdinalIgnoreCase))return "Guard tower 1 new";
   if(name.StartsWith("Mistlands_GuardTower2_new",StringComparison.OrdinalIgnoreCase))return "Guard tower 2 new";
   if(name.StartsWith("Mistlands_GuardTower3_new",StringComparison.OrdinalIgnoreCase))return "Guard tower 3 new";
   if(name.StartsWith("Mistlands_Lighthouse1_new",StringComparison.OrdinalIgnoreCase))return "Lighthouse 1 new";
   if(name.StartsWith("CharredTowerRuins1_dvergr",StringComparison.OrdinalIgnoreCase))return "Charred tower ruins 1 dvergr";
   if(name.StartsWith("StoneTowerRuins07_sunk",StringComparison.OrdinalIgnoreCase))return "Stone tower ruins 07 sunk";
   if(name.StartsWith("StoneTowerRuins08_sunk",StringComparison.OrdinalIgnoreCase))return "Stone tower ruins 08 sunk";
   if(name.StartsWith("StoneTowerRuins09_sunk",StringComparison.OrdinalIgnoreCase))return "Stone tower ruins 09 sunk";
   if(name.StartsWith("StoneTowerRuins10_sunk",StringComparison.OrdinalIgnoreCase))return "Stone tower ruins 10 sunk";
   if(name.StartsWith("StoneTowerRuins05_leet",StringComparison.OrdinalIgnoreCase))return "Stone tower ruins 05 leet";
   if(name.StartsWith("AncientUpgradeStation",StringComparison.OrdinalIgnoreCase))return "Ancient upgrade station";
   if(name.StartsWith("Mistlands_StatueGroup",StringComparison.OrdinalIgnoreCase))return "Statue group";
   if(name.StartsWith("Hildir_plainsfortress",StringComparison.OrdinalIgnoreCase))return "Sealed Tower";
   if(name.StartsWith("Mistlands_Excavation",StringComparison.OrdinalIgnoreCase))return "Dvergr excavation";
   if(name.StartsWith("Mistlands_RoadPost",StringComparison.OrdinalIgnoreCase))return "Road post";
   if(name.StartsWith("NorthMemorialPlace",StringComparison.OrdinalIgnoreCase))return "North memorial place";
   if(name.StartsWith("StoneHouse1_heath",StringComparison.OrdinalIgnoreCase))return "Stone house 1 heath";
   if(name.StartsWith("StoneHouse2_heath",StringComparison.OrdinalIgnoreCase))return "Stone house 2 heath";
   if(name.StartsWith("StoneHouse5_heath",StringComparison.OrdinalIgnoreCase))return "Stone house 5 heath";
   if(name.StartsWith("AbandonedLogCabin",StringComparison.OrdinalIgnoreCase))return "Abandoned log cabin";
   if(name.StartsWith("Mistlands_Harbour",StringComparison.OrdinalIgnoreCase))return "Dvergr harbour";
   if(name.StartsWith("Mistlands_Viaduct",StringComparison.OrdinalIgnoreCase))return "Viaduct";
   if(name.StartsWith("CharredTowerRuins",StringComparison.OrdinalIgnoreCase))return "Charred tower ruins";
   if(name.StartsWith("Mistlands_Statue",StringComparison.OrdinalIgnoreCase))return "Statue";
   if(name.StartsWith("StoneTowerRuins",StringComparison.OrdinalIgnoreCase))return "Stone tower ruins";
   if(name.StartsWith("CharredFortress",StringComparison.OrdinalIgnoreCase))return "Charred Fortress";
   if(name.StartsWith("FrozenShip01_DN",StringComparison.OrdinalIgnoreCase))return "Frozen ship 01 dn";
   if(name.StartsWith("FrozenShip02_DN",StringComparison.OrdinalIgnoreCase))return "Frozen ship 02 dn";
   if(name.StartsWith("FrozenShip03_DN",StringComparison.OrdinalIgnoreCase))return "Frozen ship 03 dn";
   if(name.StartsWith("Greydwarf_camp",StringComparison.OrdinalIgnoreCase))return "Greydwarf camp";
   if(name.StartsWith("DrakeLorestone",StringComparison.OrdinalIgnoreCase))return "Drake lorestone";
   if(name.StartsWith("PlaceofMystery",StringComparison.OrdinalIgnoreCase))return "Place of Mystery";
   if(name.StartsWith("ShipWreck01_DN",StringComparison.OrdinalIgnoreCase))return "Ship wreck 01 dn";
   if(name.StartsWith("ShipWreck02_DN",StringComparison.OrdinalIgnoreCase))return "Ship wreck 02 dn";
   if(name.StartsWith("MountainGrave",StringComparison.OrdinalIgnoreCase))return "Mountain grave";
   if(name.StartsWith("FortressRuins",StringComparison.OrdinalIgnoreCase))return "Fortress ruins";
   if(name.StartsWith("MountainWell",StringComparison.OrdinalIgnoreCase))return "Mountain well";
   if(name.StartsWith("MountainCave",StringComparison.OrdinalIgnoreCase))return "Frost Cave";
   if(name.StartsWith("Hildir_crypt",StringComparison.OrdinalIgnoreCase))return "Smouldering Tomb";
   if(name.StartsWith("CharredRuins",StringComparison.OrdinalIgnoreCase))return "Charred ruins";
   if(name.StartsWith("AshlandRuins",StringComparison.OrdinalIgnoreCase))return "Ashland ruins";
   if(name.StartsWith("NorthVillage",StringComparison.OrdinalIgnoreCase))return "North village";
   if(name.StartsWith("StoneCircle",StringComparison.OrdinalIgnoreCase))return "Stone circle";
   if(name.StartsWith("WoodVillage",StringComparison.OrdinalIgnoreCase))return "Draugr village";
   if(name.StartsWith("SunkenCrypt",StringComparison.OrdinalIgnoreCase))return "Crypt";
   if(name.StartsWith("ShipSetting",StringComparison.OrdinalIgnoreCase))return "Ship setting";
   if(name.StartsWith("Hildir_cave",StringComparison.OrdinalIgnoreCase))return "Howling Cavern";
   if(name.StartsWith("StoneHouse",StringComparison.OrdinalIgnoreCase))return "Stone house";
   if(name.StartsWith("GoblinCamp",StringComparison.OrdinalIgnoreCase))return "Fuling village";
   if(name.StartsWith("StoneTower",StringComparison.OrdinalIgnoreCase))return "Stone tower";
   if(name.StartsWith("StoneHenge",StringComparison.OrdinalIgnoreCase))return "Stone henge";
   if(name.StartsWith("CombatRuin",StringComparison.OrdinalIgnoreCase))return "Combat ruin";
   if(name.StartsWith("MorgenHole",StringComparison.OrdinalIgnoreCase))return "Putrid Hole";
   if(name.StartsWith("LumberCamp",StringComparison.OrdinalIgnoreCase))return "Lumber camp";
   if(name.StartsWith("Runestone",StringComparison.OrdinalIgnoreCase))return "Runestone";
   if(name.StartsWith("SwampRuin",StringComparison.OrdinalIgnoreCase))return "Swamp ruin";
   if(name.StartsWith("WoodHouse",StringComparison.OrdinalIgnoreCase))return "Wood house";
   if(name.StartsWith("TrollCave",StringComparison.OrdinalIgnoreCase))return "Troll Cave";
   if(name.StartsWith("SwampWell",StringComparison.OrdinalIgnoreCase))return "Swamp well";
   if(name.StartsWith("Waymarker",StringComparison.OrdinalIgnoreCase))return "Waymarker";
   if(name.StartsWith("ShipWreck",StringComparison.OrdinalIgnoreCase))return "Ship wreck";
   if(name.StartsWith("GoblinHut",StringComparison.OrdinalIgnoreCase))return "Goblin hut";
   if(name.StartsWith("FireHole",StringComparison.OrdinalIgnoreCase))return "Fire hole";
   if(name.StartsWith("WoodFarm",StringComparison.OrdinalIgnoreCase))return "Abandoned farm";
   if(name.StartsWith("SwampHut",StringComparison.OrdinalIgnoreCase))return "Swamp hut";
   if(name.StartsWith("MorkBorg",StringComparison.OrdinalIgnoreCase))return "Morkhalla";
   if(name.StartsWith("BearCave",StringComparison.OrdinalIgnoreCase))return "Bear cave";
   if(name.StartsWith("TheHole",StringComparison.OrdinalIgnoreCase))return "The hole";
   if(name.StartsWith("Dolmen",StringComparison.OrdinalIgnoreCase))return "Dolmen";
   if(name.StartsWith("TarPit",StringComparison.OrdinalIgnoreCase))return "Tar pit";
   if(name.StartsWith("DN_hut",StringComparison.OrdinalIgnoreCase))return "Dn hut";
   if(name.StartsWith("Grave",StringComparison.OrdinalIgnoreCase))return "Grave";
   if(name.StartsWith("Crypt",StringComparison.OrdinalIgnoreCase))return "Burial Chambers";
   if(name.StartsWith("Ruin",StringComparison.OrdinalIgnoreCase))return "Ruin";
   return null;
  }
  internal static bool IsService(string prefab)=>Aio.TryGetValue(prefab??"",out var item)&&item.Service;
  internal static float EnterRadius(float actualRadius)=>Math.Max(5,Math.Min(18,actualRadius*.5f));
  internal static bool Entered(bool interiorLocation,bool inInterior,bool sameInterior,float horizontalDistanceSquared,float radius,float heightDifference)=>
   interiorLocation?inInterior&&sameInterior:!inInterior&&Math.Abs(heightDifference)<=20&&horizontalDistanceSquared<=radius*radius;
  internal static bool KeepRecord(string kind)=>kind=="Portal"||kind=="Dungeon"||kind=="Landmark";
  internal static float Alpha(bool crossed,float faded)=>crossed?faded:1f;
  internal static bool CanReuseSavedLocationPin(bool saved,bool boss,bool portal,float distanceSquared,string label,string expected,string prefab)=>
   saved&&!boss&&!portal&&distanceSquared<225&&MatchingSavedPin(label,expected,prefab);
  internal static bool MatchingSavedPin(string label,string expected,string prefab){
   if(string.IsNullOrWhiteSpace(label))return false;
   string name=label.Trim();
   if(string.Equals(name,expected,StringComparison.OrdinalIgnoreCase)||string.Equals(name,prefab,StringComparison.OrdinalIgnoreCase))return true;
   return (prefab??"").StartsWith("Crypt",StringComparison.OrdinalIgnoreCase)&&
    (string.Equals(name,"Burial Chamber",StringComparison.OrdinalIgnoreCase)||string.Equals(name,"Burial Chambers",StringComparison.OrdinalIgnoreCase));
  }
 }
}
