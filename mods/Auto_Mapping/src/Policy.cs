using System;
using System.Collections.Generic;
namespace OctoberJemini.QuietMap {
 internal static class Policy {
  static readonly HashSet<string> Valuable=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"SurtlingCore","BlackCore","MoltenCore","FrostCore","RoyalJelly","GoblinTotem","DragonEgg","VoltureEgg","Tar","SulfurStone","WolfHairBundle","Crystal","IronOre","TinOre","CopperOre","SilverOre","Obsidian","DyrnwynHiltFragment","DyrnwynBladeFragment","DyrnwynTipFragment","GemstoneBlue","GemstoneGreen","GemstoneRed","AncientGemstoneBlack","AncientGemstoneGreen","AncientGemstoneOrange","AncientGemstonePurple"};
  static readonly HashSet<string> Minerals=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"CopperOre","TinOre","SilverOre","IronScrap","IronOre","Iron","CopperScrap","Obsidian","FlametalOre","FlametalOreNew","BlackMarble","Softtissue","Guck","Chitin","ProustitePowder","BellFragment","GoldOre","GoldNugget"};
  internal static bool IsValuable(string name)=>name!=null && (Valuable.Contains(name)||string.Equals(name,"DvergrNeedle",StringComparison.OrdinalIgnoreCase));
  internal static bool IsMineral(string name)=>name!=null && Minerals.Contains(name);
  internal static bool IsCompletionLoot(string name)=>IsValuable(name)||IsMineral(name)||name=="Coins"||name=="Amber"||name=="AmberPearl"||name=="Ruby"||name=="SilverNecklace";
  internal static bool IsGeneratedPoi(string name)=>!string.IsNullOrEmpty(name)&&(name.StartsWith("MWL_",StringComparison.OrdinalIgnoreCase)||name.Equals("BFD_Exterior",StringComparison.OrdinalIgnoreCase));
  internal static bool HideResourceInPoi(string kind,bool inside)=>inside&&(kind=="Loot"||kind=="Ore");
  internal static bool DepositPrefab(string name){
   string n=(name??"").ToLowerInvariant();
   return n.StartsWith("mudpile")||n.StartsWith("mineable")||n.StartsWith("rock")||n.StartsWith("guck")||n.StartsWith("giant_brain")||n.StartsWith("giant_helmet")||n.StartsWith("giant_sword");
  }
  internal static bool LootCrate(string name)=>string.Equals(name,"piece_dvergr_crate",StringComparison.OrdinalIgnoreCase)||string.Equals(name,"dvergrprops_crate",StringComparison.OrdinalIgnoreCase);
  internal static bool RestorePortal(Record r,bool interacted)=>r.Kind=="Portal"&&r.Deleted&&(interacted||r.Prefab=="portal");
  internal static bool IsTar(Record r)=>r.Kind=="Loot" && (string.Equals(r.Name,"Tar",StringComparison.OrdinalIgnoreCase)||string.Equals(r.Prefab,"Pickable_Tar",StringComparison.OrdinalIgnoreCase)||string.Equals(r.Prefab,"Pickable_TarBig",StringComparison.OrdinalIgnoreCase)||string.Equals(r.Prefab,"Tar",StringComparison.OrdinalIgnoreCase));
  internal static bool Small(string kind)=>kind=="Ore" || kind=="Chest" || kind=="Loot";
  internal static bool Show(string kind,bool available,bool deleted,bool interior,bool playerInterior)=>available&&!deleted&&(!interior||playerInterior||kind=="Portal");
  internal static bool MissingMeansDestroyed(bool inRange,bool zoneLoaded,bool hasZdo,int misses)=>inRange&&zoneLoaded&&!hasZdo&&misses>=3;
  internal static bool IsDungeonName(string name){
   if(string.IsNullOrEmpty(name))return false;
   string n=name.ToLowerInvariant();
   return n.Contains("crypt")||n.Contains("trollcave")||n.Contains("mountaincave")||n.Contains("dvergrtownentrance")||n.Contains("morgenhole")||n.Contains("charredfortress")||n.Contains("morkhalla")||n.StartsWith("morkborg")||n.Contains("hildir_cave")||n.Contains("hildir_plainsfortress")||n.Contains("tomb")||n.Contains("dungeon")||n.Contains("bearcave")||n.StartsWith("thehole")||n.StartsWith("the_hole")||n.StartsWith("thedarkesthole");
  }
 }
 public sealed class Record {
  public string Key,Kind,Name,Prefab;
  public long User;
  public uint Id;
  public float X,Y,Z;
  public bool Available=true,Deleted,Visited;
  public bool DungeonSurveyed;
  public string CompletionReason;
 }
 public sealed class SaveData {
  public int Version=1;
  public List<Record> Records=new List<Record>();
 }
}

