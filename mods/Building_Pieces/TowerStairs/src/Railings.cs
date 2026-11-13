using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
namespace OctoberJemini.TowerStairs {
 public sealed partial class Plugin {
  private static Mesh RailTimberMesh(GameObject source) {
   var filter=source.GetComponent<WearNTear>().m_new.GetComponentInChildren<MeshFilter>(true);
   if(!filter)throw new InvalidOperationException("Wood beam mesh missing.");
   var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
   var vertices=mesh.vertices;var matrix=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
   var bounds=new Bounds(matrix.MultiplyPoint3x4(vertices[0]),Vector3.zero);
   for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);bounds.Encapsulate(vertices[i]);}
   var size=bounds.size;int axis=size.x>=size.y&&size.x>=size.z?0:(size.y>=size.z?1:2);
   var rotation=Quaternion.FromToRotation(axis==0?Vector3.right:(axis==1?Vector3.up:Vector3.forward),Vector3.forward);
   for(int i=0;i<vertices.Length;i++) {
    var v=vertices[i]-bounds.center;v=new Vector3(v.x/size.x,v.y/size.y,v.z/size.z);vertices[i]=rotation*v;
   }
   mesh.name="TowerRail_Timber";mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  private static Vector2[] TimberLongitudinalUV(Mesh mesh) {
   // Recover the vanilla UV direction along each face of the normalized beam.
   // Keeping this direction preserves its authored grain and normal mapping.
   var v=mesh.vertices;var uv=mesh.uv;var tri=mesh.triangles;
   var gradients=new Vector2[v.Length];var weights=new float[v.Length];
   for(int i=0;i<tri.Length;i+=3) {
    int a=tri[i],b=tri[i+1],c=tri[i+2];
    var e1=v[b]-v[a];var e2=v[c]-v[a];var n=Vector3.Cross(e1,e2);
    float area2=n.sqrMagnitude;if(area2<1e-12f)continue;
    var d1=uv[b]-uv[a];var d2=uv[c]-uv[a];
    var g1=Vector3.Cross(e2,n)/area2;var g2=Vector3.Cross(n,e1)/area2;
    var gradient=new Vector2(g1.z*d1.x+g2.z*d2.x,g1.z*d1.y+g2.z*d2.y);
    float weight=Mathf.Sqrt(area2);
    foreach(int j in new[]{a,b,c}){gradients[j]+=gradient*weight;weights[j]+=weight;}
   }
   for(int i=0;i<gradients.Length;i++)if(weights[i]>0)gradients[i]/=weights[i];
   return gradients;
  }
  private static List<Mesh> RailTimberSections(List<RailBeam> beams,Mesh template) {
   var meshes=new List<Mesh>();var v=template.vertices;var original=template.uv;
   var gradient=TimberLongitudinalUV(template);
   foreach(var beam in beams) {
    var mesh=UnityEngine.Object.Instantiate(template);var uv=new Vector2[original.Length];
    float length=Vector3.Distance(V(beam.A),V(beam.B))+.012f;
    for(int i=0;i<uv.Length;i++) {
     // Match the original one-metre beam's texture density. Overlapping ends
     // sample matching distances instead of restarting the texture each span.
     float mapped=(float)beam.TextureStart-.006f+(v[i].z+.5f)*length-.5f;
     uv[i]=original[i]+gradient[i]*(mapped-v[i].z);
    }
    mesh.uv=uv;mesh.RecalculateTangents();meshes.Add(mesh);
   }
   return meshes;
  }
  private static GameObject RailVisual(GameObject parent,string name,List<RailBeam> beams,List<Mesh> meshes,Material material) {
   var group=new GameObject(name);group.layer=parent.layer;group.transform.SetParent(parent.transform,false);
   for(int i=0;i<beams.Count;i++) {
    var beam=beams[i];var go=Visual(group,"Timber",meshes[i],material);var a=V(beam.A);var b=V(beam.B);
    go.transform.localPosition=(a+b)/2;go.transform.localRotation=Quaternion.LookRotation(b-a);
    go.transform.localScale=new Vector3((float)beam.Size,(float)beam.Size,Vector3.Distance(a,b)+.012f);
   }
   return group;
  }
  private void RegisterRailings() {
   var source=PrefabManager.Instance.GetPrefab("wood_beam_1");
   if(!source)throw new InvalidOperationException("Vanilla wood_beam_1 unavailable.");
   var original=source.GetComponent<WearNTear>();var timber=RailTimberMesh(source);
   var material=MaterialFor(original.m_new,null);
   if(!material)throw new InvalidOperationException("Wood beam material unavailable.");
   foreach(var layout in StairLayout.All()) {
    if(layout.Legacy||layout.Length!=2)continue;
    foreach(bool curved in new[]{false,true})foreach(bool free in new[]{false,true})foreach(int dir in new[]{1,-1}) {
     string id="oct_tower_rail_"+layout.Id+(curved?"_curved":"_fitted")+(free?"_open":"_wall")+(dir==1?"_cw":"_ccw");
     if(PieceManager.Instance.GetPiece(id)!=null)continue;
     var clone=PrefabManager.Instance.CreateClonedPrefab(id,source);
     for(int i=clone.transform.childCount-1;i>=0;i--)DestroyImmediate(clone.transform.GetChild(i).gameObject);
     foreach(var c in clone.GetComponents<Collider>())DestroyImmediate(c);
     foreach(var r in clone.GetComponents<Renderer>())DestroyImmediate(r);
     foreach(var f in clone.GetComponents<MeshFilter>())DestroyImmediate(f);
     clone.transform.localRotation=Quaternion.identity;clone.transform.localScale=Vector3.one;
     var beams=RailGeometry.Build(layout,dir,free,curved);var sections=RailTimberSections(beams,timber);var wear=clone.GetComponent<WearNTear>();
     wear.m_new=RailVisual(clone,"new",beams,sections,material);
     wear.m_worn=RailVisual(clone,"worn",beams,sections,MaterialFor(original.m_worn,material));
     wear.m_broken=RailVisual(clone,"broken",beams,sections,MaterialFor(original.m_broken,material));
     wear.m_worn.SetActive(false);wear.m_broken.SetActive(false);wear.m_fragmentRoots=new[]{wear.m_new};
     var bounds=new Bounds(V(beams[0].A),Vector3.zero);
     foreach(var beam in beams) {
      var a=V(beam.A);var b=V(beam.B);bounds.Encapsulate(a);bounds.Encapsulate(b);
      var go=new GameObject("collider");go.layer=clone.layer;go.transform.SetParent(clone.transform,false);
      go.transform.localPosition=(a+b)/2;go.transform.localRotation=Quaternion.LookRotation(b-a);
      go.AddComponent<BoxCollider>().size=new Vector3((float)beam.Size,(float)beam.Size,Vector3.Distance(a,b)+.012f);
     }
     wear.m_comOffset=bounds.center;
     Snap(clone,"Stair lower mount",layout.Edge(0,free,0,dir));
     Snap(clone,"Stair upper mount",layout.Edge(1,free,layout.Rise,dir));
     Snap(clone,"Handrail lower joint",RailGeometry.Path(layout,dir,free,curved,0,RailGeometry.Height));
     Snap(clone,"Handrail upper joint",RailGeometry.Path(layout,dir,free,curved,1,RailGeometry.Height));
     var cfg=new PieceConfig {
      Enabled=curved,
      Name="Tower Rail "+layout.Label+" "+(free==layout.Inside?"Inner Edge":"Outer Edge")+" "+(curved?"Curved":"Fitted")+" "+(dir==1?"Clockwise":"Counterclockwise"),
      Description=string.Format("Wood railing for Tower Stairs {0}. {1} edge, {2}m rise, 1m handrail height. Snap Stair lower/upper mount to matching stair corner. {3}",layout.Label,free?"open":"wall",layout.Rise,curved?"Curved handrail with posts anchored to the straight stair edge.":"Straight handrail follows the polygon edge."),
      PieceTable="Hammer",Category="BuildingWorkbench",Usage=new[]{"Building"},CraftingStation="piece_workbench",
      Requirements=new[]{new RequirementConfig{Item="Wood",Amount=4,Recover=true}}
     };
     if(!PieceManager.Instance.AddPiece(new CustomPiece(clone,false,cfg)))throw new InvalidOperationException("Could not register "+id);
     pieces.Add(clone);
    }
   }
  }
  private void OrderPieces() {
   var table=PieceManager.Instance.GetPieceTable("Hammer");if(!table)return;
   var owned=new HashSet<GameObject>(pieces);int first=table.m_pieces.FindIndex(p=>owned.Contains(p));if(first<0)return;
   table.m_pieces.RemoveAll(p=>owned.Contains(p));
   var ordered=new List<GameObject>();
   foreach(var layout in StairLayout.All()) {
    if(layout.Legacy || layout.Length!=2)continue;
    foreach(string dir in new[]{"cw","ccw"}) {
     foreach(string finish in new[]{""}) {
      var p=pieces.Find(x=>x.name=="oct_tower_stairs_"+layout.Id+finish+"_"+dir);if(p)ordered.Add(p);
     }
     foreach(bool inner in new[]{true,false}) {
      bool free=inner==layout.Inside;
      var p=pieces.Find(x=>x.name=="oct_tower_rail_"+layout.Id+"_curved"+(free?"_open":"_wall")+"_"+dir);if(p)ordered.Add(p);
     }
    }
   }
   table.m_pieces.InsertRange(Math.Min(first,table.m_pieces.Count),ordered);
  }
 }
}
