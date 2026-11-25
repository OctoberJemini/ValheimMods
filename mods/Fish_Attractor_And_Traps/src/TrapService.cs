using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Newtonsoft.Json;
using UnityEngine;

namespace OctoberJemini.FishAttractor {
 internal sealed class TrapService {
  internal static TrapService Instance;
  private CustomRPC requests,replies,profiles;
  private TrapLedger ledger;private long world;private string file;private bool fault;private string saved="";
  private float nextProfile,nextCatalog;
  private readonly Dictionary<string,Presence> active=new Dictionary<string,Presence>();
  private sealed class Presence {internal Vector3 P;internal double Seen;}
  private static readonly MethodInfo Server=typeof(ZRoutedRpc).GetMethod("GetServerPeerID",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
  private long ServerPeer {get{return (long)Server.Invoke(ZRoutedRpc.instance,null);}}
  internal static string Id(ZDOID id){return id.UserID+":"+id.ID;}
  internal static ZDOID Parse(string id){var p=id.Split(':');return p.Length==2?new ZDOID(long.Parse(p[0]),uint.Parse(p[1])):ZDOID.None;}
  internal void Register(){Instance=this;requests=NetworkManager.Instance.AddRPC("OCT_FishTrap_Request_v2",OnRequest,Ignore);replies=NetworkManager.Instance.AddRPC("OCT_FishTrap_Reply_v2",Ignore,OnReply);profiles=NetworkManager.Instance.AddRPC("OCT_FishTrap_Profile_v2",OnProfile,Ignore);}
  private static IEnumerator Ignore(long sender,ZPackage p){yield break;}
  internal void Update(){
   if(!ZNet.instance||!ZNetScene.instance||ZRoutedRpc.instance==null){world=0;return;}
   long current=ZNet.instance.GetWorldUID();if(current!=world){world=current;ledger=null;fault=false;saved="";nextProfile=0;nextCatalog=0;active.Clear();if(ZNet.instance.IsServer())Load();}
   if(Time.unscaledTime>=nextCatalog){FishTrap.BuildCatalog();nextCatalog=Time.unscaledTime+30;}
   PublishProfile(false);
  }
  private void Load(){
   try{
    string directory=Path.Combine(Paths.ConfigPath,"OctoberJemini.FishTraps");Directory.CreateDirectory(directory);file=Path.Combine(directory,"world-"+world+".json");
    ledger=File.Exists(file)?JsonConvert.DeserializeObject<TrapLedger>(File.ReadAllText(file)):new TrapLedger();
    if(ledger==null||ledger.Version!=1||ledger.Profiles==null||ledger.Traps==null||ledger.Harvests==null)throw new InvalidDataException("Invalid fishing ledger");
   }catch(Exception e){Fail(e);}
  }
  private void Save(){
   if(fault||ledger==null)throw new InvalidOperationException("Fishing ledger unavailable");
   try{string text=JsonConvert.SerializeObject(ledger,Formatting.Indented);if(text==saved)return;string temp=file+".tmp";File.WriteAllText(temp,text);if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);saved=text;}
   catch(Exception e){Fail(e);throw;}
  }
  private void Fail(Exception e){fault=true;Debug.LogError("[Fish Traps] Production paused; preserve/restore world ledger: "+e.Message);}
  internal void PublishProfile(bool force){
   var p=Player.m_localPlayer;if(world==0||!p||!ZNet.instance||ZRoutedRpc.instance==null||(!force&&Time.unscaledTime<nextProfile))return;
   nextProfile=Time.unscaledTime+10;
   string fish=String.Join(",",TrapDiscovery.Known(p).OrderBy(x=>x));float skill=p.GetSkillFactor(Skills.SkillType.Fishing)*100;
   var package=new ZPackage();package.Write(world);package.Write(p.GetPlayerID());package.Write(p.GetPlayerName());package.Write(skill);package.Write(fish);
   if(ZNet.instance.IsServer())AcceptProfile(ZDOMan.GetSessionID(),LocalMessage.ReadFromStart(package));else profiles.SendPackage(ServerPeer,package);
  }
  private bool PlayerMatches(long sender,long player){
   if(sender==ZDOMan.GetSessionID())return Player.m_localPlayer&&Player.m_localPlayer.GetPlayerID()==player;
   var peer=ZNet.instance.GetPeer(sender);var zdo=peer==null?null:ZDOMan.instance.GetZDO(peer.m_characterID);
   return zdo!=null&&zdo.GetLong(ZDOVars.s_playerID,0)==player;
  }
  private IEnumerator OnProfile(long sender,ZPackage p){try{AcceptProfile(sender,p);}catch(Exception e){Debug.LogWarning("[Fish Traps] Profile rejected: "+e.Message);}yield break;}
  private void AcceptProfile(long sender,ZPackage p){
   if(!ZNet.instance.IsServer()||p.ReadLong()!=world||fault||ledger==null)return;
   long owner=p.ReadLong();string name=p.ReadString();float skill=p.ReadSingle();string species=p.ReadString();
   if(!PlayerMatches(sender,owner)||name.Length>100||species.Length>16000)return;
   var known=new HashSet<string>(species.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries).Where(FishTrap.IsFish),StringComparer.Ordinal);
   ledger.Profiles[owner]=new TrapProfile{Name=name,Skill=TrapEconomy.Skill(skill),Fish=known};Save();
  }
  internal void Request(FishTrap trap,bool ready,string bait,string[] fits,Vector3 water){
   if(world==0||ZRoutedRpc.instance==null)return;
   var zdo=trap.View.GetZDO();var p=new ZPackage();p.Write(world);p.Write(Id(zdo.m_uid));p.Write(zdo.GetLong("oct_trap_applied_v2",0));p.Write(ready);p.Write(bait??"");p.Write(String.Join(",",fits));p.Write(water);
   if(ZNet.instance.IsServer())Process(ZDOMan.GetSessionID(),LocalMessage.ReadFromStart(p));else requests.SendPackage(ServerPeer,p);
  }
  private IEnumerator OnRequest(long sender,ZPackage p){try{Process(sender,p);}catch(Exception e){Debug.LogWarning("[Fish Traps] Request paused: "+e.Message);}yield break;}
  private void Process(long sender,ZPackage p){
   if(!ZNet.instance.IsServer()||p.ReadLong()!=world||ledger==null||fault)return;
   string id=p.ReadString();long ack=p.ReadLong();bool ready=p.ReadBool();string bait=p.ReadString(),fitString=p.ReadString();Vector3 water=p.ReadVector3();
   if(id.Length>80||bait.Length>100||fitString.Length>4000)return;
   var zdo=ZDOMan.instance.GetZDO(Parse(id));if(zdo==null||zdo.GetPrefab()!=StringExtensionMethods.GetStableHashCode("oct_fish_trap")||zdo.GetOwner()!=sender)return;
   Vector3 position=zdo.GetPosition();if((water-position).sqrMagnitude>100)return;
   double now=ZNet.instance.GetTime().Ticks/(double)TimeSpan.TicksPerSecond;
   active[id]=new Presence{P=position,Seen=now};
   long owner=zdo.GetLong(ZDOVars.s_creator,0);TrapProfile profile;bool hasProfile=ledger.Profiles.TryGetValue(owner,out profile);if(!hasProfile)profile=new TrapProfile{Name="Owner must join once",Skill=0};
   var fit=new HashSet<string>(fitString.Split(','),StringComparer.Ordinal);
   var candidates=FishTrap.Eligible(bait,Heightmap.FindBiome(water),profile.Fish).Where(c=>fit.Contains(c.Fish)).ToArray();
   string status;var attempt=TrapEconomy.Step(ledger,id,ack,position.x,position.z,position.y,now,profile.Skill,ready&&hasProfile,bait,candidates.Select(c=>c.Fish).ToArray(),candidates.Select(c=>c.Weight).ToArray(),UnityEngine.Random.value,UnityEngine.Random.value,out status);
   if(!hasProfile)status="Waiting for trap owner's fishing profile";
   else if(candidates.Length==0&&ready)status="Wrong biome or undiscovered fish for this bait";
   Save(); // Persist receipt and stock debit BEFORE sending any catch to a network owner.
   int competitors=active.Values.Count(t=>now-t.Seen<=45&&TrapEconomy.Nearby(position.x,position.z,position.y,t.P.x,t.P.z,t.P.y));
   var result=new ZPackage();result.Write(world);result.Write(id);result.Write(attempt.Sequence);result.Write(attempt.Pending);result.Write(attempt.Lost);result.Write(attempt.Fish);result.Write(attempt.Bait);result.Write(attempt.Due);result.Write(status);result.Write(profile.Name);result.Write(profile.Skill);result.Write(TrapEconomy.Available(ledger,position.x,position.z,position.y,now));result.Write(competitors);
   if(sender==ZDOMan.GetSessionID())Receive(LocalMessage.ReadFromStart(result));else replies.SendPackage(sender,result);
  }
  private IEnumerator OnReply(long sender,ZPackage p){try{if(sender==ServerPeer)Receive(p);}catch(Exception e){Debug.LogWarning("[Fish Traps] Reply paused: "+e.Message);}yield break;}
  private void Receive(ZPackage p){
   if(p.ReadLong()!=world)return;string id=p.ReadString();var trap=FishTrap.Loaded.FirstOrDefault(t=>t&&t.View&&t.View.IsValid()&&Id(t.View.GetZDO().m_uid)==id);if(!trap||!trap.View.IsOwner())return;
   trap.Receive(p);
  }
 }
}
