using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
namespace OctoberJemini.ArmorInscriptions {
 internal static class TailoringExtensions {
  internal const string Rack="OCT_InscriptionLeatherRack",Tools="OCT_InscriptionStitchingTools";
  internal static void Register(){
   RegisterOne(Rack,"Leather Rack","Unlocks Plains and Mistlands armor inscriptions. Place within 5m of an Inscription Bench.","rack",new[]{Req("FineWood",10),Req("DeerHide",6),Req("LoxPelt",2),Req("LinenThread",6)},false);
   RegisterOne(Tools,"Precision Stitching Tools","With a Leather Rack, unlocks Ashlands and Deep North armor inscriptions. Place within 5m of an Inscription Bench.","stitching",new[]{Req("FineWood",8),Req("LinenThread",10),Req("AskHide",4),Req("FlametalNew",2)},true);
  }
  private static RequirementConfig Req(string item,int amount){return new RequirementConfig{Item=item,Amount=amount,Recover=true};}
  private static void RegisterOne(string prefab,string title,string description,string icon,RequirementConfig[] costs,bool tools){
   if(PrefabManager.Instance.GetPrefab(prefab))return;
   var clone=PrefabManager.Instance.CreateClonedPrefab(prefab,PrefabManager.Instance.GetPrefab("piece_beehive"));
   foreach(var c in clone.GetComponents<Beehive>())UnityEngine.Object.DestroyImmediate(c);
   for(int i=clone.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(clone.transform.GetChild(i).gameObject);
   foreach(var c in clone.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
   var wear=clone.GetComponent<WearNTear>();wear.m_new=BenchVisual.BuildExtension(clone,tools);
   wear.m_worn=UnityEngine.Object.Instantiate(wear.m_new,clone.transform);wear.m_broken=UnityEngine.Object.Instantiate(wear.m_new,clone.transform);
   wear.m_new.SetActive(true);wear.m_worn.SetActive(false);wear.m_broken.SetActive(false);wear.m_fragmentRoots=new[]{wear.m_new};wear.m_health=200;wear.m_supports=false;
   var box=clone.AddComponent<BoxCollider>();box.center=tools?new Vector3(0,.64f,0):new Vector3(0,.85f,0);box.size=tools?new Vector3(.86f,1.28f,.65f):new Vector3(1.45f,1.70f,.45f);
   var extension=clone.AddComponent<StationExtension>();extension.m_craftingStation=PrefabManager.Instance.GetPrefab(Bench.Prefab).GetComponent<CraftingStation>();extension.m_maxStationDistance=5;extension.m_stack=false;
   extension.m_connectionPrefab=PrefabManager.Instance.GetPrefab("piece_workbench_ext1").GetComponent<StationExtension>().m_connectionPrefab;extension.m_connectionOffset=new Vector3(0,1,0);
   var config=new PieceConfig{Name=title,Description=description,PieceTable="Hammer",Category="Crafting",CraftingStation="piece_workbench",Icon=Plugin.LoadIcon(icon),Requirements=costs};
   if(!PieceManager.Instance.AddPiece(new CustomPiece(clone,false,config)))throw new InvalidOperationException("Extension registration rejected: "+prefab);
  }
  internal static string Missing(Bench bench,int tier){
   if(!bench||tier<1||tier>7)return "Unknown armor tier";
   if(tier<=3)return null;
   var found=new List<StationExtension>();StationExtension.FindExtensions(bench.GetComponent<CraftingStation>(),bench.transform.position,found);
   bool rack=found.Any(e=>e&&e.name.Replace("(Clone)","")==Rack),tools=found.Any(e=>e&&e.name.Replace("(Clone)","")==Tools);
   if(ExtensionRules.Allows(tier,rack,tools))return null;
   return !rack?(tier>=6&&!tools?"Requires Leather Rack and Precision Stitching Tools":"Requires Leather Rack"):"Requires Precision Stitching Tools";
  }
 }
}
