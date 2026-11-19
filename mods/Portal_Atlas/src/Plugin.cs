using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace OctoberJemini.PortalAtlas
{
    [BepInPlugin(Id, "Portal Atlas", Version)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [DefaultExecutionOrder(10000)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "OctoberJemini.PortalAtlas";
        public const string Version = "0.2.8";
        public const string Item = "OCT_PortalAtlas";
        public const string Pillar = "OCT_AtlasPillar";
        public const string UnlockKey = "OCT_PortalAtlas_Unlocked";
        internal static Plugin Instance;
        internal static ItemDrop AtlasDrop;
        internal static ConfigEntry<int> Price;
        internal static AtlasNetwork Network;
        internal static AtlasUi UI;
        internal static PillarMapPins PillarPins;
        private Harmony harmony;
        private float inventoryCheck;
        private bool prefabsReady;

        private void Awake()
        {
            Instance = this;
            Price = Config.Bind("Trader", "AtlasPrice", 5000, new ConfigDescription("Haldor's price in coins.", new AcceptableValueRange<int>(1, 100000)));
            harmony = new Harmony(Id);
            harmony.PatchAll();
            Network = new AtlasNetwork();
            Network.Register();
            UI = new AtlasUi();
            PillarPins = new PillarMapPins();
            PrefabManager.OnVanillaPrefabsAvailable += AddPrefabs;
            Logger.LogInfo("Portal Atlas " + Version + " loaded (local preview).");
        }
        private void AddPrefabs()
        {
            if (prefabsReady) return;
            try
            {
                Icons.Load();
                var item = new CustomItem(Item, "Ruby", new ItemConfig
                {
                    Name = AtlasTrade.ItemName,
                    Description = "A trader's enchanted atlas. Carry it and use it from your inventory or hotbar to travel to any portal or Atlas pillar in this world. Owning an atlas unlocks the Atlas pillar in the hammer menu.",
                    StackSize = 1, Weight = 0.1f, Icon = Icons.Get("atlas")
                });
                AtlasDrop = item.ItemDrop;
                var shared = AtlasDrop.m_itemData.m_shared;
                shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
                shared.m_maxQuality = 1;
                shared.m_value = 0;
                shared.m_teleportable = true;
                shared.m_equipStatusEffect = null;
                MakeMapVisual(item.ItemPrefab);
                if (!ItemManager.Instance.AddItem(item)) throw new Exception("Item registration failed");

                var piece = new CustomPiece(Pillar, "stone_pillar", new PieceConfig
                {
                    Name = "Atlas pillar",
                    Description = "A named destination for the Portal Atlas. No workbench required. Use E to rename it; dismantle the pillar to remove its map marker.",
                    PieceTable = "Hammer", Category = "Misc", Icon = Icons.Get("pillar"),
                    Requirements = new[] { new RequirementConfig("Stone", 20, 0, true), new RequirementConfig("Wisp", 1, 0, true), new RequirementConfig("SurtlingCore", 1, 0, true) }
                });
                piece.Piece.m_craftingStation = null;
                piece.Piece.m_noInWater = false;
                MakePillarVisual(piece.PiecePrefab);
                piece.PiecePrefab.AddComponent<AtlasPillar>();
                if (!PieceManager.Instance.AddPiece(piece)) throw new Exception("Pillar registration failed");
                prefabsReady = true;
                Logger.LogInfo("Registered Portal Atlas and Atlas pillar.");
            }
            catch (Exception ex) { Logger.LogError("Could not register Portal Atlas: " + ex); }
        }
        private static void MakeMapVisual(GameObject prefab)
        {
            var original=prefab.GetComponentInChildren<Renderer>(true).sharedMaterial;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            var root = new GameObject("AtlasParchment");
            root.transform.SetParent(prefab.transform, false);
            root.transform.localPosition = Vector3.up * 0.12f;
            root.transform.localRotation = Quaternion.Euler(75, 0, 0);
            var mesh = new Mesh { name = "Atlas folded parchment" };
            var front=new[] { new Vector3(-.18f,-.14f,0),new Vector3(0,-.14f,.025f),new Vector3(.18f,-.14f,0),new Vector3(-.18f,.14f,0),new Vector3(0,.14f,.025f),new Vector3(.18f,.14f,0) };
            var uv=new[] { new Vector2(0,0),new Vector2(.5f,0),new Vector2(1,0),new Vector2(0,1),new Vector2(.5f,1),new Vector2(1,1) };
            mesh.vertices=front.Concat(front).ToArray();mesh.uv=uv.Concat(uv).ToArray();
            mesh.triangles = new[] { 0,3,1,1,3,4,1,4,2,2,4,5,7,9,6,10,9,7,8,10,7,11,10,8 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(Shader.Find("Standard") ?? original.shader);
            material.mainTexture = Icons.Get("atlas").texture;
            material.SetFloat("_Glossiness", 0);
            material.SetFloat("_Mode", 1); material.SetInt("_SrcBlend",1); material.SetInt("_DstBlend",0);
            material.SetInt("_ZWrite",1); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=2450;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        private static void MakePillarVisual(GameObject prefab)
        {
            AtlasPillarVisual.Build(prefab);
        }
        private void Update()
        {
            try
            {
                Network.Tick();
                if (Time.unscaledTime >= inventoryCheck)
                {
                    inventoryCheck = Time.unscaledTime + 1;
                    var player = Player.m_localPlayer;
                    if (player)
                    {
                        if (HasAtlas(player)) Unlock(player);
                        var view = player.GetComponent<ZNetView>();
                        if (view && view.IsValid() && view.IsOwner())
                        {
                            view.GetZDO().Set("oct_atlas_carried", HasAtlas(player));
                            view.GetZDO().Set("oct_atlas_unlocked", Unlocked(player));
                        }
                    }
                }
                Icons.MatchNativePortal();
                UI.Tick();
                PillarPins.Tick();
            }
            catch (Exception ex) { Logger.LogError("Atlas update failed: " + ex); enabled = false; }
        }
        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddPrefabs;
            AtlasRenameDialog.Detach(); Network?.Save(); UI?.Dispose(); PillarPins?.Dispose(); harmony?.UnpatchSelf();
        }
        private void LateUpdate(){UI?.RenderAboveMap();}
        internal static bool IsAtlas(ItemDrop.ItemData item)
        {
            return item != null && item.m_dropPrefab && item.m_dropPrefab.name == Item;
        }
        internal static bool HasAtlas(Player player)
        {
            return player && player.GetInventory().GetAllItems().Any(IsAtlas);
        }
        internal static bool Unlocked(Player player)
        {
            return player && (player.m_customData.ContainsKey(UnlockKey) || HasAtlas(player));
        }
        internal static void Unlock(Player player)
        {
            if (player.m_customData.ContainsKey(UnlockKey)) return;
            player.m_customData[UnlockKey]="1";
            AccessTools.Method(typeof(Player),"UpdateAvailablePiecesList")?.Invoke(player,null);
            player.Message(MessageHud.MessageType.TopLeft,"Atlas pillar unlocked in the hammer menu.");
        }
        internal static void Notice(string message)
        {
            if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageHud.MessageType.Center,message);
        }
        internal static void Warn(string message) { Instance.Logger.LogWarning(message); }
        private static bool PlacedPortal(TeleportWorld portal)
        {
            var view=portal?portal.GetComponent<ZNetView>():null;
            return view && view.IsValid() && Game.instance && Game.instance.PortalPrefabHash.Contains(view.GetZDO().GetPrefab());
        }
        [HarmonyPatch(typeof(TeleportWorld),nameof(TeleportWorld.Teleport))]
        private static class PortalTravel
        {
            private static bool Prefix(TeleportWorld __instance,Player player)
            {
                if(!PlacedPortal(__instance))return true;
                if(player!=Player.m_localPlayer || UI.Active || Time.unscaledTime<UI.SuppressPortalUntil)return false;
                var blocked=AtlasNetwork.WorldTravelBlock();if(blocked.Length>0){Notice(blocked);return false;}
                UI.Open(__instance.GetComponent<ZNetView>().GetZDO().m_uid);return false;
            }
        }
        [HarmonyPatch(typeof(TeleportWorld),"HaveTarget")]
        private static class PortalHasDestination
        {
            private static bool Prefix(TeleportWorld __instance,ref bool __result)
            {if(!PlacedPortal(__instance))return true;__result=true;return false;}
        }
        [HarmonyPatch(typeof(TeleportWorld),"TargetFound")]
        private static class PortalFoundDestination
        {
            private static bool Prefix(TeleportWorld __instance,ref bool __result)
            {if(!PlacedPortal(__instance))return true;__result=true;return false;}
        }
        [HarmonyPatch(typeof(TeleportWorld),nameof(TeleportWorld.Interact))]
        private static class PortalRename
        {
            private static bool Prefix(TeleportWorld __instance,Humanoid human,bool hold,ref bool __result)
            {
                if(!PlacedPortal(__instance) || human!=Player.m_localPlayer)return true;
                __result=false;if(hold)return false;
                var zdo=__instance.GetComponent<ZNetView>().GetZDO();
                var entry=Network.Find(zdo.m_uid);
                AtlasRenameDialog.Open(new PortalNameReceiver(zdo.m_uid,entry==null?__instance.GetText():entry.name),"Name portal");
                __result=true;return false;
            }
        }
        private sealed class PortalNameReceiver:TextReceiver
        {
            private readonly ZDOID uid;private readonly string name;
            internal PortalNameReceiver(ZDOID uid,string name){this.uid=uid;this.name=name;}
            public string GetText(){return name;}
            public void SetText(string text){Network.RenameAtPortal(uid,text);}
        }
        [HarmonyPatch(typeof(TeleportWorld),nameof(TeleportWorld.GetHoverText))]
        private static class PortalHover
        {
            private static void Postfix(TeleportWorld __instance,ref string __result)
            {
                if(!PlacedPortal(__instance))return;
                var zdo=__instance.GetComponent<ZNetView>().GetZDO();var entry=Network.Find(zdo.m_uid);
                var name=entry==null?AtlasWorldData.Clean(__instance.GetText()):entry.name;
                __result=Localization.instance.Localize((name.Length>0?name:"Portal")+"\n[<color=yellow><b>$KEY_Use</b></color>] Rename\nEnter to choose a destination");
            }
        }

        [HarmonyPatch(typeof(Humanoid),nameof(Humanoid.UseItem))]
        private static class UsePatch
        {
            private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (!IsAtlas(item) || __instance != Player.m_localPlayer) return true;
                UI.Open(); return false;
            }
        }
        [HarmonyPatch(typeof(Trader),nameof(Trader.GetAvailableItems))]
        private static class TraderPatch
        {
            private static void Postfix(Trader __instance, ref List<Trader.TradeItem> __result)
            {
                if (!AtlasDrop || __instance.m_name != "$npc_haldor") return;
                if (__result.Any(i => i.m_prefab == AtlasDrop)) return;
                // Copy the result: never append to a trader's own persistent stock list.
                __result = new List<Trader.TradeItem>(__result) { AtlasTrade.Create(AtlasDrop,Price.Value) };
            }
        }
        [HarmonyPatch(typeof(Player),nameof(Player.HaveRequirements),new Type[] {typeof(Piece),typeof(Player.RequirementMode)})]
        private static class BuildGate
        {
            private static void Postfix(Player __instance,Piece piece,ref bool __result)
            {
                if (piece && piece.gameObject.name == Pillar && !Unlocked(__instance)) __result=false;
            }
        }
        [HarmonyPatch(typeof(PieceTable),nameof(PieceTable.UpdateAvailable))]
        private static class PieceVisibility
        {
            private static readonly FieldInfo Categories=AccessTools.Field(typeof(PieceTable),"m_availablePiecesByCategory");
            private static void Postfix(PieceTable __instance,Player player)
            {
                if (Unlocked(player)) return;
                __instance.m_availablePieces.RemoveWhere(p=>p && p.name==Pillar);
                var categories=Categories.GetValue(__instance) as List<List<Piece>>;
                if(categories!=null) foreach(var category in categories) category.RemoveAll(p=>p && p.name==Pillar);
            }
        }
        [HarmonyPatch(typeof(Minimap),"UpdateMap")]
        private static class MapPanelInput
        {
            private static void Prefix(ref bool takeInput) { if(UI!=null && UI.PointerOverPanel) takeInput=false; }
        }
        [HarmonyPatch(typeof(Minimap),"InTextInput")]
        private static class MapSearchInput
        {
            private static void Postfix(ref bool __result){if(UI!=null && UI.SearchFocused)__result=true;}
        }
        [HarmonyPatch(typeof(ZDO),nameof(ZDO.SetPrefab))]
        private static class TrackPillars
        {
            private static void Postfix(ZDO __instance){Network?.Track(__instance);}
        }
        [HarmonyPatch(typeof(Minimap),nameof(Minimap.OnMapLeftClick))]
        private static class MarkerClick
        {
            private static bool Prefix() { return UI==null || !UI.PointerOverAtlas; }
        }
        [HarmonyPatch(typeof(Minimap),nameof(Minimap.OnMapDblClick))]
        private static class MarkerDoubleClick
        {
            private static bool Prefix() { return UI==null || !UI.PointerOverAtlas; }
        }
        [HarmonyPatch(typeof(Minimap),"RemovePinUnderPointer")]
        private static class MarkerRightClick
        {
            private static bool Prefix() { return UI==null || !UI.PointerOverAtlas; }
        }
        [HarmonyPatch(typeof(Minimap),nameof(Minimap.RemovePin),new Type[]{typeof(Minimap.PinData)})]
        private static class PreservePillarPin
        {
            private static bool Prefix(Minimap.PinData pin)
            {
                return PillarPins==null || PillarPins.Removing || !PillarPins.Owns(pin);
            }
        }
    }

    internal static class AtlasTrade
    {
        internal const string ItemName="Portal Atlas Map";
        internal static Trader.TradeItem Create(ItemDrop prefab,int price)
        {
            // Unity-serialized vanilla entries contain empty strings; newly constructed
            // entries do not. StoreGui reads m_tooltip.Length without a null check.
            return new Trader.TradeItem
            {
                m_prefab=prefab,m_stack=1,m_price=price,m_name=ItemName,
                m_tooltip="Carry this enchanted map and use it from your inventory or hotbar to travel to any portal or Atlas pillar. Also unlocks Atlas pillar construction.",
                m_requiredGlobalKey="",m_buyKey="",m_incrementKey="",m_buyPlayerEffects=new EffectList()
            };
        }
    }

    internal static class Icons
    {
        private static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        private static bool nativeStyleReady,nativeStyleFailed;
        internal static void MatchNativePortal()
        {
            if(nativeStyleReady || nativeStyleFailed || !Minimap.instance || sprites.Count==0)return;
            var portal=Minimap.instance.m_icons.FirstOrDefault(icon=>icon.m_name==Minimap.PinType.Icon4).m_icon;
            if(!portal)return;
            try
            {
                var rect=portal.textureRect;int width=(int)rect.width,height=(int)rect.height;
                var previous=RenderTexture.active;var buffer=RenderTexture.GetTemporary(portal.texture.width,portal.texture.height,0);
                var source=new Texture2D(width,height,TextureFormat.RGBA32,false);
                try{Graphics.Blit(portal.texture,buffer);RenderTexture.active=buffer;source.ReadPixels(rect,0,0);source.Apply();}
                finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(buffer);}
                var original=sprites["pillar-pin"];var glyph=original.texture;
                var pixels=NativePinStyle.Build(source.GetPixels32(),width,height,glyph.GetPixels32(),glyph.width,glyph.height,64);
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="PortalAtlas_native_style_pillar_pin",wrapMode=TextureWrapMode.Clamp,filterMode=portal.texture.filterMode};
                texture.SetPixels32(pixels);texture.Apply();
                sprites["pillar-pin"]=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),original.pixelsPerUnit*64/glyph.width);
                UnityEngine.Object.Destroy(source);nativeStyleReady=true;
                Debug.Log("[Portal Atlas] Pillar pin matched to native portal fill and shadow.");
            }
            catch(Exception error){nativeStyleFailed=true;Debug.LogWarning("[Portal Atlas] Native pin styling unavailable; keeping the fallback sprite: "+error.Message);}
        }
        internal static void Load()
        {
            if(sprites.Count>0)return;
            foreach(var name in new[]{"atlas","pillar","pillar-pin","star-on","star-off","rename","search","travel","close"})
            {
                using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("PortalAtlas."+name+".png"))
                using(var memory=new MemoryStream())
                {
                    if(stream==null)throw new InvalidDataException("Missing icon: "+name);
                    stream.CopyTo(memory);
                    var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    if(!ImageConversion.LoadImage(texture,memory.ToArray()))throw new InvalidDataException(name);
                    texture.name="PortalAtlas_"+name; texture.wrapMode=TextureWrapMode.Clamp;
                    if(name=="pillar-pin")texture.filterMode=FilterMode.Bilinear;
                    sprites[name]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
                }
            }
        }
        internal static Sprite Get(string name) { Load(); return sprites[name]; }
    }

    public sealed class AtlasPillar : MonoBehaviour,Hoverable,Interactable,TextReceiver
    {
        private ZNetView view;
        private void Awake()
        {
            view=GetComponent<ZNetView>();
            if(!view || !view.IsValid())return;
            view.Register<string,int,string>("OCT_AtlasLabel",ApplyName);
        }
        private IEnumerator Start()
        {
            for(int i=0;i<3;i++)
            {
                yield return new WaitForSeconds(2);
                if(!view || !view.IsValid())yield break;
                if(view.IsOwner())Plugin.Network.RegisterPillar(view.GetZDO());
            }
        }
        private void LateUpdate()
        {
            if(Vector3.Dot(transform.up,Vector3.up)>.99999f)return;
            var upright=Quaternion.Euler(0,transform.eulerAngles.y,0);
            transform.rotation=upright;
            if(view && view.IsValid() && view.IsOwner())view.GetZDO().SetRotation(upright);
        }
        private void ApplyName(long sender,string biome,int number,string label)
        {
            if(!view || !view.IsValid() || !view.IsOwner() || sender!=AtlasNetwork.ServerPeer)return;
            var zdo=view.GetZDO(); zdo.Set("oct_atlas_biome",biome);zdo.Set("oct_atlas_number",number);zdo.Set("oct_atlas_label",AtlasWorldData.Clean(label));
        }
        public string GetHoverName() { return GetText(); }
        public string GetHoverText() { return Localization.instance.Localize(GetText()+"\n[<color=yellow><b>$KEY_Use</b></color>] Rename pillar"); }
        public float GetHoverOffset() { return 0; }
        public string GetText()
        {
            if(!view || !view.IsValid())return "Atlas pillar";
            var zdo=view.GetZDO();
            var label=zdo.GetString("oct_atlas_label","");
            var number=zdo.GetInt("oct_atlas_number",0);
            // The replicated pillar state is fresher than a previously opened map catalogue.
            if(label.Length>0 || number>0)return label.Length>0?label:AtlasWorldData.DefaultName(zdo.GetString("oct_atlas_biome","Pillar"),number);
            var entry=Plugin.Network.Find(zdo.m_uid);
            return entry!=null?entry.name:"Atlas pillar";
        }
        public bool Interact(Humanoid user,bool hold,bool alt)
        {
            if(hold)return false;
            if(!Plugin.HasAtlas(user as Player)){Plugin.Notice("Carry a Portal Atlas to rename this pillar.");return true;}
            if(view && view.IsValid())AtlasRenameDialog.Open(this,"Name Atlas pillar");
            return true;
        }
        public bool UseItem(Humanoid user,ItemDrop.ItemData item) { return false; }
        public void SetText(string text)
        {
            if(view && view.IsValid()) Plugin.Network.Rename(view.GetZDO().m_uid,text);
        }
    }
}
