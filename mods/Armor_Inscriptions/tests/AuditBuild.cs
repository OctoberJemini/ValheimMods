using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
class AuditBuild{
 static int count;
 static void Check(bool condition,string why){count++;if(!condition)throw new Exception(why);}
 static void Main(string[] args){
  var mod=AssemblyDefinition.ReadAssembly(args[0]);
  var game=AssemblyDefinition.ReadAssembly(args[1]);
  var epic=AssemblyDefinition.ReadAssembly(args[2]);
  string[] callCounts={"Player:UpdatePlacement:HaveStamina:2","Player:UpdatePlacement:UseStamina:2","Player:Repair:UseStamina:1","Player:CheckRun:HaveStamina:2","Player:CheckRun:UseStamina:1","Character:Jump:HaveStamina:2","Player:OnJump:UseStamina:1","Player:UpdateHover:InPlaceMode:1","Player:OnSwimming:UseStamina:1","Player:OnSwimming:HaveStamina:1"};
  foreach(string check in callCounts){
   var p=check.Split(':');var method=game.MainModule.GetType(p[0]).Methods.Single(m=>m.Name==p[1]);
   int n=method.Body.Instructions.Count(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).DeclaringType.FullName=="Character"&&((MethodReference)i.Operand).Name==p[2]);
   Check(n==int.Parse(p[3]),"Native cost/hover call count changed: "+check);
  }
  foreach(string field in new[]{"m_rightItem","m_legItem","m_chestItem","m_helmetItem"})Check(game.MainModule.GetType("Humanoid").Fields.Any(f=>f.Name==field),"Equipment slot "+field);
  Check(game.MainModule.GetType("Hud").Fields.Any(f=>f.Name=="m_pieceHealthRoot"&&f.FieldType.Name=="RectTransform"),"HUD field");
  var swimming=game.MainModule.GetType("Character").Methods.Single(m=>m.Name=="UpdateSwimming");
  Check(swimming.Body.Instructions.Count(i=>i.OpCode==OpCodes.Ldfld&&((FieldReference)i.Operand).Name=="m_swimSpeed")==1,"exact swimming speed read target");
  var speedMods=swimming.Body.Instructions.Where(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="ApplyStatusEffectSpeedMods").ToArray();
  Check(speedMods.Length==2,"swim speed and turn speed anchors");
  Check(speedMods[0].Previous.Previous.Previous.OpCode==OpCodes.Ldloca_S||speedMods[0].Previous.Previous.Previous.OpCode==OpCodes.Ldloca,"speed reference local anchor");
  var movement=mod.MainModule.GetTypes().Single(t=>t.FullName.EndsWith("Effects/SwimMovement")).Methods.Single(m=>m.Name=="Transpiler");
  Check(!movement.Body.Instructions.Any(i=>i.Operand is FieldReference&&((FieldReference)i.Operand).Name=="SwimSpeedField"),"do not require field read replaced by Impactful Skills");
  var impact=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[2]),"../MidnightMods-ImpactfulSkills/ImpactfulSkills.dll"));
  var impactSwim=impact.MainModule.GetTypes().Single(t=>t.FullName.EndsWith("Swimming/SwimmingSpeedPatch")).Methods.Single(m=>m.Name=="Transpiler");
  Check(impactSwim.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldstr&&(string)i.Operand=="m_swimSpeed")&&impactSwim.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="RemoveInstruction"),"installed Impactful Skills replaces the speed field read");
  Check(!impactSwim.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldstr&&(string)i.Operand=="ApplyStatusEffectSpeedMods"),"Impactful Skills does not target the new anchor");
  var onSwim=game.MainModule.GetType("Player").Methods.Single(m=>m.Name=="OnSwimming");
  Check(onSwim.Body.Instructions.Count(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="RaiseSkill")==1,"swim skill advancement retained in native method");
  var effects=mod.MainModule.GetTypes().Single(t=>t.Name=="Effects");
  var speed=effects.Methods.Single(m=>m.Name=="SwimSpeed");
  Check(!speed.Body.Instructions.Any(i=>i.OpCode==OpCodes.Stfld),"no persistent speed field mutation or repeated multiplier accumulation");
  foreach(string helper in new[]{"SwimHave","SwimUse"}){
   var body=effects.Methods.Single(m=>m.Name==helper).Body.Instructions;
   Check(body.Any(i=>i.OpCode==OpCodes.Ldstr&&(string)i.Operand=="snorkel"),"swim stamina checks snorkel only");
   Check(body.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name==(helper=="SwimHave"?"HaveStamina":"UseStamina")),"uninscribed swimming preserves native fallback");
  }
  var apply=mod.MainModule.GetTypes().Single(t=>t.Name=="ApplyMenu");
  foreach(var method in apply.Methods.Where(m=>m.HasBody)){
   foreach(var ins in method.Body.Instructions.Where(i=>i.OpCode==OpCodes.Stfld)){
    var f=(FieldReference)ins.Operand;
    if(f.DeclaringType.FullName=="ItemDrop/ItemData")Check(f.Name=="m_customData"||(method.Name=="Consume"&&f.Name=="m_stack"),"Application mutates protected item field "+f.Name);
   }
  }
  foreach(var type in mod.MainModule.GetTypes()){
   foreach(var attr in type.CustomAttributes.Where(a=>a.AttributeType.Name=="HarmonyPatch"&&a.ConstructorArguments.Count>=2)){
    var t=attr.ConstructorArguments[0].Value as TypeReference;
    var name=attr.ConstructorArguments[1].Value as string;
    if(t==null||name==null)continue;
    Check(game.MainModule.GetType(t.FullName).Methods.Any(m=>m.Name==name),"Patch target "+t.FullName+"."+name);
    Check(name!="UseStamina"&&name!="HaveStamina","No global stamina patches");
   }
  }
  var remove=epic.MainModule.GetType("EpicLoot.MagicItemData").Methods.Single(m=>m.Name=="Remove");
  Check(remove.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldstr&&(string)i.Operand=="randyknapp.mods.epicloot#EpicLoot.MagicItemComponent"),"Epic Loot removes only its own key");
  Check(!remove.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="Clear"),"Disenchant does not clear all metadata");
  foreach(string id in new[]{"runner","leaper","cultivator","builder","snorkel","tidewalker","bench"})Check(mod.MainModule.Resources.Any(r=>r.Name=="patch."+id+".png"),"Embedded icon "+id);
  var persistence=mod.MainModule.GetTypes().Single(t=>t.FullName.EndsWith("UpgradePreservation/Added"));
  Check(persistence.Methods.Any(m=>m.Name=="Postfix"),"Upgrade metadata carry-over");
  var bench=mod.MainModule.GetTypes().Single(t=>t.Name=="Bench");
  Check(bench.Interfaces.Any(i=>i.InterfaceType.Name=="Interactable"),"Bench interactable");
  Check(bench.Interfaces.Any(i=>i.InterfaceType.Name=="Hoverable"),"Bench hover prompt");
  var use=bench.Methods.Single(m=>m.Name=="CanUse");
  Check(use.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="CheckAccess"),"Bench ward access");
  var applyFinish=apply.Methods.Single(m=>m.Name=="Apply");
  Check(applyFinish.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="CanUse"),"Revalidate bench before consuming");
  Check(applyFinish.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="Required"),"Tier cost before consuming");
  var hover=mod.MainModule.GetTypes().Single(t=>t.FullName.EndsWith("Plugin/Use"));
  Check(!hover.Methods.SelectMany(m=>m.HasBody?m.Body.Instructions.ToArray():new Instruction[0]).Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).DeclaringType.Name=="ApplyMenu"),"No application away from bench");
  var trader=mod.MainModule.GetTypes().Single(t=>t.FullName.EndsWith("Plugin/TraderItems")).Methods.Single(m=>m.Name=="Postfix");
  var ti=trader.Body.Instructions;
  Check(ti.Any(i=>i.OpCode==OpCodes.Ldstr&&(string)i.Operand=="Hildir"),"Patches sold by Hildir");
  foreach(string field in new[]{"m_tooltip","m_name","m_buyKey","m_incrementKey"}){
   var stores=ti.Where(i=>i.OpCode==OpCodes.Stfld&&((FieldReference)i.Operand).Name==field).ToArray();
   Check(stores.Length==1&&stores[0].Previous.OpCode==OpCodes.Ldstr&&(string)stores[0].Previous.Operand=="","Non-null trader string: "+field);
  }
  Check(!ti.Any(i=>i.OpCode==OpCodes.Stind_Ref),"Do not replace available stock list");
  Check(!ti.Any(i=>i.Operand is MethodReference&&new[]{"Clear","Remove","RemoveAll","RemoveAt"}.Contains(((MethodReference)i.Operand).Name)),"Preserve other mods' stock");
  var fill=game.MainModule.GetType("StoreGui").Methods.Single(m=>m.Name=="FillList");
  Check(fill.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldfld&&((FieldReference)i.Operand).Name=="m_tooltip"&&i.Next.Operand is MethodReference&&((MethodReference)i.Next.Operand).Name=="get_Length"),"Native store dereferences tooltip: regression target");
  var start=apply.NestedTypes.Single(t=>t.Name=="Start").Methods.Single(m=>m.Name=="Prefix");
  Check(!start.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="Consume"),"Craft start never consumes patches");
  var finish=apply.NestedTypes.Single(t=>t.Name=="Finish").Methods.Single(m=>m.Name=="Prefix");
  Check(finish.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="Apply"),"Native crafting completes through guarded transaction");
  Check(!apply.Methods.SelectMany(m=>m.HasBody?m.Body.Instructions.ToArray():new Instruction[0]).Any(i=>i.Operand is MethodReference&&new[]{"AddItem","RemoveItem","CreateWoodpanel","BlockInput"}.Contains(((MethodReference)i.Operand).Name)),"No armor recreation or separate menu");
  var open=apply.Methods.Single(m=>m.Name=="Open");
  Check(open.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="SetCraftingStation"),"Native station entry");
  Check(open.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).DeclaringType.Name=="InventoryGui"&&((MethodReference)i.Operand).Name=="Show"),"Native inventory UI entry");
  var timer=game.MainModule.GetType("InventoryGui").Methods.Single(m=>m.Name=="UpdateRecipe");
  Check(timer.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="DoCrafting"),"Native timer calls completion hook");
  var stationUpdate=game.MainModule.GetType("Player").Methods.Single(m=>m.Name=="UpdateStations");
  Check(stationUpdate.Body.Instructions.Any(i=>i.Operand is FieldReference&&((FieldReference)i.Operand).Name=="m_useAnimation"),"Native station animation still supported");
  var valid=apply.Methods.Single(m=>m.Name=="Valid");
  foreach(string requiredCall in new[]{"CanUse","ContainsItem","Compatible","Required","Has"})Check(valid.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name==requiredCall),"Completion validity: "+requiredCall);
  Check(valid.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).DeclaringType.Name=="TailoringExtensions"&&((MethodReference)i.Operand).Name=="Missing"),"Extension gate is rechecked at completion");
  foreach(var method in new[]{valid,apply.NestedTypes.Single(t=>t.Name=="Preview").Methods.Single(m=>m.Name=="Postfix")}){
   var gate=method.Body.Instructions.Single(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).DeclaringType.Name=="TailoringExtensions"&&((MethodReference)i.Operand).Name=="Missing");
   Check(gate.Previous.Operand is MethodReference&&((MethodReference)gate.Previous.Operand).Name=="Tier","extension gates use original armor tier, not discounted cost");
  }
  var costHelper=apply.Methods.Single(m=>m.Name=="Required");
  Check(costHelper.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="PatchCost"),"single shared cost rule for preview and consumption");
  var extensions=mod.MainModule.GetTypes().Single(t=>t.Name=="TailoringExtensions");
  var missing=extensions.Methods.Single(m=>m.Name=="Missing");
  Check(missing.Body.Instructions.Any(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).Name=="FindExtensions"),"Fresh native extension lookup, not stale station level");
  foreach(string icon in new[]{"rack","stitching"})Check(mod.MainModule.Resources.Any(r=>r.Name=="patch."+icon+".png"),"Extension icon "+icon);
  Console.WriteLine("Passed "+count+" installed-assembly compatibility and scope checks.");
 }
}
