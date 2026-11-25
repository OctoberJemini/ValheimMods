using UnityEngine;
namespace OctoberJemini.WorthwhileTreasures {
 internal static class ChestIdentity {
  internal static int Tier(Container chest,ZNetView view) {
   // Network prefab identity survives numbered dungeon copies and renamed instances.
   var prefab=ZNetScene.instance&&view&&view.GetZDO()!=null?ZNetScene.instance.GetPrefab(view.GetZDO().GetPrefab()):null;
   string name=prefab?prefab.name:chest.gameObject.name;
   int tier=1;
   if(WorldGenerator.instance!=null) {
    var biome=WorldGenerator.instance.GetBiome(chest.transform.position.x,chest.transform.position.z);
    switch(biome) {
     case Heightmap.Biome.Swamp:tier=2;break;case Heightmap.Biome.Mountain:tier=3;break;
     case Heightmap.Biome.Plains:tier=4;break;case Heightmap.Biome.Mistlands:tier=5;break;
     case Heightmap.Biome.AshLands:tier=6;break;case Heightmap.Biome.DeepNorth:tier=7;break;
    }
   }
   return ChestPolicy.Tier(name,tier);
  }
 }
}
