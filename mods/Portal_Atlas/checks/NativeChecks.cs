using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

internal static class NativeChecks
{
    private static int count;
    private static void Check(bool condition,string message){count++;if(!condition)throw new Exception(message);}
    private static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
            var name=new AssemblyName(e.Name).Name+".dll";
            foreach(var root in new[]{Environment.GetEnvironmentVariable("ATLAS_GAME_MANAGED"),Environment.GetEnvironmentVariable("ATLAS_BEP_CORE"),Environment.GetEnvironmentVariable("ATLAS_JOTUNN"),Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".."))})
            {
                var file=Path.Combine(root,name);if(File.Exists(file))return Assembly.LoadFrom(file);
            }
            return null;
        };
        Run();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var mod=Assembly.LoadFrom(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","PortalAtlas.dll")));
        var network=mod.GetType("OctoberJemini.PortalAtlas.AtlasNetwork",true);
        var instance=Activator.CreateInstance(network,true);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        network.GetField("world",flags).SetValue(instance,123L);
        var receive=network.GetMethod("ReceiveCatalogue",flags);
        var revision=network.GetProperty("Revision",flags);
        var entries=(IList)network.GetField("Destinations",flags).GetValue(instance);
        var id=network.GetMethod("Id",BindingFlags.Static|BindingFlags.NonPublic);
        var parse=network.GetMethod("ParseId",BindingFlags.Static|BindingFlags.NonPublic);
        var expected=new ZDOID(-1234567890123L,4294967295U);
        var encoded=(string)id.Invoke(null,new object[]{expected});
        Check(((ZDOID)parse.Invoke(null,new object[]{encoded})).Equals(expected),"Full-range object ID roundtrip");
        Check(((ZDOID)parse.Invoke(null,new object[]{"bogus"})).Equals(ZDOID.None),"Invalid object IDs cannot select a destination");
        Action<ZPackage> accept=p=>receive.Invoke(instance,new object[]{new ZPackage(p.GetArray())});
        Func<int,ZPackage> catalogue=total=>{
            var p=new ZPackage();p.Write(total);
            for(int i=0;i<total;i++){p.Write("123:"+(i+1));p.Write("Meadows "+(i+1).ToString("D4"));p.Write("Meadows");p.Write(true);p.Write(new UnityEngine.Vector3(i*100,8,i*300));}
            return p;
        };
        accept(catalogue(2));Check(entries.Count==2,"Native ZPackage catalogue deserialization");
        var ui=mod.GetType("OctoberJemini.PortalAtlas.AtlasUi",true);
        var favouriteKey=ui.GetMethod("FavouriteKey",BindingFlags.Static|BindingFlags.NonPublic);
        var destination=entries[0];var destinationType=destination.GetType();
        var savedFavourite=(string)favouriteKey.Invoke(null,new[]{destination});
        destinationType.GetField("id",flags).SetValue(destination,"1:999999");
        destinationType.GetField("name",flags).SetValue(destination,"Renamed after restart");
        Check((string)favouriteKey.Invoke(null,new[]{destination})==savedFavourite,"Actual UI favourite key survives reassigned native IDs and renames");
        var replay=new ZPackage();replay.Write(1);replay.Write("987654:770");replay.Write("Renamed after restart");replay.Write("Meadows");replay.Write(true);replay.Write(new UnityEngine.Vector3(0,8,0));
        accept(replay);
        Check((string)favouriteKey.Invoke(null,new[]{entries[0]})==savedFavourite,"Freshly deserialized catalogue preserves favourite identity across world sessions");
        var other=new ZPackage();other.Write(1);other.Write("1:999999");other.Write("Other destination");other.Write("Meadows");other.Write(true);other.Write(new UnityEngine.Vector3(100,8,300));
        accept(other);
        Check((string)favouriteKey.Invoke(null,new[]{entries[0]})!=savedFavourite,"Reused network IDs do not transfer stars to a different destination");
        accept(catalogue(2));
        var version=(int)revision.GetValue(instance,null);
        accept(catalogue(2));Check((int)revision.GetValue(instance,null)==version,"Unchanged polling snapshot does not rebuild UI");
        accept(catalogue(1));Check(entries.Count==1,"Deleted object removed on next authoritative snapshot");
        Check((int)revision.GetValue(instance,null)>version,"Deletion triggers marker/list refresh");
        try {accept(catalogue(-1));throw new Exception("Negative catalogue count was accepted");}
        catch(TargetInvocationException ex){Check(ex.InnerException is InvalidDataException,"Malformed catalogue is rejected");}
        var tradeType=mod.GetType("OctoberJemini.PortalAtlas.AtlasTrade",true);
        var trade=(Trader.TradeItem)tradeType.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{null,1500});
        Check(trade.m_name=="Portal Atlas Map","Requested trader item name");
        Check(trade.m_price==1500 && trade.m_stack==1,"Atlas is sold singly for the configured price");
        Check(trade.m_tooltip!=null && trade.m_tooltip.Length>0,"Native shop's unchecked tooltip Length access is safe");
        Check(trade.m_requiredGlobalKey!=null && trade.m_buyKey!=null && trade.m_incrementKey!=null,"Trader string fields have serialized-equivalent defaults");
        var project=mod.GetType("OctoberJemini.PortalAtlas.MapProjection",true).GetMethod("ToLocal",BindingFlags.Static|BindingFlags.NonPublic);
        Action<float,float,float,UnityEngine.Rect,UnityEngine.Rect,float,float,string> projection=(x,z,pixelSize,uv,target,wantX,wantY,label)=>{
            var point=(UnityEngine.Vector2)project.Invoke(null,new object[]{x,z,pixelSize,2048,uv,target});
            Check(Math.Abs(point.x-wantX)<.003f && Math.Abs(point.y-wantY)<.003f,label);
        };
        var full=new UnityEngine.Rect(0,0,1,1);
        var centered=new UnityEngine.Rect(-500,-300,1000,600);
        projection(0,0,1,full,centered,0,0,"World centre maps to image centre with centred pivot");
        projection(-1024,-1024,1,full,centered,-500,-300,"Bottom-left includes negative rect origin");
        projection(1024,1024,1,full,centered,500,300,"Top-right respects rect maximum");
        projection(0,0,1,full,new UnityEngine.Rect(0,0,1000,600),500,300,"Bottom-left pivot maps correctly");
        projection(0,0,1,full,new UnityEngine.Rect(-250,-450,1000,600),250,-150,"Non-centred pivot maps correctly");
        projection(-256,256,1,new UnityEngine.Rect(.25f,.5f,.25f,.25f),centered,0,0,"Panned UV centre remains aligned");
        projection(102.4f,-102.4f,1,new UnityEngine.Rect(.4f,.4f,.2f,.2f),centered,250,-150,"Zoomed non-square map aligns both axes");
        projection(204.8f,-204.8f,2,new UnityEngine.Rect(.4f,.4f,.2f,.2f),centered,250,-150,"Metres-per-pixel scaling remains correct");
        projection(0,0,1,full,new UnityEngine.Rect(-1000,-600,2000,1200),0,0,"Larger UI rect retains centred placement");
        var model=mod.GetType("OctoberJemini.PortalAtlas.WaystoneModel",true);
        var read=model.GetMethod("Read",BindingFlags.Static|BindingFlags.NonPublic);
        Array parts;using(var stream=mod.GetManifestResourceStream("PortalAtlas.waystone.mesh"))parts=(Array)read.Invoke(null,new object[]{stream});
        Check(parts.Length==4,"Actual packaged waystone contains painted stone, carvings, orange insets and masonry meshes");
        var pointLists=new Dictionary<string,UnityEngine.Vector3[]>();
        var uvLists=new Dictionary<string,UnityEngine.Vector2[]>();
        foreach(var part in parts)
        {
            var type=part.GetType();string name=(string)type.GetField("name",flags).GetValue(part);
            var vertices=(UnityEngine.Vector3[])type.GetField("vertices",flags).GetValue(part);
            var triangles=(int[])type.GetField("triangles",flags).GetValue(part);
            var uv=(UnityEngine.Vector2[])type.GetField("uv",flags).GetValue(part);
            pointLists[name]=vertices;uvLists[name]=uv;
            Check(vertices.All(p=>!Single.IsNaN(p.x) && !Single.IsNaN(p.y) && !Single.IsNaN(p.z)),"Finite model vertices in "+name);
            Check(triangles.Length%3==0 && triangles.All(i=>i>=0 && i<vertices.Length),"Valid triangle indices in "+name);
            Check(uv.Length==vertices.Length && uv.All(p=>!Single.IsNaN(p.x) && !Single.IsNaN(p.y)),"Mapped native stone surface coordinates in "+name);
            Check(Enumerable.Range(0,triangles.Length/3).All(i=>UnityEngine.Vector3.Cross(vertices[triangles[i*3+1]]-vertices[triangles[i*3]],vertices[triangles[i*3+2]]-vertices[triangles[i*3]]).sqrMagnitude>1e-16f),"Non-degenerate geometry in "+name);
        }
        var all=pointLists.Values.SelectMany(p=>p).ToArray();
        Check(Math.Abs(all.Max(p=>p.y)-1.5f)<.0001f && Math.Abs(all.Min(p=>p.y))<.0001f,"Approved shoulder-height stone is 1.50 m tall with base on ground");
        Check(Math.Abs(all.Max(p=>p.x)-all.Min(p=>p.x)-.75f)<.0001f && Math.Abs(all.Max(p=>p.z)-all.Min(p=>p.z)-.75f)<.0001f,"Footprint is 0.75 m square, 25 percent thinner than the initial 1 m design");
        var orange=pointLists["orange"];
        for(int side=0;side<4;side++)
        {
            Func<UnityEngine.Vector3,bool> facing=p=>side==0?p.z>.20f:side==1?p.x>.20f:side==2?p.z<-.20f:p.x<-.20f;
            Check(orange.Any(p=>facing(p)&&p.y>.82f && p.y<1.04f),"Orange recessed windows exist on face "+side);
            Check(orange.Any(p=>facing(p)&&p.y>.10f && p.y<.26f),"Orange triangular base inset exists on face "+side);
        }
        var crownPosition=model.GetMethod("CrownPosition",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(float offset in new[]{-1f,-.008f,0f,.008f,1f})
        {
            var p=(UnityEngine.Vector3)crownPosition.Invoke(null,new object[]{offset});
            Check(p.x==0 && p.z==0 && Math.Abs(p.y-1.59f)<=.0201f,"Wisp stays horizontally centred with bounded vertical motion");
        }
        var colours=mod.GetType("OctoberJemini.PortalAtlas.WaystoneColours",true).GetMethod("StoneGrey",BindingFlags.Static|BindingFlags.NonPublic);
        Func<UnityEngine.Color32,UnityEngine.Color32> grey=p=>(UnityEngine.Color32)colours.Invoke(null,new object[]{p});
        var warm=grey(new UnityEngine.Color32(109,90,76,255));
        Check(warm.g>warm.b && warm.r-warm.b<15 && warm.g>100,"Painted stone takes the neutral native-stone grey palette");
        var fire=new UnityEngine.Color32(255,170,30,255);var kept=grey(fire);
        Check(kept.r==fire.r && kept.g==fire.g && kept.b==fire.b && kept.a==fire.a,"Orange inset colour is preserved byte-for-byte");
        var low=grey(new UnityEngine.Color32(60,48,38,255));var high=grey(new UnityEngine.Color32(140,118,98,255));
        Check(high.r>low.r && high.g>low.g && high.b>low.b,"Painted light/shadow detail remains ordered after colour correction");
        var foot=pointLists["masonry"].Where(p=>p.y<.071f).ToArray();
        Check(Math.Abs(foot.Max(p=>p.x)+foot.Min(p=>p.x))<.0001f && Math.Abs(foot.Max(p=>p.z)+foot.Min(p=>p.z))<.0001f,"Foot remains geometrically centred beneath the shaft");
        var bottomUV=Enumerable.Range(0,orange.Length).Where(i=>orange[i].y<.3f).ToArray();
        Check(bottomUV.Count(i=>orange[i].y>.2f)==4,"Each base aperture has one physical apex");
        Check(bottomUV.Where(i=>orange[i].y>.2f).All(i=>Math.Abs(uvLists["orange"][i].x-.3515625f)<.0001f && Math.Abs(uvLists["orange"][i].y-(1-1734/2048f))<.0001f),"Painted triangle apex is pinned directly to the actual apex on all four sides");
        var reach=(float)model.GetMethod("SparkMaximumReach",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        Check(reach>.375f && reach<.50f,"Spark flight can reach slightly past the pillar footprint before fading");
        var styler=mod.GetType("OctoberJemini.PortalAtlas.NativePinStyle",true).GetMethod("Build",BindingFlags.Static|BindingFlags.NonPublic);
        var nativeArt=new UnityEngine.Color32[32*32];var glyphArt=new UnityEngine.Color32[32*32];
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            int index=y*32+x;int dx=Math.Max(10-x,Math.Max(0,x-21)),dy=Math.Max(8-y,Math.Max(0,y-23));float distance=(float)Math.Sqrt(dx*dx+dy*dy);
            byte tone=(byte)(170+(index*17)%70);
            nativeArt[index]=distance==0?new UnityEngine.Color32(tone,tone,tone,255):new UnityEngine.Color32(0,0,0,(byte)Math.Max(0,240-distance*45));
            if(x>=12 && x<=19 && y>=5 && y<=26)glyphArt[index]=new UnityEngine.Color32(184,187,177,255);
        }
        var styled=(UnityEngine.Color32[])styler.Invoke(null,new object[]{nativeArt,32,32,glyphArt,32,32,32});
        Check(Enumerable.Range(12,8).SelectMany(x=>Enumerable.Range(6,20).Select(y=>styled[y*32+x].r)).Distinct().Count()>8,"Pillar fill inherits native artwork variation rather than a flat vector colour");
        Check(styled[15*32+10].a>100 && styled[15*32+10].r<60,"Pillar inherits a pronounced diffuse native dark border");
        Check(styled[0].a==0,"Native-style shadow remains transparent far from the silhouette");
        string pinPreview=Environment.GetEnvironmentVariable("ATLAS_PIN_PREVIEW");
        if(!String.IsNullOrEmpty(pinPreview))
        {
            Func<string,UnityEngine.Color32[]> loadPixels=name=>{
                var data=File.ReadAllBytes(Path.Combine(pinPreview,name));var values=new UnityEngine.Color32[data.Length/4];
                for(int i=0;i<values.Length;i++)values[i]=new UnityEngine.Color32(data[i*4],data[i*4+1],data[i*4+2],data[i*4+3]);return values;
            };
            var actual=(UnityEngine.Color32[])styler.Invoke(null,new object[]{loadPixels("native-portal-preview-only.rgba"),64,61,loadPixels("pillar-mask-preview-only.rgba"),256,256,64});
            File.WriteAllBytes(Path.Combine(pinPreview,"native-style-pillar-preview-only.rgba"),actual.SelectMany(p=>new[]{p.r,p.g,p.b,p.a}).ToArray());
        }
        bool rejected=false;try{using(var bad=new MemoryStream(new byte[]{0,1,2,3,4}))read.Invoke(null,new object[]{bad});}catch(TargetInvocationException){rejected=true;}
        Check(rejected,"Invalid mesh resource is rejected explicitly");
        System.Console.WriteLine("Passed "+count+" native serialization, projection and packaged waystone geometry checks.");
    }
}
