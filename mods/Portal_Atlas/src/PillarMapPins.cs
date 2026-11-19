using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OctoberJemini.PortalAtlas
{
    internal sealed class PillarMapPins
    {
        private Minimap map;
        private long world;
        private float nextTick,nextPoll;
        private readonly Dictionary<string,Minimap.PinData> pins=new Dictionary<string,Minimap.PinData>();
        private static readonly MethodInfo IsExplored=typeof(Minimap).GetMethod("IsExplored",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(Vector3)},null);
        internal bool Removing {get;private set;}
        internal bool Owns(Minimap.PinData pin){return pins.Values.Contains(pin);}
        internal void Tick()
        {
            if(Time.unscaledTime<nextTick)return;nextTick=Time.unscaledTime+.5f;
            var current=Minimap.instance;var player=Player.m_localPlayer;
            if(!current || !player || !ZNet.instance){Dispose();return;}
            var uid=ZNet.instance.GetWorldUID();
            if(map!=current || world!=uid){Dispose();map=current;world=uid;}
            if(Plugin.Unlocked(player) && Time.unscaledTime>=nextPoll)
            {nextPoll=Time.unscaledTime+5;Plugin.Network.RequestList();}
            var entries=Plugin.Network.Destinations.Where(d=>d.pillar).ToList();
            var ids=new HashSet<string>(entries.Select(d=>d.id));
            foreach(var id in pins.Keys.Where(id=>!ids.Contains(id)).ToArray())Remove(id);
            foreach(var entry in entries)
            {
                // The ordinary map gets a passive, native pin only for explored
                // pillars. The Atlas alone exposes distant undiscovered destinations.
                bool explored=(bool)IsExplored.Invoke(map,new object[]{entry.position});
                if(!explored && (player.transform.position-entry.position).sqrMagnitude>10000)continue;
                Minimap.PinData pin;
                if(pins.TryGetValue(entry.id,out pin) && pin.m_name!=entry.name){Remove(entry.id);pin=null;}
                if(pin==null)
                {
                    pin=map.AddPin(entry.position,Minimap.PinType.Icon4,entry.name,false,false);
                    pin.m_icon=Icons.Get("pillar-pin");pins[entry.id]=pin;
                }
                pin.m_icon=Icons.Get("pillar-pin");
                if(pin.m_iconElement){var image=pin.m_iconElement.GetComponent<UnityEngine.UI.Image>();if(image)image.sprite=pin.m_icon;}
                pin.m_pos=entry.position;
            }
        }
        private void Remove(string id)
        {
            var pin=pins[id];pins.Remove(id);
            if(!map)return;
            Removing=true;try{map.RemovePin(pin);}finally{Removing=false;}
        }
        internal void Dispose()
        {
            foreach(var id in pins.Keys.ToArray())Remove(id);
            map=null;world=0;nextPoll=0;
        }
    }
}
