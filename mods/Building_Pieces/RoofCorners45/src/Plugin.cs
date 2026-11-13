using System;
using System.Collections.Generic;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace OctoberJemini.RoofCorners45 {
    [BepInPlugin(Guid,"Roof Corners 45","0.3.0")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod,VersionStrictness.Patch)]
    public sealed class Plugin : BaseUnityPlugin {
        public const string Guid="OctoberJemini.RoofCorners45";
        private bool registered;
        private void Awake() { PrefabManager.OnVanillaPrefabsAvailable+=Register; }
        private static Vector3 Map(Vector3 v,double rise,bool half) {
            if(half)return new Vector3(v.x*.5f,v.y,v.z);
            double x,y,z;CornerGeometry.Map(v.x,v.y,v.z,rise,out x,out y,out z);
            return new Vector3((float)x,(float)y,(float)z);
        }
        // M = rotation * diagonalScale * rotation. This represents the full
        // corner shear without reading/writing any imported mesh data.
        private static void VisualTransform(double rise,bool half,out Quaternion outer,out Vector3 scale,out Quaternion inner) {
            outer=inner=Quaternion.identity;scale=new Vector3(.5f,1,1);
            if(half)return;
            switch((int)rise) {
case 1: outer=new Quaternion(0.06325046103f,0.6264577985f,0.7042722337f,0.3279491221f); scale=new Vector3(1.838076216f,0.5411961001f,0.1219594501f); inner=new Quaternion(-0.2366976824f,-0.3037845375f,-0.6663138954f,0.6385256101f); break;
case 2: outer=new Quaternion(0.1781647334f,0.5640238874f,0.6842933054f,0.4264704614f); scale=new Vector3(1.26677685f,0.5411961001f,0.1769615261f); inner=new Quaternion(-0.2343002416f,-0.3060785639f,-0.6671606979f,0.6374291433f); break;
case 4: outer=new Quaternion(-0.4969686862f,0.2745417756f,-0.5030130465f,0.6516339566f); scale=new Vector3(1.07383402f,0.5411961001f,0.2087573689f); inner=new Quaternion(0.295744546f,-0.2450439356f,0.6422890031f,0.6632898835f); break;
                default: throw new ArgumentOutOfRangeException("rise");
            }
        }
        private static readonly int[] BoxTriangles={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
        private static void Reshape(GameObject root,double rise,bool half) {
            var inverseRoot=root.transform.worldToLocalMatrix;
            var visuals=new List<Transform>();
            foreach(Transform child in root.transform) {
                if(child.GetComponentsInChildren<MeshFilter>(true).Length==0)continue;
                if(child.GetComponentsInChildren<Collider>(true).Length!=0)
                    throw new InvalidOperationException("Visual/collider hierarchy requires separate handling: "+child.name);
                visuals.Add(child);
            }
            if(visuals.Count==0)throw new InvalidOperationException("Roof has no visual hierarchy");
            // Exact affine hulls for the source boxes. Keep their native state
            // hierarchy and material; never deform an unreadable imported mesh.
            foreach(var collider in root.GetComponentsInChildren<Collider>(true)) {
                var box=collider as BoxCollider;
                if(!box)throw new InvalidOperationException("Unsupported roof collider "+collider.GetType().Name);
                var toRoot=inverseRoot*collider.transform.localToWorldMatrix;
                var fromRoot=toRoot.inverse;
                var h=box.size/2;var p=box.center;
                var vertices=new[]{p+new Vector3(-h.x,-h.y,-h.z),p+new Vector3(h.x,-h.y,-h.z),p+new Vector3(h.x,h.y,-h.z),p+new Vector3(-h.x,h.y,-h.z),p+new Vector3(-h.x,-h.y,h.z),p+new Vector3(h.x,-h.y,h.z),p+new Vector3(h.x,h.y,h.z),p+new Vector3(-h.x,h.y,h.z)};
                for(int i=0;i<vertices.Length;i++)vertices[i]=fromRoot.MultiplyPoint3x4(Map(toRoot.MultiplyPoint3x4(vertices[i]),rise,half));
                var mesh=new Mesh {name="OctRoofCollider",vertices=vertices,triangles=BoxTriangles};mesh.RecalculateBounds();
                var go=new GameObject("OctRoofCollider");go.layer=collider.gameObject.layer;
                go.transform.SetParent(collider.transform,false);
                var replacement=go.AddComponent<MeshCollider>();replacement.sharedMesh=mesh;replacement.convex=true;
                replacement.sharedMaterial=box.sharedMaterial;replacement.isTrigger=box.isTrigger;replacement.enabled=box.enabled;
                UnityEngine.Object.DestroyImmediate(box);
            }
            foreach(var child in root.GetComponentsInChildren<Transform>(true)) {
                if(!child.CompareTag("snappoint"))continue;
                var local=inverseRoot.MultiplyPoint3x4(child.position);
                child.position=root.transform.TransformPoint(Map(local,rise,half));
            }
            Quaternion outer,inner;Vector3 scale;
            VisualTransform(rise,half,out outer,out scale,out inner);
            foreach(var child in visuals) {
                var first=new GameObject("OctRoofVisualAffine").transform;
                first.SetParent(root.transform,false);
                first.localPosition=Map(Vector3.zero,rise,half);
                first.localRotation=outer;first.localScale=scale;
                var second=new GameObject("OctRoofVisualBasis").transform;
                second.SetParent(first,false);second.localRotation=inner;
                child.SetParent(second,false);
            }
            foreach(var lod in root.GetComponentsInChildren<LODGroup>(true))lod.RecalculateBounds();
            var wear=root.GetComponent<WearNTear>();wear.m_comOffset=Map(wear.m_comOffset,rise,half);
        }
        private void Register() {
            if(registered)return;
            int count=0;
            try {
                foreach(bool half in new[]{false,true}) foreach(int slope in new[]{26,45,67}) foreach(bool shingle in new[]{false,true}) {
                    double rise=slope==26?1:(slope==45?2:4);
                    string sourceName=(shingle?"darkwood_roof":"wood_roof")+(half?"":"_ocorner")+(slope==26?"":"_"+slope);
                    string id="oct_roof_"+(shingle?"shingle":"thatch")+"_"+slope+(half?"_half_width":"_outer_turn45");
                    if(PieceManager.Instance.GetPiece(id)!=null)continue;
                    var source=PrefabManager.Instance.GetPrefab(sourceName);
                    if(!source)throw new InvalidOperationException("Missing vanilla roof "+sourceName);
                    var clone=PrefabManager.Instance.CreateClonedPrefab(id,source);
                    Reshape(clone,rise,half);
                    var requirements=new List<RequirementConfig>{new RequirementConfig {Item="Wood",Amount=half?1:2,Recover=true}};
                    if(shingle)requirements.Add(new RequirementConfig {Item="Tar",Amount=1,Recover=true});
                    var cfg=new PieceConfig {
                        Name=(shingle?"Shingle":"Thatch")+" Roof "+slope+"\u00b0 - "+(half?"Half Width":"Outer 45\u00b0 Turn"),
                        Description=half?"Half the standard panel width (1m), with the original roof length and slope.":"For walls turning 45 degrees (135-degree inside angle). Matches the standard "+slope+"-degree roof slope.",
                        PieceTable="Hammer",Category="BuildingWorkbench",Usage=new[]{"Building","Roof"},
                        CraftingStation="piece_workbench",Requirements=requirements.ToArray()
                    };
                    // Native icons are deliberately reused: exact vanilla camera,
                    // lighting and material appearance, no generated/cache icons.
                    clone.GetComponent<Piece>().m_icon=source.GetComponent<Piece>().m_icon;
                    if(!PieceManager.Instance.AddPiece(new CustomPiece(clone,false,cfg)))throw new InvalidOperationException("Could not register "+id);
                    count++;
                }
                registered=true;PrefabManager.OnVanillaPrefabsAvailable-=Register;
                Logger.LogInfo("Registered "+count+" roofs: six corner turns and six half-width panels; native meshes and icons preserved.");
            } catch(Exception ex) { Logger.LogError("Roof registration failed: "+ex); }
        }
        private void OnDestroy() { PrefabManager.OnVanillaPrefabsAvailable-=Register; }
    }
}
