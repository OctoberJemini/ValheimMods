using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace OctoberJemini.ArmorInscriptions {
 internal static class Effects {
  private static readonly FieldInfo Chest=AccessTools.Field(typeof(Humanoid),"m_chestItem");
  private static readonly FieldInfo Legs=AccessTools.Field(typeof(Humanoid),"m_legItem");
  private static readonly FieldInfo Helmet=AccessTools.Field(typeof(Humanoid),"m_helmetItem");
  private static readonly FieldInfo Right=AccessTools.Field(typeof(Humanoid),"m_rightItem");
  internal static bool Active(Character character,string id){
   var player=character as Player;
   if(!player||player!=Player.m_localPlayer||!Rules.IsKnown(id))return false;
   var item=(ItemDrop.ItemData)(Rules.IsHelmet(id)?Helmet:Rules.IsLegs(id)?Legs:Chest).GetValue(player);
   return Plugin.Compatible(item,id)&&(!item.m_shared.m_useDurability||item.m_durability>0)&&Rules.Has(item.m_customData,id);
  }
  private static bool ToolUse(Character character){
   var player=character as Player;if(!player||player!=Player.m_localPlayer)return false;
   var tool=(ItemDrop.ItemData)Right.GetValue(player);
   string id=tool!=null&&tool.m_dropPrefab?Rules.ToolInscription(tool.m_dropPrefab.name):null;
   return id!=null&&Active(character,id);
  }
  public static bool BuildHave(Character c,float n){return ToolUse(c)||c.HaveStamina(n);}
  public static void BuildUse(Character c,float n){if(!ToolUse(c))c.UseStamina(n);}
  public static bool RunHave(Character c,float n){return Active(c,"runner")||c.HaveStamina(n);}
  public static void RunUse(Character c,float n){if(!Active(c,"runner"))c.UseStamina(n);}
  public static bool JumpHave(Character c,float n){return Active(c,"leaper")||c.HaveStamina(n);}
  public static void JumpUse(Character c,float n){if(!Active(c,"leaper"))c.UseStamina(n);}
  public static bool SwimHave(Character c,float n){return Active(c,"snorkel")||c.HaveStamina(n);}
  public static void SwimUse(Character c,float n){if(!Active(c,"snorkel"))c.UseStamina(n);}
  public static void SwimSpeed(Character c,ref float speed){speed*=Rules.SwimMultiplier(Active(c,"tidewalker"));}
  public static bool HoverPlaceMode(Character c){
   var p=c as Player;
   var item=p&&p==Player.m_localPlayer?(ItemDrop.ItemData)Right.GetValue(p):null;
   if(item!=null&&item.m_dropPrefab&&Rules.ToolInscription(item.m_dropPrefab.name)!=null)return false;
   return c.InPlaceMode();
  }
  // Scope to exact action call sites. Combat, blocking, dodging,
  // grappling, and encumbrance continue to call the unmodified stamina APIs.
  internal static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> input,string oldName,string newName,int expected,bool firstOnly){
   var code=new List<CodeInstruction>(input);
   var original=AccessTools.Method(typeof(Character),oldName,oldName=="InPlaceMode"?Type.EmptyTypes:new[]{typeof(float)});
   int count=0;foreach(var i in code)if(i.Calls(original))count++;
   if(count!=expected)throw new InvalidOperationException("Unsupported Valheim code: "+oldName+" expected "+expected+", got "+count);
   bool done=false;
   foreach(var i in code)if(i.Calls(original)&&(!firstOnly||!done)){i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(Effects),newName);done=true;}
   return code;
  }
  [HarmonyPatch(typeof(Player),"UpdatePlacement")]
  private static class Place{
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> c){return Replace(Replace(c,"HaveStamina","BuildHave",2,false),"UseStamina","BuildUse",2,false);}
  }
  [HarmonyPatch(typeof(Player),"Repair")]
  private static class Repair{
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> c){return Replace(c,"UseStamina","BuildUse",1,false);}
  }
  [HarmonyPatch(typeof(Player),"CheckRun")]
  private static class Run{
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> c){return Replace(Replace(c,"HaveStamina","RunHave",2,false),"UseStamina","RunUse",1,false);}
  }
  [HarmonyPatch(typeof(Character),"Jump")]
  private static class JumpCheck{
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> c){return Replace(c,"HaveStamina","JumpHave",2,true);}
  }
  [HarmonyPatch(typeof(Player),"OnJump")]
  private static class JumpCost{
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> c){return Replace(c,"UseStamina","JumpUse",1,false);}
  }
  [HarmonyPatch(typeof(Player),"OnSwimming")]
  private static class SwimCost {
   // Preserve skill gains, base swimming callbacks and all unrelated damage.
   // Only the local swimming drain and exhaustion check use the helmet benefit.
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> c){return Replace(Replace(c,"UseStamina","SwimUse",1,false),"HaveStamina","SwimHave",1,false);}
  }
  [HarmonyPatch(typeof(Character),"UpdateSwimming")]
  private static class SwimMovement {
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> input){
    var code=new List<CodeInstruction>(input);
    var modify=AccessTools.Method(typeof(SEMan),"ApplyStatusEffectSpeedMods");
    var sites=new List<int>();for(int i=0;i<code.Count;i++)if(code[i].Calls(modify))sites.Add(i);
    // First call modifies swimming speed; second modifies turn speed. This anchor
    // survives Impactful Skills replacing the original m_swimSpeed field read.
    if(sites.Count!=2)throw new InvalidOperationException("Unsupported swimming speed-modifier call count: "+sites.Count);
    int site=sites[0];var local=code[site-3];
    if(local.opcode!=OpCodes.Ldloca&&local.opcode!=OpCodes.Ldloca_S)throw new InvalidOperationException("Swimming speed local not found");
    code.InsertRange(site+1,new[]{new CodeInstruction(OpCodes.Ldarg_0),new CodeInstruction(local.opcode,local.operand),new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Effects),"SwimSpeed"))});
    return code;
   }
  }
  [HarmonyPatch(typeof(Player),"UpdateHover")]
  private static class Hover{
   private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> c){return Replace(c,"InPlaceMode","HoverPlaceMode",1,false);}
  }
  [HarmonyPatch(typeof(Hud),"Awake")]
  private static class HudLayout{
   private static void Postfix(RectTransform ___m_pieceHealthRoot){if(___m_pieceHealthRoot)___m_pieceHealthRoot.anchoredPosition+=new Vector2(0,20);}
  }
 }
}
