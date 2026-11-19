using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace OctoberJemini.PortalAtlas
{
    internal sealed class Destination
    {
        internal string id,name,biome;
        internal bool pillar,allowAll;
        internal Vector3 position;
        internal Quaternion rotation;
        internal float exit;
        internal ZDOID Uid { get { return AtlasNetwork.ParseId(id); } }
    }

    internal sealed class AtlasNetwork
    {
        private CustomRPC request,response,registerPillar;
        private long world;
        private AtlasWorldData data;
        private string file;
        private bool dirty,storageFailed,scanActive,refreshAfterRename,firstScan=true;
        private float nextScan,nextRefresh,nextSave,nextRequest;
        private int scanIndex,nonce,pendingTravel;
        private float travelDeadline;
        private readonly List<ZDO> scan=new List<ZDO>();
        private readonly HashSet<ZDOID> pillars=new HashSet<ZDOID>();
        private readonly Dictionary<string,Destination> authoritative=new Dictionary<string,Destination>();
        private readonly Dictionary<string,float> limits=new Dictionary<string,float>();
        internal readonly List<Destination> Destinations=new List<Destination>();
        internal int Revision {get;private set;}
        private string signature="";
        private static readonly System.Reflection.MethodInfo ServerMethod=typeof(ZRoutedRpc).GetMethod("GetServerPeerID",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
        internal static long ServerPeer {get {return ZRoutedRpc.instance==null?0:(long)ServerMethod.Invoke(ZRoutedRpc.instance,null);}}
        internal static string Id(ZDOID id) {return id.UserID.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+id.ID.ToString(System.Globalization.CultureInfo.InvariantCulture);}
        internal static ZDOID ParseId(string value)
        {
            var parts=(value??"").Split(new[]{':'}); long user;uint id;
            if(parts.Length!=2 || !Int64.TryParse(parts[0],out user) || !UInt32.TryParse(parts[1],out id))return ZDOID.None;
            return new ZDOID(user,id);
        }
        internal Destination Find(ZDOID id){return Destinations.FirstOrDefault(d=>d.id==Id(id));}
        internal void Register()
        {
            request=NetworkManager.Instance.AddRPC("OCT_PortalAtlas_Request",OnRequest,Ignore);
            response=NetworkManager.Instance.AddRPC("OCT_PortalAtlas_Response",Ignore,OnResponse);
            registerPillar=NetworkManager.Instance.AddRPC("OCT_PortalAtlas_RegisterPillar",OnRegisterPillar,Ignore);
        }
        private IEnumerator OnRegisterPillar(long sender,ZPackage package)
        {
            try
            {
                if(ZNet.instance && ZNet.instance.IsServer() && ZNet.instance.GetPeer(sender)!=null && package.ReadLong()==world)
                {
                    var key="register:"+sender;float next;
                    if(!limits.TryGetValue(key,out next) || Time.unscaledTime>=next)
                    {
                        limits[key]=Time.unscaledTime+.05f;
                        var zdo=ZDOMan.instance.GetZDO(ParseId(package.ReadString()));
                        if(zdo!=null && zdo.GetOwner()==sender)Track(zdo);
                    }
                }
            }
            catch(Exception ex){Plugin.Warn("Rejected malformed pillar registration: "+ex.Message);}
            yield break;
        }
        internal void RegisterPillar(ZDO zdo)
        {
            if(!ZNet.instance || zdo==null)return;
            if(ZNet.instance.IsServer())Track(zdo);
            else
            {
                var package=new ZPackage();package.Write(ZNet.instance.GetWorldUID());package.Write(Id(zdo.m_uid));
                registerPillar.SendPackage(ServerPeer,package);
            }
        }
        private static IEnumerator Ignore(long sender,ZPackage package){yield break;}
        private IEnumerator OnRequest(long sender,ZPackage package)
        {
            try {if(ZNet.instance && ZNet.instance.IsServer())ProcessRequest(sender,package);}
            catch(Exception ex){Plugin.Warn("Rejected malformed Atlas request: "+ex.Message);}
            yield break;
        }
        private IEnumerator OnResponse(long sender,ZPackage package)
        {
            try {if(sender==ServerPeer)Receive(package);}
            catch(Exception ex){Plugin.Warn("Rejected malformed Atlas response: "+ex.Message);}
            yield break;
        }
        internal void Tick()
        {
            if(!ZNet.instance || ZDOMan.instance==null || ZNet.instance.GetWorldUID()==0)
            {if(world!=0)Reset();return;}
            var current=ZNet.instance.GetWorldUID();
            if(world!=current)
            {
                Reset();world=current;
                if(ZNet.instance.IsServer())Load();
            }
            if(ZNet.instance.IsServer())
            {
                if(!scanActive && Time.unscaledTime>=nextScan)
                {scanActive=true;scanIndex=0;scan.Clear();nextScan=Time.unscaledTime+30;}
                if(scanActive)
                {
                    // Native incremental API bounds each step to ~400 sectors, never a scene-wide FindObjects scan.
                    if(ZDOMan.instance.GetAllZDOsWithPrefabIterative(Plugin.Pillar,scan,ref scanIndex))
                    {
                        foreach(var zdo in scan)
                        {
                            pillars.Add(zdo.m_uid);
                            data.Observe(zdo.GetString("oct_atlas_biome",Biome(zdo.GetPosition())),zdo.GetInt("oct_atlas_number",0));
                        }
                        scanActive=false;firstScan=false;nextRefresh=0;
                    }
                }
                if(Time.unscaledTime>=nextRefresh)
                {nextRefresh=Time.unscaledTime+2;Refresh();}
                if(dirty && Time.unscaledTime>=nextSave){Save();nextSave=Time.unscaledTime+5;}
            }
            if(refreshAfterRename)RequestList();
            if(pendingTravel!=0 && Time.unscaledTime>travelDeadline)
            {pendingTravel=0;Plugin.Notice("Atlas travel request timed out. Please try again.");}
        }
        private void Reset()
        {
            Save();world=0;data=null;file=null;dirty=false;storageFailed=false;
            pillars.Clear();authoritative.Clear();Destinations.Clear();scan.Clear();limits.Clear();
            scanActive=false;refreshAfterRename=false;scanIndex=0;firstScan=true;nextScan=nextRefresh=nextRequest=0;pendingTravel=0;signature="";Revision++;
        }
        private void Load()
        {
            file=Path.Combine(BepInEx.Paths.ConfigPath,Plugin.Id,"world-"+world+".json");
            data=new AtlasWorldData();
            if(!File.Exists(file))return;
            try
            {
                var loaded=JsonUtility.FromJson<AtlasWorldData>(File.ReadAllText(file));
                if(loaded==null || loaded.names==null || loaded.counters==null || loaded.version!=1)throw new InvalidDataException("Unsupported or invalid save data");
                data=loaded;
                foreach(var name in data.names)data.Observe(name.biome,name.number);
            }
            catch(Exception ex)
            {
                // Preserve the damaged file and refuse to overwrite world naming history.
                storageFailed=true;Plugin.Warn("Atlas naming data needs recovery; original file preserved: "+file+" "+ex.Message);
            }
        }
        internal void Save()
        {
            if(!dirty || data==null || String.IsNullOrEmpty(file) || storageFailed)return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                var tmp=file+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(data,true));
                if(File.Exists(file))File.Replace(tmp,file,file+".bak");else File.Move(tmp,file);
                dirty=false;
            }
            catch(Exception ex){Plugin.Warn("Could not save Atlas names: "+ex.Message);}
        }
        private void Refresh()
        {
            if(data==null || !Game.instance || WorldGenerator.instance==null)return;
            authoritative.Clear();
            foreach(var zdo in ZDOMan.instance.GetPortalList())
            {
                if(zdo==null || !Game.instance.PortalPrefabHash.Contains(zdo.GetPrefab()))continue;
                var id=Id(zdo.m_uid);var alias=data.Find(id);
                var biome=Biome(zdo.GetPosition());
                var label=alias!=null && alias.label.Length>0?alias.label:zdo.GetString(ZDOVars.s_tag,"");
                if(String.IsNullOrWhiteSpace(label))label="Unnamed portal";
                var prefab=ZNetScene.instance?ZNetScene.instance.GetPrefab(zdo.GetPrefab()):null;
                var portal=prefab?prefab.GetComponent<TeleportWorld>():null;
                authoritative[id]=new Destination {id=id,name=AtlasWorldData.Clean(label),biome=biome,position=zdo.GetPosition(),rotation=zdo.GetRotation(),exit=portal?portal.m_exitDistance:1.5f,allowAll=portal && portal.m_allowAllItems};
            }
            var removed=new List<ZDOID>();
            foreach(var uid in pillars)
            {
                var zdo=ZDOMan.instance.GetZDO(uid);
                if(zdo==null || zdo.GetPrefab()!=Plugin.Pillar.GetStableHashCode()){removed.Add(uid);continue;}
                var id=Id(uid);var record=data.Find(id);
                if(record==null)
                {
                    // Reconstruct all existing biome maxima before assigning a new sequence number.
                    if(firstScan || storageFailed)continue;
                    record=data.Assign(id,Biome(zdo.GetPosition()),zdo.GetInt("oct_atlas_number",0),zdo.GetString("oct_atlas_label",""));
                    dirty=true;WritePillarName(zdo,record);
                }
                authoritative[id]=new Destination{id=id,name=record.Display,biome=record.biome,pillar=true,position=zdo.GetPosition(),rotation=zdo.GetRotation(),exit=1.5f};
            }
            foreach(var uid in removed)pillars.Remove(uid);
        }
        internal static string Biome(Vector3 position)
        {
            var biome=WorldGenerator.instance.GetBiome(position).ToString();
            switch(biome)
            {
                case "BlackForest":return "Black Forest";
                case "Mountain":return "Mountains";
                case "AshLands":return "Ashlands";
                case "DeepNorth":return "Deep North";
                default:return biome;
            }
        }
        internal void Track(ZDO zdo)
        {
            if(zdo!=null && ZNet.instance && ZNet.instance.IsServer() && zdo.GetPrefab()==Plugin.Pillar.GetStableHashCode())
            {pillars.Add(zdo.m_uid);nextRefresh=0;}
        }
        private static void WritePillarName(ZDO zdo,AtlasName record)
        {
            if(zdo.GetOwner()==ZDOMan.GetSessionID())
            {
                zdo.Set("oct_atlas_biome",record.biome);zdo.Set("oct_atlas_number",record.number);zdo.Set("oct_atlas_label",record.label);
            }
            else if(zdo.GetOwner()!=0 && ZRoutedRpc.instance!=null)
                ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(),zdo.m_uid,"OCT_AtlasLabel",record.biome,record.number,record.label);
        }
        private bool Authorized(long sender,string source,bool readOnly)
        {
            if(sender==ZDOMan.GetSessionID())
            {
                if(Plugin.HasAtlas(Player.m_localPlayer) || (readOnly && Plugin.Unlocked(Player.m_localPlayer)))return true;
                return Player.m_localPlayer && IsNearPortal(ParseId(source),Player.m_localPlayer.transform.position);
            }
            var peer=ZNet.instance.GetPeer(sender);
            var character=peer==null?null:ZDOMan.instance.GetZDO(peer.m_characterID);
            // Valheim inventories are client-owned; this presence flag is not an anti-cheat system.
            return character!=null && (character.GetBool("oct_atlas_carried",false) || (readOnly && character.GetBool("oct_atlas_unlocked",false)) || IsNearPortal(ParseId(source),character.GetPosition()));
        }
        internal static bool IsNearPortal(ZDOID source,Vector3 playerPosition)
        {
            if(source.Equals(ZDOID.None) || ZDOMan.instance==null || !Game.instance)return false;
            var zdo=ZDOMan.instance.GetZDO(source);
            return zdo!=null && Game.instance.PortalPrefabHash.Contains(zdo.GetPrefab()) && (zdo.GetPosition()-playerPosition).sqrMagnitude<=36;
        }
        private void ProcessRequest(long sender,ZPackage package)
        {
            if(world==0 || package.ReadLong()!=world)return;
            var op=package.ReadInt();var token=package.ReadInt();var id=package.ReadString();var label=package.ReadString();var source=package.ReadString();
            if(op<0 || op>2 || id.Length>80 || source.Length>80 || label.Length>200 || !Authorized(sender,source,op==0))return;
            var key=sender+":"+op;float next;
            if(limits.TryGetValue(key,out next) && Time.unscaledTime<next)return;
            limits[key]=Time.unscaledTime+(op==0?.75f:op==1?3:1);
            Refresh();
            var reply=new ZPackage();reply.Write(world);reply.Write(op);reply.Write(token);
            if(op==0)
            {
                reply.Write("");reply.Write(authoritative.Count);
                foreach(var entry in authoritative.Values.OrderBy(d=>d.id))WriteEntry(reply,entry);
            }
            else
            {
                Destination entry;
                var zdo=ZDOMan.instance.GetZDO(ParseId(id));
                if(!authoritative.TryGetValue(id,out entry) || zdo==null)
                    reply.Write("That destination no longer exists.");
                else if(op==1)
                {
                    var blocked=WorldTravelBlock();reply.Write(blocked);
                    if(blocked.Length==0)
                    {
                        // Derive the arrival point from the current world object; never accept client coordinates.
                        reply.Write(entry.position+entry.rotation*Vector3.forward*entry.exit+Vector3.up);
                        reply.Write(entry.rotation);reply.Write(entry.allowAll);
                        ZDOMan.instance.ForceSendZDO(sender,zdo.m_uid);
                    }
                }
                else if(storageFailed)reply.Write("Atlas naming data needs recovery before it can be changed.");
                else
                {
                    var record=data.Find(id);
                    if(record==null){record=new AtlasName{id=id,biome=entry.biome};data.names.Add(record);}
                    record.label=AtlasWorldData.Clean(label);dirty=true;
                    if(entry.pillar)WritePillarName(zdo,record);
                    Save();nextRefresh=0;reply.Write("Renamed to "+(entry.pillar?record.Display:(record.label.Length>0?record.label:"the portal's tag"))+".");
                }
            }
            if(sender==ZDOMan.GetSessionID())Receive(new ZPackage(reply.GetArray()));else response.SendPackage(sender,reply);
        }
        private static void WriteEntry(ZPackage package,Destination d)
        {
            package.Write(d.id);package.Write(d.name);package.Write(d.biome);package.Write(d.pillar);package.Write(d.position);
        }
        private void Send(int op,string id="",string label="",string sourceOverride=null)
        {
            var source=sourceOverride??Id(Plugin.UI.Source);
            if(!ZNet.instance || ZDOMan.instance==null || !Player.m_localPlayer || (!Plugin.HasAtlas(Player.m_localPlayer) && !(op==0 && Plugin.Unlocked(Player.m_localPlayer)) && !IsNearPortal(ParseId(source),Player.m_localPlayer.transform.position)))return;
            var token=++nonce;
            if(op==1){pendingTravel=token;travelDeadline=Time.unscaledTime+12;}
            var package=new ZPackage();package.Write(world);package.Write(op);package.Write(token);package.Write(id);package.Write(label??"");package.Write(source);
            if(ZNet.instance.IsServer())ProcessRequest(ZDOMan.GetSessionID(),new ZPackage(package.GetArray()));
            else request.SendPackage(ServerPeer,package);
        }
        internal void RequestList(bool force=false)
        {
            if(Time.unscaledTime<nextRequest)return;
            nextRequest=Time.unscaledTime+(force?1:3);Send(0);
        }
        internal void Rename(ZDOID id,string text){Send(2,Id(id),AtlasWorldData.Clean(text));nextRequest=0;}
        internal void RenameAtPortal(ZDOID id,string text){Send(2,Id(id),AtlasWorldData.Clean(text),Id(id));nextRequest=0;}
        internal void Travel(Destination destination)
        {
            if(destination==null || pendingTravel!=0 || !Plugin.UI.Active)return;
            // Item transport permissions are checked again using the authoritative destination in the reply.
            var blocked=ClientTravelBlock(true);
            if(blocked.Length>0){Plugin.Notice(blocked);return;}
            Send(1,destination.id);
        }
        private void Receive(ZPackage package)
        {
            if(package.ReadLong()!=world)return;
            var op=package.ReadInt();var token=package.ReadInt();var message=package.ReadString();
            if(op==0)
            {
                if(message.Length>0)return;
                ReceiveCatalogue(package);refreshAfterRename=false;
            }
            else if(op==1)
            {
                if(token!=pendingTravel || Time.unscaledTime>travelDeadline)return;
                pendingTravel=0;
                if(message.Length>0){Plugin.Notice(message);return;}
                var position=package.ReadVector3();var rotation=package.ReadQuaternion();var allowAll=package.ReadBool();
                var blocked=ClientTravelBlock(allowAll);
                if(blocked.Length>0){Plugin.Notice(blocked);return;}
                if(!Plugin.UI.Active){Plugin.Notice("Atlas travel cancelled because the map was closed.");return;}
                Plugin.UI.SuppressPortalUntil=Time.unscaledTime+5;
                Plugin.UI.Close();
                if(!Player.m_localPlayer.TeleportTo(position,rotation,true))Plugin.Notice("Unable to travel right now.");
            }
            else if(op==2){Plugin.Notice(message);nextRequest=0;refreshAfterRename=true;}
        }
        private void ReceiveCatalogue(ZPackage package)
        {
            var count=package.ReadInt();if(count<0 || count>100000)throw new InvalidDataException("Invalid catalogue length");
            var entries=new List<Destination>();
            for(int i=0;i<count;i++)
            {
                var entry=new Destination{id=package.ReadString(),name=package.ReadString(),biome=package.ReadString(),pillar=package.ReadBool(),position=package.ReadVector3()};
                if(entry.id.Length>80 || entry.name.Length>200 || entry.biome.Length>80)throw new InvalidDataException("Invalid destination");
                entries.Add(entry);
            }
            var current=String.Join("|",entries.Select(e=>e.id+"/"+e.name+"/"+e.position+"/"+e.pillar));
            if(current!=signature){signature=current;Destinations.Clear();Destinations.AddRange(entries);Revision++;}
        }
        internal static string WorldTravelBlock()
        {
            if(!ZoneSystem.instance)return "The world is not ready yet.";
            if(ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals))return "Portals are disabled in this world.";
            if(ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals))
            {
                float bosses;
                if((RandEventSystem.instance && !String.IsNullOrEmpty(RandEventSystem.instance.GetBossEvent())) || (ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses,out bosses) && bosses>0))
                    return "Portal travel is blocked while a boss is active.";
            }
            return "";
        }
        internal static string ClientTravelBlock(bool allowAll)
        {
            var player=Player.m_localPlayer;
            if(!player || (!Plugin.HasAtlas(player) && !IsNearPortal(Plugin.UI.Source,player.transform.position)))return "Carry a Portal Atlas or stand beside a portal to travel.";
            if(player.IsDead() || player.IsTeleporting() || player.IsSleeping() || player.InCutscene())return "You cannot travel right now.";
            var blocked=WorldTravelBlock();if(blocked.Length>0)return blocked;
            if(!player.IsTeleportable(allowAll))return "An item in your inventory prevents teleporting.";
            return "";
        }
    }
}
