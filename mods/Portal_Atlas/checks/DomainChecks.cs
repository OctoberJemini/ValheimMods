using System;
using System.Collections.Generic;
using System.IO;
using OctoberJemini.PortalAtlas;

internal static class DomainChecks
{
    private static int count;
    private static void Check(bool condition,string message){count++;if(!condition)throw new Exception(message);}
    private static void Main()
    {
        var world=new AtlasWorldData();
        var a=world.Assign("a","Meadows",0,"");
        var b=world.Assign("b","Meadows",0,"");
        var forest=world.Assign("c","Black Forest",0,"");
        Check(a.Display=="Atlas-001","First Atlas name");
        Check(b.Display=="Atlas-002","Second Atlas name");
        Check(forest.Display=="Atlas-001","Each biome retains its own sequence");
        world.names.Remove(b);
        Check(world.Assign("d","Meadows",0,"").Display=="Atlas-003","Deleted destination numbers must not be reused");
        a.label="Home";
        Check(world.Assign("a","Meadows",0,"").Display=="Home","Rescans must retain manual names");
        Check(world.counters.Find(c=>c.biome=="Meadows").last==3,"Rescan must not increment counter");
        a.label="";Check(a.Display=="Atlas-001","Empty rename resets pillar to automatic name");
        world.Observe("Plains",82);
        Check(world.Assign("e","Plains",0,"").Display=="Atlas-083","Reconstruct maxima before assigning new pillars");
        Check(world.Assign("f","Mistlands",321,"Harbour").number==321,"Retain world ZDO sequence on recovery");
        Check(world.Assign("g","Mistlands",0,"").Display=="Atlas-322","Recovered sequence advances safely");
        var otherWorld=new AtlasWorldData();
        Check(otherWorld.Assign("a","Meadows",0,"").Display=="Atlas-001","World counters are independent");
        Check(AtlasWorldData.Clean("  <b>Home</b>\n\t  ")=="bHome/b","Names cannot inject rich text or control characters");
        Check(AtlasWorldData.Clean(new string('x',100)).Length==48,"Name length limit");
        Check(AtlasWorldData.Search("Home","Meadows","MEAD"),"Search biome case-insensitively");
        Check(AtlasWorldData.Search("Trader","Black Forest"," trad "),"Search name with trimmed input");
        Check(!AtlasWorldData.Search("Trader","Black Forest","swamp"),"Exclude nonmatching names");
        Check(AtlasWorldData.Search("Home","Meadows",""),"Empty query shows all destinations");
        for(int i=0;i<10000;i++)world.Next("Ocean");
        Check(AtlasWorldData.DefaultName("Ocean",world.Next("Ocean"))=="Atlas-10001","Sequences grow beyond three digits");
        Check(AtlasWorldData.WithBiome(a.Display,"Meadows")=="Atlas-001 (Meadows)","Biome shown once after automatic name");
        Check(AtlasWorldData.WithBiome("Home","Black Forest")=="Home (Black Forest)","Custom names retain biome suffix");
        Check(AtlasWorldData.WithBiome("Home (Black Forest)","Black Forest")=="Home (Black Forest)","Already suffixed labels must not duplicate biome");
        var folder=Path.Combine(Path.GetTempPath(),"PortalAtlas-favourite-checks-"+Guid.NewGuid().ToString("N"));
        var path=Path.Combine(folder,"character1-world1.txt");
        Check(FavouriteStore.Read(path)==null,"No existing backup permits character-data migration");
        FavouriteStore.Save(path,new[]{"portal:100:200:300","pillar:-400:500:600","portal:100:200:300"});
        var fresh=FavouriteStore.Read(path);
        Check(fresh.Count==2 && fresh.Contains("pillar:-400:500:600"),"Favourites survive a new store read and deduplicate");
        FavouriteStore.Save(path,new[]{"portal:100:200:300"});
        Check(FavouriteStore.Read(path).Count==1,"Unstarring persists immediately");
        Check(FavouriteStore.Read(path+".bak").Count==2,"Previous favourite selection is backed up atomically");
        FavouriteStore.Save(path,new string[0]);Check(FavouriteStore.Read(path).Count==0,"Unstarring every entry does not resurrect old character data");
        Check(FavouriteStore.Read(Path.Combine(folder,"character2-world1.txt"))==null,"Character favourites are isolated");
        Check(FavouriteStore.Read(Path.Combine(folder,"character1-world2.txt"))==null,"World favourites are isolated");
        var stable=FavouriteStore.Key(false,12.34f,5.6f,-78.9f);
        Check(stable=="portal:1234:560:-7890","World position key has deterministic centimetre precision");
        Check(stable!=FavouriteStore.Key(true,12.34f,5.6f,-78.9f),"Pillars and normal portals have separate identities");
        var oldCulture=System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("de-DE");
            Check(FavouriteStore.Key(false,12.34f,5.6f,-78.9f)==stable,"Favourite identity is independent of locale");
        } finally {System.Globalization.CultureInfo.CurrentCulture=oldCulture;}
        FavouriteStore.Save(path,new[]{stable});
        Check(FavouriteStore.Read(path).Contains(FavouriteStore.Key(false,12.34f,5.6f,-78.9f)),"Saved position-based favourites survive a fresh store read");
        Check(!FavouriteStore.Read(path).Contains(FavouriteStore.Key(false,15,5.6f,-78.9f)),"A different destination is not accidentally starred");
        FavouriteStore.Save(path,new string[0]);
        Check(FavouriteStore.Read(path).Count==0,"A cleared v2 list survives reload without restoring legacy stars");
        // Only files created by this harness are removed, without recursive directory deletion.
        foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);
        Console.WriteLine("Passed "+count+" naming, recovery, world isolation and search checks.");
    }
}
