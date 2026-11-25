using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace OctoberJemini.ArmorInscriptions {
 internal static class ApplyMenu {
  internal sealed class Entry { internal Recipe Recipe; internal ItemDrop.ItemData Armor; internal string Kind; }
  private static readonly Dictionary<Recipe,Entry> Entries=new Dictionary<Recipe,Entry>();
  private static readonly FieldInfo Selected=AccessTools.Field(typeof(InventoryGui),"m_selectedRecipe");
  private static readonly FieldInfo Available=AccessTools.Field(typeof(InventoryGui),"m_availableRecipes");
  private static readonly MethodInfo Changed=AccessTools.Method(typeof(Inventory),"Changed",new[]{typeof(bool),typeof(bool)});
  private static Entry pending;
  private static bool refresh;
  private static bool customized;
  internal static int Tier(ItemDrop.ItemData item){return item!=null&&item.m_dropPrefab?ArmorTiers.Cost(item.m_dropPrefab.name):0;}
  internal static int Required(ItemDrop.ItemData item,string kind){return Rules.PatchCost(kind,Tier(item));}
  private static Bench Current(Player p){var station=p?p.GetCurrentCraftingStation():null;return station?station.GetComponent<Bench>():null;}
  private static bool Here(){return Current(Player.m_localPlayer);}
  private static Entry Find(Recipe recipe){Entry e;return recipe&&Entries.TryGetValue(recipe,out e)?e:null;}
  private static Recipe RecipeOf(object pair){return (Recipe)pair.GetType().GetProperty("Recipe").GetValue(pair,null);}
  private static int Count(Player p,string kind){return p.GetInventory().GetAllItems().Where(i=>Plugin.PatchId(i)==kind).Sum(i=>i.m_stack);}
  private static bool Valid(Player p,Entry e){var bench=Current(p);return e!=null&&bench&&bench.CanUse(p)&&p.GetInventory().ContainsItem(e.Armor)&&e.Armor.m_stack==1&&Plugin.Compatible(e.Armor,e.Kind)&&Required(e.Armor,e.Kind)>0&&TailoringExtensions.Missing(bench,Tier(e.Armor))==null&&!Rules.Has(e.Armor.m_customData,e.Kind);}
  internal static void Open(Player p,Bench bench,string kind=null){
   if(!bench.CanUse(p)||p.InAttack()||p.InDodge())return;
   p.SetCraftingStation(bench.GetComponent<CraftingStation>());
   InventoryGui.instance.Show(null,3);
  }
  internal static void Close(){pending=null;foreach(var r in Entries.Keys)UnityEngine.Object.Destroy(r);Entries.Clear();refresh=false;}
  internal static void Tick(){
   var p=Player.m_localPlayer;var gui=InventoryGui.instance;
   if(!p||!gui)return;
   var bench=Current(p);
   if(bench&&InventoryGui.IsVisible()&&!bench.CanUse(p)){gui.Hide();return;}
   if(refresh&&InventoryGui.IsVisible()){refresh=false;AccessTools.Method(typeof(InventoryGui),"UpdateCraftingPanel").Invoke(gui,new object[]{false});}
  }
  private static bool Consume(Inventory inventory,string kind,int amount){
   var stacks=inventory.GetAllItems().Where(i=>Plugin.PatchId(i)==kind).ToArray();
   var plan=ArmorTiers.Plan(stacks.Select(i=>i.m_stack).ToArray(),amount);if(plan==null)return false;
   var all=inventory.GetAllItems();for(int i=0;i<stacks.Length;i++)if(plan[i]>0){stacks[i].m_stack-=plan[i];if(stacks[i].m_stack==0)all.Remove(stacks[i]);}return true;
  }
  private static bool Apply(Player p,Entry e){
   var bench=Current(p);if(!bench||!bench.CanUse(p)||!Valid(p,e))return false;
   int cost=Required(e.Armor,e.Kind);if(Count(p,e.Kind)<cost)return false;
   if(e.Armor.m_customData==null)e.Armor.m_customData=new Dictionary<string,string>();
   if(!Rules.TryApply(e.Armor.m_customData,e.Kind,true,true,()=>Consume(p.GetInventory(),e.Kind,cost)))return false;
   Changed.Invoke(p.GetInventory(),new object[]{false,false});
   p.Message(MessageHud.MessageType.Center,Plugin.Title(e.Kind)+" applied");
   return true;
  }
  [HarmonyPatch(typeof(Player),"GetAvailableRecipes")]
  private static class Recipes {
   private static void Postfix(Player __instance,ref List<Recipe> __0){
    var bench=Current(__instance);if(!bench||__instance!=Player.m_localPlayer)return;
    // Only this station's local menu is replaced. No ObjectDB/global recipes change.
    __0.Clear();var station=bench.GetComponent<CraftingStation>();
    foreach(var armor in __instance.GetInventory().GetAllItems())foreach(string kind in Rules.Ids){
     if(!Plugin.Compatible(armor,kind)||armor.m_stack!=1||Required(armor,kind)==0||Rules.Has(armor.m_customData,kind))continue;
     var e=Entries.Values.FirstOrDefault(x=>ReferenceEquals(x.Armor,armor)&&x.Kind==kind);
     if(e==null){
      var r=ScriptableObject.CreateInstance<Recipe>();r.name="OCT_InscriptionRecipe_"+Entries.Count;r.m_item=armor.m_dropPrefab.GetComponent<ItemDrop>();r.m_amount=1;r.m_enabled=true;r.m_minStationLevel=1;r.m_qualityResultAmountMultiplier=0;
      e=new Entry{Recipe=r,Armor=armor,Kind=kind};Entries.Add(r,e);
     }
     e.Recipe.m_craftingStation=station;e.Recipe.m_resources=new[]{new Piece.Requirement{m_resItem=Plugin.Items[kind].ItemDrop,m_amount=Required(armor,kind),m_amountPerLevel=0,m_recover=false}};
     __0.Add(e.Recipe);
    }
   }
  }
  // Use the game's normal crafting list/timer while presenting it as an armor upgrade.
  // This avoids vanilla's max-quality filter and never increments armor quality.
  [HarmonyPatch(typeof(InventoryGui),"InCraftTab")]
  private static class CraftTab {private static bool Prefix(ref bool __result){if(!Here())return true;__result=true;return false;}}
  [HarmonyPatch(typeof(InventoryGui),"InUpradeTab")]
  private static class UpgradeTab {private static bool Prefix(ref bool __result){if(!Here())return true;__result=false;return false;}}
  private static void Badge(Image image,string kind){
   if(!image)return;var child=image.transform.Find("OCT_InscriptionBadge");
   if(kind==null){if(child)child.gameObject.SetActive(false);return;}
   if(!child){var go=new GameObject("OCT_InscriptionBadge",typeof(RectTransform),typeof(Image));go.transform.SetParent(image.transform,false);child=go.transform;
    var rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.54f,0);rect.anchorMax=new Vector2(1,.46f);rect.offsetMin=rect.offsetMax=Vector2.zero;
    go.GetComponent<Image>().raycastTarget=false;go.GetComponent<Image>().preserveAspect=true;
   }
   child.gameObject.SetActive(true);child.GetComponent<Image>().sprite=Plugin.Items[kind].ItemDrop.m_itemData.GetIcon();
  }
  private static string Name(Entry e){return Localization.instance.Localize(e.Armor.m_shared.m_name)+" — "+Plugin.Title(e.Kind);}
  [HarmonyPatch(typeof(InventoryGui),"UpdateRecipeList")]
  private static class Labels {
   private static void Postfix(InventoryGui __instance){if(!Here())return;
    foreach(var pair in (IEnumerable)Available.GetValue(__instance)){
     var e=Find(RecipeOf(pair));if(e==null)continue;var row=(GameObject)pair.GetType().GetProperty("InterfaceElement").GetValue(pair,null);
     var label=row.transform.Find("name");if(label)label.GetComponent<TMP_Text>().text=Name(e);
     var icon=row.transform.Find("icon");if(icon){var image=icon.GetComponent<Image>();image.sprite=e.Armor.GetIcon();Badge(image,e.Kind);}
    }
   }
  }
  [HarmonyPatch(typeof(InventoryGui),"UpdateRecipe")]
  private static class Preview {
   private static void Prefix(ref int ___m_multiCraftAmount,out int __state){__state=___m_multiCraftAmount;if(Here())___m_multiCraftAmount=1;}
   private static void Postfix(InventoryGui __instance,Player __0,TMP_Text ___m_recipeName,TMP_Text ___m_recipeDecription,Image ___m_recipeIcon,Button ___m_craftButton,Button ___m_tabCraft,Button ___m_tabUpgrade,ref int ___m_multiCraftAmount,int __state,float ___m_craftTimer){
    ___m_multiCraftAmount=__state;
    if(!Here()){
     Badge(___m_recipeIcon,null);
     if(customized){___m_tabCraft.GetComponentInChildren<TMP_Text>().text=Localization.instance.Localize("$inventory_craft");customized=false;}
     return;
    }
    customized=true;___m_tabCraft.gameObject.SetActive(true);___m_tabUpgrade.gameObject.SetActive(false);___m_tabCraft.interactable=false;___m_tabCraft.GetComponentInChildren<TMP_Text>().text="UPGRADE";
    var e=Find(RecipeOf(Selected.GetValue(__instance)));
    if(e==null){Badge(___m_recipeIcon,null);___m_recipeDecription.text="Carry a helmet, chest armor or leg armor with an available inscription.\n\nBuy patches from Hildir for 500 coins each after Eikthyr.";return;}
    ___m_recipeName.text=Name(e);___m_recipeIcon.sprite=e.Armor.GetIcon();Badge(___m_recipeIcon,e.Kind);
    ___m_recipeDecription.text=Plugin.Benefit(e.Kind)+"\n\nQuality: "+e.Armor.m_quality+(__0.IsItemEquiped(e.Armor)?" · Equipped":"")+"\n\nPatches: "+Count(__0,e.Kind)+" / "+Required(e.Armor,e.Kind)+"\n\nPermanent on this armor. Keeps quality, durability, enchantments, sockets and other inscriptions.";
    var missing=TailoringExtensions.Missing(Current(__0),Tier(e.Armor));
    if(missing!=null)___m_recipeDecription.text+="\n\n<color=orange>"+missing+" (within 5m).</color>";
    ___m_craftButton.GetComponentInChildren<TMP_Text>().text="Apply Inscription";
    ___m_craftButton.interactable=___m_craftTimer<0&&Valid(__0,e)&&Count(__0,e.Kind)>=Required(e.Armor,e.Kind);
   }
  }
  [HarmonyPatch(typeof(InventoryGui),"OnCraftPressed")]
  private static class Start {
   private static bool Prefix(InventoryGui __instance,out object __state){
    __state=null;var pair=Selected.GetValue(__instance);var e=Find(RecipeOf(pair));if(e==null)return !Here();
    var p=Player.m_localPlayer;if(!Valid(p,e)||Count(p,e.Kind)<Required(e.Armor,e.Kind))return false;
    pending=e;__state=Selected.GetValue(__instance);
    // Native upgrade start accepts a full inventory and prevents shift multi-craft.
    pair.GetType().GetProperty("ItemData").SetValue(pair,e.Armor,null);Selected.SetValue(__instance,pair);return true;
   }
   private static void Postfix(InventoryGui __instance,object __state,ref bool ___m_multiCrafting){if(__state!=null){Selected.SetValue(__instance,__state);___m_multiCrafting=false;}}
  }
  [HarmonyPatch(typeof(InventoryGui),"DoCrafting")]
  private static class Finish {
   [HarmonyPriority(Priority.First)]
   private static bool Prefix(Player __0,Recipe ___m_craftRecipe){
    var e=Find(___m_craftRecipe);if(e==null)return !Here();
    var captured=pending;pending=null;
    try{
     if(e!=captured||!Apply(__0,e)){__0.Message(MessageHud.MessageType.Center,"Armor or patches changed. Nothing was applied.");return false;}
     var station=__0.GetCurrentCraftingStation();station.m_craftItemDoneEffects.Create(__0.transform.position,Quaternion.identity);
    }catch(Exception error){Plugin.Instance.Error(error);}
    finally{refresh=true;}
    return false;
   }
  }
  [HarmonyPatch(typeof(InventoryGui),"OnCraftCancelPressed")]
  private static class Cancel {private static void Postfix(){pending=null;}}
  [HarmonyPatch(typeof(InventoryGui),"Hide")]
  private static class Hidden {private static void Postfix(){Close();}}
 }
}
