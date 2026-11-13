using System;
using System.Collections.Generic;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace OctoberJemini.TowerStairs
{
    [BepInPlugin(Guid,"Tower Stairs","0.4.2")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod,VersionStrictness.Patch)]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string Guid="OctoberJemini.TowerStairs";
        private readonly List<GameObject> pieces=new List<GameObject>();
        private bool registered, iconsDone;
        private void Awake()
        {
            PrefabManager.OnVanillaPrefabsAvailable+=Register;
            PieceManager.OnPiecesRegistered+=Icons;
        }
        private static Vector3 V(Point p) { return new Vector3((float)p.X,(float)p.Y,(float)p.Z); }
        private static Mesh MakeMesh(StairLayout layout,int direction,bool walkingCollision=false,bool smooth=false)
        {
            var data=layout.Build(direction,walkingCollision);
            var vertices=new Vector3[data.Vertices.Count]; var uv=new Vector2[vertices.Length];
            var colors=new Color[vertices.Length];
            for(int i=0;i<vertices.Length;i++) { vertices[i]=V(data.Vertices[i]); uv[i]=new Vector2((float)data.UVs[i].X,(float)data.UVs[i].Y); colors[i]=Color.white; }
            var mesh=new Mesh {name="TowerStairs_"+layout.Id+"_"+direction, vertices=vertices, uv=uv, colors=colors, triangles=data.Triangles.ToArray()};
            if(smooth) { for(int i=0;i<uv.Length;i++) uv[i]=new Vector2(.05f+uv[i].x*.8f,.53f+uv[i].y*.55f); mesh.uv=uv; }
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
        private static GameObject Visual(GameObject parent,string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name); go.layer=parent.layer;
            go.transform.SetParent(parent.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            return go;
        }
        private static Material MaterialFor(GameObject state,Material fallback)
        {
            if(!state) return fallback;
            var renderer=state.GetComponentInChildren<MeshRenderer>(true);
            return renderer && renderer.sharedMaterial ? renderer.sharedMaterial : fallback;
        }
        private static Material SmoothMaterialFor(GameObject state,Material fallback) {
            if(state) foreach(var renderer in state.GetComponentsInChildren<MeshRenderer>(true))
                foreach(var mat in renderer.sharedMaterials) if(mat && mat.name.StartsWith("stonefloor_2x2")) return mat;
            return fallback;
        }
        private void Register()
        {
            if(registered) return;
            try
            {
                var source=PrefabManager.Instance.GetPrefab("stone_stair");
                if(!source) throw new InvalidOperationException("Vanilla stone_stair not available.");
                var wearSource=source.GetComponent<WearNTear>();
                var material=MaterialFor(wearSource.m_new,null);
                if(!material) throw new InvalidOperationException("Stone stair material not available.");
                var smoothSource=PrefabManager.Instance.GetPrefab("stone_floor_2x2");
                if(!smoothSource) throw new InvalidOperationException("Vanilla stone_floor_2x2 unavailable.");
                var smoothWear=smoothSource.GetComponent<WearNTear>();
                var smoothMaterial=SmoothMaterialFor(smoothWear.m_new,null);
                if(!smoothMaterial) throw new InvalidOperationException("Smooth stone floor material unavailable.");
                foreach(var layout in StairLayout.All()) foreach(int direction in new[]{1,-1}) foreach(bool smooth in new[]{false,true})
                {
                    if(smooth && (layout.Legacy || layout.Length!=2)) continue;
                    var finishWear=smooth?smoothWear:wearSource;
                    var finishMaterial=smooth?smoothMaterial:material;
                    string suffix=direction==1?"cw":"ccw";
                    string id="oct_tower_stairs_"+layout.Id+(smooth?"_smooth":"")+"_"+suffix;
                    if(PieceManager.Instance.GetPiece(id)!=null) continue;
                    var clone=PrefabManager.Instance.CreateClonedPrefab(id,source);
                    // Keep vanilla Piece, ZNetView and WearNTear behavior; replace geometry.
                    for(int i=clone.transform.childCount-1;i>=0;i--) DestroyImmediate(clone.transform.GetChild(i).gameObject);
                    foreach(var c in clone.GetComponents<Collider>()) DestroyImmediate(c);
                    foreach(var r in clone.GetComponents<Renderer>()) DestroyImmediate(r);
                    foreach(var f in clone.GetComponents<MeshFilter>()) DestroyImmediate(f);
                    clone.transform.localRotation=Quaternion.identity;
                    clone.transform.localScale=Vector3.one;
                    var mesh=MakeMesh(layout,direction,false,smooth);
                    var wear=clone.GetComponent<WearNTear>();
                    wear.m_new=Visual(clone,"new",mesh,finishMaterial);
                    wear.m_worn=Visual(clone,"worn",mesh,(smooth?SmoothMaterialFor(finishWear.m_worn,finishMaterial):MaterialFor(finishWear.m_worn,finishMaterial)));
                    wear.m_broken=Visual(clone,"broken",mesh,(smooth?SmoothMaterialFor(finishWear.m_broken,finishMaterial):MaterialFor(finishWear.m_broken,finishMaterial)));
                    wear.m_worn.SetActive(false); wear.m_broken.SetActive(false);
                    wear.m_comOffset=mesh.bounds.center;
                    wear.m_fragmentRoots=new[]{wear.m_new};
                    AddPlacementColliders(clone,layout,direction);
                    foreach(bool outer in new[]{false,true})
                    {
                        Snap(clone,"Start "+(outer?"outer":"wall"),layout.Edge(0,outer,0,direction));
                        Snap(clone,"End "+(outer?"outer":"wall"),layout.Edge(1,outer,layout.Rise,direction));
                    }
                    Snap(clone,"Start centre",Midpoint(layout.Edge(0,false,0,direction),layout.Edge(0,true,0,direction)));
                    Snap(clone,"End centre",Midpoint(layout.Edge(1,false,layout.Rise,direction),layout.Edge(1,true,layout.Rise,direction)));
                    var config=new PieceConfig {
                        Name="Tower Stairs "+layout.Label+(smooth?" Smooth Stone ":" Stone ")+(direction==1?"Clockwise":"Counterclockwise"),
                        Enabled=!smooth && !layout.Legacy && layout.Length!=1,
                        Description=layout.Legacy?"Original R4 exterior prototype.":string.Format("Fits {0}m poles turned {1} degrees. {2}m wide, {3}m rise. Align wall edge with pole centreline; rotate {4} clicks between sections.",layout.Length,layout.Angle*180/Math.PI,layout.Width,layout.Rise,(int)Math.Round(layout.Angle/(Math.PI/8))),
                        PieceTable="Hammer", Category="BuildingStonecutter", Usage=new[]{"Building","Stairs"},
                        CraftingStation="piece_stonecutter", Requirements=new[]{new RequirementConfig {Item="Stone",Amount=8,Recover=true}}
                    };
                    if(!PieceManager.Instance.AddPiece(new CustomPiece(clone,false,config))) throw new InvalidOperationException("Could not register "+id);
                    pieces.Add(clone);
                }
                RegisterRailings();
                registered=true;
                PrefabManager.OnVanillaPrefabsAvailable-=Register;
                Logger.LogInfo("Registered original R4 stairs and pole-matched inside/outside stairs in both directions.");
            }
            catch(Exception ex) {Logger.LogError("Tower stair registration failed: "+ex);}
        }
        private static Point Midpoint(Point a,Point b) {
            return new Point((a.X+b.X)/2,(a.Y+b.Y)/2,(a.Z+b.Z)/2);
        }
        private static void AddPlacementColliders(GameObject parent,StairLayout layout,int direction)
        {
            // Player.UpdatePlacementGhost skips non-convex MeshColliders.
            // Each geometry cell is a closed six-face wedge. Keep separate
            // convex hulls so placement can use ClosestPoint without filling
            // the whole stepped stair profile with a single large hull.
            var data=layout.Build(direction,true);
            if(data.Vertices.Count%24!=0 || data.Triangles.Count!=data.Vertices.Count/24*36)
                throw new InvalidOperationException("Unexpected stair collision cell layout.");
            for(int cell=0;cell<data.Vertices.Count/24;cell++) {
                var vertices=new Vector3[24];var triangles=new int[36];
                for(int i=0;i<24;i++) vertices[i]=V(data.Vertices[cell*24+i]);
                for(int i=0;i<36;i++) triangles[i]=data.Triangles[cell*36+i]-cell*24;
                var mesh=new Mesh {name=parent.name+"_collision_"+cell,vertices=vertices,triangles=triangles};
                mesh.RecalculateBounds();
                var go=new GameObject("collider_"+cell);go.layer=parent.layer;
                go.transform.SetParent(parent.transform,false);
                var collider=go.AddComponent<MeshCollider>();
                collider.sharedMesh=mesh;collider.convex=true;
            }
        }
        private static void Snap(GameObject parent,string name,Point p)
        {
            var go=new GameObject(name); go.tag="snappoint";
            go.transform.SetParent(parent.transform,false); go.transform.localPosition=V(p);
        }
        private void Icons()
        {
            OrderPieces();
            if(iconsDone || !registered || SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            foreach(var piece in pieces)
            {
                try {
                    string resource=piece.name.StartsWith("oct_tower_rail_")?piece.name+".png":piece.name.Substring("oct_tower_stairs_".Length)+".png";
                    piece.GetComponent<Piece>().m_icon=LoadIcon(resource);
                } catch(Exception ex) {Logger.LogWarning("Stair thumbnail: "+ex.Message);}
            }
            iconsDone=true;
        }
        private static Sprite LoadIcon(string resource)
        {
            using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream(resource))
            using(var buffer=new System.IO.MemoryStream()) {
                if(stream==null) throw new InvalidOperationException("Missing thumbnail "+resource);
                stream.CopyTo(buffer);
                var texture=AssetUtils.LoadImage(buffer.ToArray());
                texture.filterMode=FilterMode.Bilinear;
                return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(0.5f,0.5f));
            }
        }
        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable-=Register;
            PieceManager.OnPiecesRegistered-=Icons;
        }
    }
}
