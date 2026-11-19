using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using TMPro;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberJemini.PortalAtlas
{
    internal sealed class AtlasUi
    {
        private GameObject panel,markerLayer;
        private RectTransform content;
        private InputField search;
        private Text count;
        private string selected="",hovered="",favouritesKey="";
        private string favouritesIdentity="",favouritesPath;
        private readonly HashSet<string> favourites=new HashSet<string>();
        private readonly Dictionary<string,MapMarker> markers=new Dictionary<string,MapMarker>();
        private int revision=-1;
        private bool active,rebuild;
        private float nextPinBinding;
        private static readonly FieldInfo NativePins=typeof(Minimap).GetField("m_pins",BindingFlags.Instance|BindingFlags.NonPublic);
        private static readonly FieldInfo NativeNamePrefab=typeof(Minimap).GetField("m_pinNamePrefab",BindingFlags.Instance|BindingFlags.NonPublic);
        private readonly Dictionary<Graphic,bool> mutedGraphics=new Dictionary<Graphic,bool>();
        internal ZDOID Source {get;private set;}=ZDOID.None;
        internal float SuppressPortalUntil;
        internal bool Active {get {return active;}}
        internal bool SearchFocused {get{return active && search && search.isFocused;}}
        private static Color Gold {get{return new Color(1,.73f,.31f);}}
        internal bool PointerOverPanel
        {
            get {return active && panel && (Over((RectTransform)panel.transform) || (search && search.isFocused));}
        }
        internal bool PointerOverAtlas
        {
            get {return active && (PointerOverPanel || markers.Values.Any(m=>m.root && m.root.activeInHierarchy && (Over(m.rect) || Over(m.caption.rectTransform))));}
        }
        private static bool Over(RectTransform rect) {return RectTransformUtility.RectangleContainsScreenPoint(rect,Input.mousePosition,null);}

        internal void Open()
        {
            Open(ZDOID.None);
        }
        internal void Open(ZDOID source)
        {
            var player=Player.m_localPlayer;
            var carried=Plugin.HasAtlas(player);
            if(!carried && (!player || !AtlasNetwork.IsNearPortal(source,player.transform.position)))
            {Plugin.Notice("Carry a Portal Atlas or enter a portal to choose a destination.");return;}
            if(player.IsDead() || player.IsTeleporting() || player.InCutscene())return;
            if(!Minimap.instance || GUIManager.CustomGUIFront==null)return;
            if(carried)Plugin.Unlock(player);LoadFavourites();Source=source;
            if(InventoryGui.instance)InventoryGui.instance.Hide();
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            if(Minimap.instance.m_mode!=Minimap.MapMode.Large){Plugin.Notice("Maps are disabled in this world.");return;}
            if(!panel)CreatePanel();
            panel.SetActive(true);active=true;rebuild=true;
            Plugin.Network.RequestList(true);
        }
        internal void Close()
        {
            active=false;hovered="";Source=ZDOID.None;
            RestoreNativeGraphics();if(markerLayer)markerLayer.SetActive(false);
            if(panel)panel.SetActive(false);
            if(Minimap.instance && Minimap.instance.m_mode==Minimap.MapMode.Large)
                Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        }
        internal void Tick()
        {
            var map=Minimap.instance;
            var player=Player.m_localPlayer;
            if(!map || !player)
            {Dispose();return;}
            var carried=Plugin.HasAtlas(player);
            var access=carried || (active && AtlasNetwork.IsNearPortal(Source,player.transform.position));
            if(active && (!access || map.m_mode!=Minimap.MapMode.Large || player.IsDead() || player.IsTeleporting()))
            {active=false;hovered="";Source=ZDOID.None;if(panel)panel.SetActive(false);}
            if(active && map.m_mode==Minimap.MapMode.Large && access)Plugin.Network.RequestList();
            if(revision!=Plugin.Network.Revision)
            {revision=Plugin.Network.Revision;rebuild=true;SyncMarkers();}
            if(active && rebuild){rebuild=false;BuildRows();}
        }
        private void LoadFavourites()
        {
            var uid=ZNet.instance.GetWorldUID();
            var identity=Player.m_localPlayer.GetPlayerID()+"-"+uid;
            if(identity==favouritesIdentity)return;
            favouritesIdentity=identity;favouritesKey="OCT_Atlas_Favourites_v2_"+uid;favourites.Clear();selected="";
            favouritesPath=Path.Combine(BepInEx.Paths.ConfigPath,Plugin.Id,"client-favourites",identity+".v2.txt");
            HashSet<string> stored=null;
            try{stored=FavouriteStore.Read(favouritesPath);}catch(Exception ex){Plugin.Warn("Could not read favourite backup: "+ex.Message);}
            if(stored!=null)favourites.UnionWith(stored);
            else
            {
                string saved;
                if(Player.m_localPlayer.m_customData.TryGetValue(favouritesKey,out saved))foreach(var id in saved.Split(new[]{','}))if(id.Length>0)favourites.Add(id);
                SaveFavourites();
                if(favourites.Count==0 && File.Exists(Path.Combine(BepInEx.Paths.ConfigPath,Plugin.Id,"client-favourites",identity+".txt")))
                    Plugin.Notice("Favourite storage fixed for world reloads. Please star your destinations again; the previous list is preserved.");
            }
        }
        private static string FavouriteKey(Destination entry)
        {
            return FavouriteStore.Key(entry.pillar,entry.position.x,entry.position.y,entry.position.z);
        }
        private void ToggleFavourite(string id)
        {
            if(!favourites.Remove(id))favourites.Add(id);
            SaveFavourites();rebuild=true;
        }
        private void SaveFavourites()
        {
            Player.m_localPlayer.m_customData[favouritesKey]=String.Join(",",favourites.OrderBy(x=>x));
            try{FavouriteStore.Save(favouritesPath,favourites);}
            catch(Exception ex){Plugin.Warn("Could not save favourite backup: "+ex.Message);Plugin.Notice("Favourite changed, but its immediate backup could not be saved.");}
        }
        private static RectTransform Rect(GameObject go,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        private static GameObject Child(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go;
        }
        private static Text Label(string text,Transform parent,int size,TextAnchor alignment)
        {
            var go=Child("Label",parent);var label=go.AddComponent<Text>();
            label.font=GUIManager.Instance.AveriaSerifBold;label.fontSize=size;label.color=Gold;label.text=text;
            label.alignment=alignment;label.supportRichText=false;label.raycastTarget=false;
            return label;
        }
        private static Button Button(string text,Transform parent,Action action)
        {
            var go=GUIManager.Instance.CreateButton(text,parent,Vector2.zero,Vector2.zero,Vector2.zero,10,10);
            var button=go.GetComponent<Button>();button.onClick.AddListener(()=>action());
            var label=go.GetComponentInChildren<Text>();if(label){label.fontSize=17;label.supportRichText=false;}
            return button;
        }
        private static Image Icon(string name,Transform parent,float size)
        {
            var go=Child(name,parent);var image=go.AddComponent<Image>();image.sprite=Icons.Get(name);image.preserveAspect=true;image.raycastTarget=false;
            Rect(go,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(size,size));return image;
        }
        private void CreatePanel()
        {
            var gui=GUIManager.Instance;
            panel=gui.CreateWoodpanel(mapParent(),Vector2.zero,Vector2.zero,Vector2.zero,0,0,false);
            panel.name="OCT_PortalAtlas_Panel";
            Rect(panel,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(28,0),new Vector2(440,620));
            var title=Label("PORTAL ATLAS",panel.transform,32,TextAnchor.MiddleCenter);
            title.font=gui.NorseBold;
            Rect(title.gameObject,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-10,-16),new Vector2(325,44));
            var close=Button("",panel.transform,Close);
            Rect(close.gameObject,new Vector2(1,1),new Vector2(1,1),new Vector2(-15,-18),new Vector2(32,32));Icon("close",close.transform,18);
            var input=gui.CreateInputField(panel.transform,Vector2.zero,Vector2.zero,Vector2.zero,InputField.ContentType.Standard,"Search by name or biome",18,400,38);
            input.name="AtlasSearch";Rect(input,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-72),new Vector2(400,38));
            search=input.GetComponent<InputField>();
            search.characterLimit=48;search.lineType=InputField.LineType.SingleLine;
            search.customCaretColor=true;search.caretColor=new Color(1,.95f,.86f);search.caretWidth=2;
            search.selectionColor=new Color(.7f,.45f,.15f,.5f);
            var searchText=search.textComponent;
            searchText.font=gui.AveriaSerifBold;searchText.fontSize=18;searchText.color=new Color(1,.95f,.86f);
            searchText.supportRichText=false;searchText.alignment=TextAnchor.MiddleLeft;searchText.horizontalOverflow=HorizontalWrapMode.Overflow;
            searchText.rectTransform.anchorMin=Vector2.zero;searchText.rectTransform.anchorMax=Vector2.one;
            searchText.rectTransform.offsetMin=new Vector2(12,5);searchText.rectTransform.offsetMax=new Vector2(-40,-5);
            var placeholder=search.placeholder as Text;
            if(placeholder)
            {
                placeholder.font=gui.AveriaSerif;placeholder.fontSize=18;placeholder.color=new Color(.8f,.77f,.7f,.8f);placeholder.alignment=TextAnchor.MiddleLeft;
                placeholder.rectTransform.anchorMin=Vector2.zero;placeholder.rectTransform.anchorMax=Vector2.one;
                placeholder.rectTransform.offsetMin=new Vector2(12,5);placeholder.rectTransform.offsetMax=new Vector2(-40,-5);
            }
            search.onValueChanged.AddListener(value=>rebuild=true);
            var searchIcon=Icon("search",input.transform,18);searchIcon.rectTransform.anchorMin=searchIcon.rectTransform.anchorMax=new Vector2(1,.5f);searchIcon.rectTransform.anchoredPosition=new Vector2(-15,0);
            var scrollRoot=Child("AtlasScroll",panel.transform);
            Rect(scrollRoot,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-124),new Vector2(400,432));
            var scroll=scrollRoot.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=480;
            var viewport=Child("AtlasDestinations",scrollRoot.transform);
            var viewRect=Rect(viewport,new Vector2(0,1),new Vector2(0,1),Vector2.zero,new Vector2(372,432));
            var background=viewport.AddComponent<Image>();background.color=new Color(0,0,0,.18f);
            viewport.AddComponent<RectMask2D>();
            content=(RectTransform)Child("Content",viewport.transform).transform;
            content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=5;layout.padding=new RectOffset(0,0,2,2);
            layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=content;scroll.viewport=viewRect;
            var barObject=Child("VerticalScrollbar",scrollRoot.transform);
            Rect(barObject,new Vector2(1,.5f),new Vector2(1,.5f),Vector2.zero,new Vector2(18,432));
            var track=barObject.AddComponent<Image>();track.color=new Color(.1f,.085f,.06f,.95f);
            var bar=barObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;
            var area=(RectTransform)Child("SlidingArea",barObject.transform).transform;
            area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=new Vector2(2,2);area.offsetMax=new Vector2(-2,-2);
            var handle=(RectTransform)Child("Handle",area).transform;handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
            var thumb=handle.gameObject.AddComponent<Image>();thumb.color=Gold;
            bar.handleRect=handle;bar.targetGraphic=thumb;
            scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
            count=Label("Loading destinations...",panel.transform,15,TextAnchor.MiddleLeft);
            Rect(count.gameObject,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,24),new Vector2(400,24));
        }
        private static Transform mapParent(){return Minimap.instance.m_largeRoot.transform;}
        private void BuildRows()
        {
            if(!content)return;
            foreach(Transform child in content){child.gameObject.SetActive(false);UnityEngine.Object.Destroy(child.gameObject);}
            var query=search?search.text:"";
            var destinations=Plugin.Network.Destinations.Where(d=>AtlasWorldData.Search(d.name,d.biome,query)).OrderByDescending(d=>favourites.Contains(FavouriteKey(d))).ThenBy(d=>d.name,StringComparer.OrdinalIgnoreCase).ThenBy(d=>d.id).ToList();
            count.text=destinations.Count+" / "+Plugin.Network.Destinations.Count+" destinations  •  click to travel";
            foreach(var destination in destinations)
            {
                var entry=destination;
                var row=Child("Destination_"+entry.id,content);
                row.AddComponent<LayoutElement>().preferredHeight=36;
                var layout=row.AddComponent<HorizontalLayoutGroup>();layout.spacing=5;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=false;layout.childForceExpandHeight=true;
                var star=Button("",row.transform,()=>ToggleFavourite(FavouriteKey(entry)));star.gameObject.AddComponent<LayoutElement>().preferredWidth=34;
                Icon(favourites.Contains(FavouriteKey(entry))?"star-on":"star-off",star.transform,26);
                var name=Button(AtlasWorldData.WithBiome(entry.name,entry.biome),row.transform,()=>TravelTo(entry.id));
                name.gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
                var text=name.GetComponentInChildren<Text>();if(text){text.fontSize=17;text.alignment=TextAnchor.MiddleLeft;text.rectTransform.offsetMin=new Vector2(12,text.rectTransform.offsetMin.y);text.rectTransform.offsetMax=new Vector2(-10,text.rectTransform.offsetMax.y);text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;text.resizeTextForBestFit=true;text.resizeTextMinSize=10;text.resizeTextMaxSize=17;}
                name.gameObject.AddComponent<RectMask2D>();
                if(entry.id==selected){var colors=name.colors;colors.normalColor=new Color(1,.8f,.44f);name.colors=colors;}
                var rename=Button("",row.transform,()=>Rename(entry));rename.gameObject.AddComponent<LayoutElement>().preferredWidth=30;Icon("rename",rename.transform,22);
                var hover=row.AddComponent<AtlasHover>();hover.enter=()=>hovered=entry.id;hover.exit=()=>{if(hovered==entry.id)hovered="";};
            }
            if(destinations.Count==0)
            {
                var empty=Label(Plugin.Network.Destinations.Count==0?"No destinations yet. Build a pillar or a portal.\nThe world catalogue may take a moment to load.":"No matching destinations.",content,18,TextAnchor.MiddleCenter);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight=100;
            }
            if(!Plugin.Network.Destinations.Any(d=>d.id==selected))selected="";
        }
        private void TravelTo(string id)
        {
            // Ordinary map clicks must never activate Atlas or initiate travel.
            if(!active)return;
            selected=id;
            var entry=Plugin.Network.Destinations.FirstOrDefault(d=>d.id==id);
            if(entry!=null)Plugin.Network.Travel(entry);
        }
        private void Rename(Destination entry)
        {
            AtlasRenameDialog.Open(new AtlasRename(entry),entry.pillar?"Name Atlas pillar":"Atlas label (portal link stays unchanged)");
        }
        private sealed class AtlasRename:TextReceiver
        {
            private readonly Destination entry;
            internal AtlasRename(Destination entry){this.entry=entry;}
            public string GetText(){return entry.name;}
            public void SetText(string text){Plugin.Network.Rename(entry.Uid,text);}
        }
        private sealed class MapMarker
        {
            internal GameObject root;
            internal RectTransform rect;
            internal Image image;
            internal Destination entry;
            internal TMP_Text caption;
            internal readonly List<Minimap.PinData> pins=new List<Minimap.PinData>();
        }
        private void SyncMarkers()
        {
            var map=Minimap.instance;if(!map)return;
            EnsureMarkerLayer(map);
            var current=new HashSet<string>(Plugin.Network.Destinations.Select(d=>d.id));
            foreach(var id in markers.Keys.Where(id=>!current.Contains(id)).ToArray())
            {if(markers[id].root)UnityEngine.Object.Destroy(markers[id].root);markers.Remove(id);}
            foreach(var entry in Plugin.Network.Destinations)
            {
                MapMarker marker;
                if(!markers.TryGetValue(entry.id,out marker) || !marker.root)
                {
                    var go=Child("OCT_AtlasMarker_"+entry.id,markerLayer.transform);
                    var rect=Rect(go,Vector2.zero,new Vector2(.5f,.5f),Vector2.zero,new Vector2(28,28));
                    var image=go.AddComponent<Image>();image.preserveAspect=true;
                    var button=go.AddComponent<Button>();button.targetGraphic=image;
                    var id=entry.id;button.onClick.AddListener(()=>TravelTo(id));
                    // Clone the game's own label prefab: font, material, colour and
                    // outline are the exact native pin style, rather than a UI font.
                    var nameObject=UnityEngine.Object.Instantiate((GameObject)NativeNamePrefab.GetValue(map),go.transform,false);
                    nameObject.name="AtlasNativePinName";nameObject.SetActive(true);
                    var caption=nameObject.GetComponentInChildren<TMP_Text>(true);
                    caption.richText=false;caption.raycastTarget=true;caption.textWrappingMode=TextWrappingModes.NoWrap;caption.color=Color.white;
                    var nameRect=(RectTransform)nameObject.transform;
                    nameRect.anchorMin=nameRect.anchorMax=new Vector2(.5f,.5f);nameRect.anchoredPosition=Vector2.zero;
                    var hover=go.AddComponent<AtlasHover>();hover.enter=()=>hovered=id;hover.exit=()=>{if(hovered==id)hovered="";};
                    marker=new MapMarker{root=go,rect=rect,image=image,caption=caption};markers[entry.id]=marker;
                }
                marker.entry=entry;
                marker.caption.text=AtlasWorldData.WithBiome(entry.name,entry.biome);
            }
            nextPinBinding=0;
        }
        private void EnsureMarkerLayer(Minimap map)
        {
            if(markerLayer)return;
            markerLayer=Child("OCT_AtlasInteractivePins",map.m_largeRoot.transform);
            var rect=(RectTransform)markerLayer.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            markerLayer.AddComponent<RectMask2D>();
            markerLayer.SetActive(false);
        }
        // Use existing portal-pin records and their native artwork wherever available.
        // Only the Atlas presentation is moved to the top layer: the original record,
        // saved label, filters and owning mod remain unchanged.
        private void BindNativePins(Minimap map)
        {
            foreach(var marker in markers.Values)marker.pins.Clear();
            var pins=NativePins.GetValue(map) as List<Minimap.PinData>;
            if(pins==null)return;
            foreach(var pin in pins)
            {
                if(pin.m_type!=Minimap.PinType.Icon4)continue;
                MapMarker nearest=null;float distance=4;
                foreach(var marker in markers.Values)
                {
                    if(marker.entry.pillar!=Plugin.PillarPins.Owns(pin))continue;
                    float delta=(marker.entry.position-pin.m_pos).sqrMagnitude;
                    if(delta<distance){distance=delta;nearest=marker;}
                }
                if(nearest!=null)nearest.pins.Add(pin);
            }
        }
        private void Mute(Graphic graphic)
        {
            if(!graphic)return;
            if(!mutedGraphics.ContainsKey(graphic))mutedGraphics[graphic]=graphic.enabled;
            graphic.enabled=false;
        }
        private void RestoreNativeGraphics()
        {
            foreach(var pair in mutedGraphics)if(pair.Key)pair.Key.enabled=pair.Value;
            mutedGraphics.Clear();
        }
        internal void RenderAboveMap()
        {
            RestoreNativeGraphics();
            var map=Minimap.instance;var player=Player.m_localPlayer;
            if(!map || !player)return;
            var access=active && map.m_mode==Minimap.MapMode.Large && (Plugin.HasAtlas(player) || AtlasNetwork.IsNearPortal(Source,player.transform.position));
            if(markerLayer)markerLayer.SetActive(access);
            if(!access)return;
            if(markerLayer)markerLayer.transform.SetAsLastSibling();
            if(panel)panel.transform.SetAsLastSibling();
            UpdateMarkers(map,access);
        }
        private void UpdateMarkers(Minimap map,bool carried)
        {
            if(carried && map.m_mode==Minimap.MapMode.Large && markers.Count!=Plugin.Network.Destinations.Count)SyncMarkers();
            if(carried && map.m_mode==Minimap.MapMode.Large && Time.unscaledTime>=nextPinBinding)
            {BindNativePins(map);nextPinBinding=Time.unscaledTime+.5f;}
            var image=map.m_mapImageLarge;var uv=image.uvRect;var rect=image.rectTransform.rect;
            if(markerLayer)
            {
                var layer=(RectTransform)markerLayer.transform;
                // Match the actual map image, including its pivot, so the mask and
                // marker positions remain aligned when the screen/UI scale changes.
                layer.pivot=image.rectTransform.pivot;layer.sizeDelta=rect.size;
                layer.position=image.rectTransform.position;layer.rotation=image.rectTransform.rotation;
                var scale=image.rectTransform.lossyScale;var parentScale=layer.parent.lossyScale;
                layer.localScale=new Vector3(scale.x/parentScale.x,scale.y/parentScale.y,scale.z/parentScale.z);
                markerLayer.SetActive(true);
            }
            foreach(var marker in markers.Values)
            {
                if(!marker.root)continue;
                var p=marker.entry.position;
                var x=(p.x/map.m_pixelSize+map.m_textureSize/2)/(float)map.m_textureSize;
                var y=(p.z/map.m_pixelSize+map.m_textureSize/2)/(float)map.m_textureSize;
                bool visible=carried && map.m_mode==Minimap.MapMode.Large && x>uv.xMin && x<uv.xMax && y>uv.yMin && y<uv.yMax;
                marker.root.SetActive(visible);if(!visible)continue;
                var point=MapProjection.ToLocal(p.x,p.z,map.m_pixelSize,map.m_textureSize,uv,rect);
                marker.rect.position=image.rectTransform.TransformPoint(new Vector3(point.x,point.y,0));
                var pulse=(active && (hovered==marker.entry.id || selected==marker.entry.id))?1.1f+.2f*Mathf.Sin(Time.unscaledTime*6):1;
                marker.rect.localScale=Vector3.one*pulse;
                var nativePortal=map.m_icons.FirstOrDefault(icon=>icon.m_name==Minimap.PinType.Icon4).m_icon;
                if(!marker.entry.pillar && !nativePortal){marker.root.SetActive(false);continue;}
                marker.image.sprite=marker.entry.pillar?Icons.Get("pillar-pin"):nativePortal;
                // Muted native-style red; retain the sprite's feathered alpha and shadow.
                marker.image.color=new Color(.92f,.32f,.30f,.94f);
                foreach(var pin in marker.pins)
                {
                    Mute(pin.m_iconElement);
                    if(pin.m_NamePinData!=null && pin.m_NamePinData.PinNameGameObject)
                        foreach(var graphic in pin.m_NamePinData.PinNameGameObject.GetComponentsInChildren<Graphic>(true))Mute(graphic);
                    if(pin.m_checkedElement)foreach(var graphic in pin.m_checkedElement.GetComponentsInChildren<Graphic>(true))Mute(graphic);
                }
            }
        }
        internal void Dispose()
        {
            RestoreNativeGraphics();
            if(panel)UnityEngine.Object.Destroy(panel);panel=null;content=null;search=null;active=false;Source=ZDOID.None;selected=hovered=favouritesKey="";revision=-1;
            favouritesIdentity="";favouritesPath=null;
            foreach(var marker in markers.Values)if(marker.root)UnityEngine.Object.Destroy(marker.root);
            markers.Clear();favourites.Clear();
            if(markerLayer)UnityEngine.Object.Destroy(markerLayer);markerLayer=null;
        }
    }
    public sealed class AtlasHover:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        internal Action enter,exit;
        public void OnPointerEnter(PointerEventData data){enter?.Invoke();}
        public void OnPointerExit(PointerEventData data){exit?.Invoke();}
        private void OnDisable(){exit?.Invoke();}
    }
}
