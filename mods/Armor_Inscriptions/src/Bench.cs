using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
namespace OctoberJemini.ArmorInscriptions {
 public sealed class Bench:MonoBehaviour,Hoverable,Interactable {
  internal const string Prefab="OCT_InscriptionBench";
  private static GameObject registered;
  internal static void Register(){
   if(registered)return;
   var source=PrefabManager.Instance.GetPrefab("piece_beehive");
   if(!source)throw new InvalidOperationException("Building template unavailable");
   var clone=PrefabManager.Instance.CreateClonedPrefab(Prefab,source);
   foreach(var component in clone.GetComponents<Beehive>())UnityEngine.Object.DestroyImmediate(component);
   for(int i=clone.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(clone.transform.GetChild(i).gameObject);
   foreach(var c in clone.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
   clone.transform.localScale=Vector3.one;clone.transform.localRotation=Quaternion.identity;
   var wear=clone.GetComponent<WearNTear>();
   wear.m_new=BenchVisual.Build(clone);
   wear.m_worn=UnityEngine.Object.Instantiate(wear.m_new,clone.transform);wear.m_worn.name="Worn";
   wear.m_broken=UnityEngine.Object.Instantiate(wear.m_new,clone.transform);wear.m_broken.name="Weathered";
   wear.m_new.SetActive(true);wear.m_worn.SetActive(false);wear.m_broken.SetActive(false);
   wear.m_fragmentRoots=new[]{wear.m_new};wear.m_comOffset=new Vector3(0,.6f,0);
   wear.m_health=200;wear.m_noRoofWear=false;wear.m_noSupportWear=false;wear.m_supports=false;
   Box(clone,new Vector3(0,1,0),new Vector3(2.12f,.13f,1.06f));
   foreach(float x in new[]{-.86f,.86f})foreach(float z in new[]{-.38f,.38f})Box(clone,new Vector3(x,.48f,z),new Vector3(.15f,.96f,.15f));
   Box(clone,new Vector3(0,1.36f,.46f),new Vector3(2.05f,.5f,.12f));
   var piece=clone.GetComponent<Piece>();piece.m_noInWater=true;piece.m_groundPiece=false;piece.m_clipGround=false;
   clone.AddComponent<Bench>();
   var template=PrefabManager.Instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();
   var station=clone.AddComponent<CraftingStation>();station.m_name="Inscription Bench";station.m_icon=Plugin.LoadIcon("bench");
   station.m_craftRequireRoof=false;station.m_craftRequireFire=false;station.m_showBasicRecipies=false;station.m_canRepair=false;station.m_useDistance=4f;
   station.m_hasCraftTab=true;station.m_useAnimation=template.m_useAnimation;station.m_craftingSkill=Skills.SkillType.None;
   station.m_connectionPoint=clone.transform;station.m_roofCheckPoint=clone.transform;
   station.m_craftItemEffects=template.m_craftItemEffects;station.m_craftItemDoneEffects=template.m_craftItemDoneEffects;
   var config=new PieceConfig{Name="Inscription Bench",Description="Sew purchased inscription patches onto body or leg armor. Higher-tier armor needs more matching patches. Press Use to select a patch and armor.",PieceTable="Hammer",Category="Crafting",CraftingStation="piece_workbench",
    Icon=Plugin.LoadIcon("bench"),Requirements=new[]{new RequirementConfig{Item="Wood",Amount=20,Recover=true},new RequirementConfig{Item="LeatherScraps",Amount=10,Recover=true},new RequirementConfig{Item="DeerHide",Amount=5,Recover=true}}};
   if(!PieceManager.Instance.AddPiece(new CustomPiece(clone,false,config)))throw new InvalidOperationException("Bench registration rejected");
   registered=clone;
  }
  private static void Box(GameObject go,Vector3 center,Vector3 size){var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;}
  internal bool CanUse(Player player){
   if(!this||!player||player!=Player.m_localPlayer||player.IsDead()||player.IsTeleporting())return false;
   var view=GetComponent<ZNetView>();
   return view&&view.IsValid()&&(player.transform.position-transform.position).sqrMagnitude<=16f&&PrivateArea.CheckAccess(transform.position,0,false,false);
  }
  public string GetHoverName(){return "Inscription Bench";}
  public float GetHoverOffset(){return 0f;}
  public string GetHoverText(){return Localization.instance.Localize("Inscription Bench\n[<color=yellow><b>$KEY_Use</b></color>] Apply armor inscriptions");}
  public bool Interact(Humanoid user,bool hold,bool alt){
   if(hold)return false;
   var p=user as Player;if(!CanUse(p))return false;
   ApplyMenu.Open(p,this);return true;
  }
  public bool UseItem(Humanoid user,ItemDrop.ItemData item){
   var id=Plugin.PatchId(item);var p=user as Player;
   if(id==null||!CanUse(p))return false;
   ApplyMenu.Open(p,this,id);return true;
  }
 }
 internal static class BenchVisual {
  private static Mesh beam;
  private static Material wood,hide,thread,bone;
  private static GameObject Child(GameObject parent,string name){
   var go=new GameObject(name);go.layer=parent.layer;go.transform.SetParent(parent.transform,false);return go;
  }
  private static void Part(GameObject parent,string name,Mesh mesh,Material mat,Vector3 pos,Vector3 scale,Quaternion rotation){
   var go=Child(parent,name);go.transform.localPosition=pos;go.transform.localScale=scale;go.transform.localRotation=rotation;
   go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
  }
  private static Material Native(string prefab){
   var source=PrefabManager.Instance.GetPrefab(prefab);
   var r=source?source.GetComponentInChildren<MeshRenderer>(true):null;
   if(!r||!r.sharedMaterial)throw new InvalidOperationException("Missing material: "+prefab);
   return r.sharedMaterial;
  }
  private static Material Tint(Material source,Color color){
   var m=new Material(source);if(m.HasProperty("_Color"))m.SetColor("_Color",color);
   if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0);
   if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",0);
   return m;
  }
  internal static GameObject Build(GameObject parent){
   var native=PrefabManager.Instance.GetPrefab("wood_beam_1");
   var model=native.GetComponent<WearNTear>().m_new;
   var filter=model.GetComponentInChildren<MeshFilter>(true);
   beam=Normalize(native,filter);
   wood=model.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
   hide=Tint(Native("DeerHide"),new Color(.8f,.65f,.46f));
   thread=Tint(Native("Wood"),new Color(.78f,.62f,.4f));
   bone=Tint(Native("Wood"),new Color(.95f,.87f,.64f));
   var root=Child(parent,"InscriptionBenchVisual");
   // Five rough planks, sturdy legs, shelf and diagonal bracing.
   for(int n=0;n<5;n++)Part(root,"TopPlank",beam,wood,new Vector3(0,1,-.42f+n*.21f),new Vector3(.2f,.1f,2.1f),Quaternion.Euler(0,90,0));
   foreach(float x in new[]{-.86f,.86f})foreach(float z in new[]{-.38f,.38f})Beam(root,wood,new Vector3(x,0,z),new Vector3(x,1,z),.15f);
   for(int n=0;n<4;n++)Part(root,"ShelfPlank",beam,wood,new Vector3(0,.27f,-.3f+n*.2f),new Vector3(.18f,.08f,1.8f),Quaternion.Euler(0,90,0));
   foreach(float z in new[]{-.37f,.37f}){Beam(root,wood,new Vector3(-.85f,.35f,z),new Vector3(-.38f,.92f,z),.09f);Beam(root,wood,new Vector3(.85f,.35f,z),new Vector3(.38f,.92f,z),.09f);}
   foreach(float x in new[]{-.95f,.95f})Beam(root,wood,new Vector3(x,1,.45f),new Vector3(x,1.85f,.45f),.1f);
   Part(root,"RearShelf",beam,wood,new Vector3(0,1.48f,.36f),new Vector3(.34f,.08f,1.96f),Quaternion.Euler(0,90,0));
   foreach(float x in new[]{-.85f,.85f})Beam(root,wood,new Vector3(x,1.15f,.45f),new Vector3(x,1.45f,.20f),.065f);
   StorageBox(root,new Vector3(-.57f,1.67f,.36f),new Vector3(.53f,.30f,.28f));
   StorageBox(root,new Vector3(0,1.62f,.36f),new Vector3(.25f,.20f,.25f));
   StorageBox(root,new Vector3(.55f,1.70f,.36f),new Vector3(.36f,.36f,.28f));
   BlankBoxPatch(root,new Vector3(-.57f,1.67f,.196f));
   Part(root,"HideMat",HideMesh(),hide,new Vector3(-.35f,1.058f,-.12f),new Vector3(.96f,1,.68f),Quaternion.identity);
   Item(root,"DeerHide","FoldedHideA",new Vector3(-.16f,.38f,0),new Vector3(.75f,.15f,.45f));
   Item(root,"LeatherScraps","FoldedHideB",new Vector3(.05f,.49f,0),new Vector3(.65f,.10f,.4f));
   // Render-only native hide meshes; the bottom hide bends over the front edge.
   Item(root,"DeerHide","DrapedHide",new Vector3(.44f,1.10f,-.30f),new Vector3(1.02f,.025f,1.65f),false,true);
   Item(root,"DeerHide","HidePileMiddle",new Vector3(.44f,1.14f,-.05f),new Vector3(1.03f,.035f,.85f));
   Item(root,"DeerHide","HidePileTop",new Vector3(.48f,1.18f,-.02f),new Vector3(.94f,.035f,.77f));
   var threadA=Item(root,"LinenThread","ThreadBundleA",new Vector3(-.75f,1.16f,.17f),new Vector3(.20f,.25f,.20f),true,false,new Vector3(0,12,90));
   var threadB=Item(root,"LinenThread","ThreadBundleB",new Vector3(-.50f,1.16f,.17f),new Vector3(.20f,.25f,.20f),true,false,new Vector3(0,-9,90));
   var threadC=Item(root,"LinenThread","ThreadBundleC",new Vector3(-.63f,1.32f,.17f),new Vector3(.20f,.25f,.20f),true,false,new Vector3(0,5,90));
   float threadTop=Mathf.Max(RestOn(threadA,1.052f),RestOn(threadB,1.052f));
   RestOn(threadC,threadTop-.012f);
   Item(root,"BronzeNails","UpholsteryTacks",new Vector3(-.81f,1.08f,-.32f),new Vector3(.12f,.035f,.11f));
   Beam(root,bone,new Vector3(-.67f,1.085f,-.28f),new Vector3(-.17f,1.085f,-.06f),.016f);
   Item(root,"Flint","FlintTool",new Vector3(-.15f,1.085f,-.35f),new Vector3(.19f,.035f,.1f));
   Loop(root,new Vector3(-.5f,1.09f,-.06f),.13f,false);
   Combine(root);
   return root;
  }
  internal static GameObject BuildExtension(GameObject parent,bool tools){
   var root=Child(parent,tools?"StitchingToolsVisual":"LeatherRackVisual");
   if(!tools){
    foreach(float x in new[]{-.63f,.63f}){
     Beam(root,wood,new Vector3(x,.05f,0),new Vector3(x,1.72f,0),.11f);
     Beam(root,wood,new Vector3(x,.06f,-.32f),new Vector3(x,.06f,.32f),.13f);
     Beam(root,wood,new Vector3(x,.1f,-.28f),new Vector3(x,.43f,0),.075f);
     Lash(root,new Vector3(x,1.52f,0));
     Lash(root,new Vector3(x,.20f,0));
    }
    Beam(root,wood,new Vector3(-.72f,1.52f,0),new Vector3(.72f,1.52f,0),.10f);
    var shades=new[]{new Color(.40f,.23f,.12f),new Color(.61f,.40f,.23f),new Color(.30f,.16f,.09f)};
    for(int i=0;i<3;i++)Part(root,"IndividualDrapedLeather",RackLeather(i),LeatherSurface(shades[i],i+7),new Vector3(-.40f+i*.40f,1.52f,0),Vector3.one,Quaternion.identity);
    Part(root,"LowSlat",beam,wood,new Vector3(0,.16f,0),new Vector3(.35f,.06f,1.25f),Quaternion.Euler(0,90,0));
    TailoringLeatherStack(root,new Vector3(0,.20f,0),.80f);
   }else{
    foreach(float x in new[]{-.34f,.34f})foreach(float z in new[]{-.24f,.24f})Beam(root,wood,new Vector3(x,0,z),new Vector3(x,.90f,z),.10f);
    for(int i=0;i<3;i++)Part(root,"TablePlank",beam,wood,new Vector3(0,.90f,-.22f+i*.22f),new Vector3(.21f,.07f,.86f),Quaternion.Euler(0,90,0));
    Part(root,"LowerShelf",beam,wood,new Vector3(0,.25f,0),new Vector3(.5f,.06f,.72f),Quaternion.Euler(0,90,0));
    var metal=Surface(new Color(.24f,.26f,.27f));
    foreach(float x in new[]{-.36f,.36f})foreach(float z in new[]{-.27f,.27f}){
     Part(root,"BracketFront",beam,metal,new Vector3(x,.875f,z+Mathf.Sign(z)*.065f),new Vector3(.018f,.09f,.13f),Quaternion.Euler(0,90,0));
     Part(root,"BracketSide",beam,metal,new Vector3(x+Mathf.Sign(x)*.065f,.875f,z),new Vector3(.018f,.09f,.13f),Quaternion.identity);
     Part(root,"Rivet",Cylinder(),metal,new Vector3(x,.875f,z+Mathf.Sign(z)*.078f),new Vector3(.022f,.012f,.022f),Quaternion.Euler(90,0,0));
    }
    var clamp=Child(root,"StitchingClamp");clamp.transform.localPosition=new Vector3(-.18f,.94f,0);
    Part(clamp,"ClampBase",beam,wood,new Vector3(0,.025f,0),new Vector3(.20f,.05f,.32f),Quaternion.Euler(0,90,0));
    foreach(float x in new[]{-.09f,.09f})Part(clamp,"BroadJaw",beam,wood,new Vector3(x,.16f,0),new Vector3(.055f,.27f,.15f),Quaternion.Euler(0,0,x<0?-7:7));
    Beam(clamp,metal,new Vector3(-.17f,.14f,0),new Vector3(.17f,.14f,0),.018f);
    Part(clamp,"ScrewKnob",Cylinder(),wood,new Vector3(.18f,.14f,0),new Vector3(.065f,.035f,.065f),Quaternion.Euler(0,0,90));
    Part(clamp,"LeatherBeingSewn",RackLeather(1),LeatherSurface(new Color(.48f,.28f,.14f),19),new Vector3(0,.27f,0),new Vector3(.36f,.24f,.32f),Quaternion.identity);
    var stitch=Surface(new Color(.73f,.65f,.46f));
    for(int i=0;i<5;i++)Beam(clamp,stitch,new Vector3(-.045f+i*.022f,.252f,-.038f),new Vector3(-.035f+i*.022f,.241f,-.041f),.004f);
    Part(root,"ToolBlock",beam,wood,new Vector3(.22f,.96f,.18f),new Vector3(.13f,.06f,.30f),Quaternion.Euler(0,90,0));
    for(int i=0;i<3;i++){float x=.12f+i*.09f;Beam(root,wood,new Vector3(x,.98f,.18f),new Vector3(x,1.09f,.18f),.035f);Beam(root,metal,new Vector3(x,1.09f,.18f),new Vector3(x,1.32f,.18f),.009f);}
    for(int i=0;i<3;i++){var bundle=Item(root,"LinenThread","ThreadBundle",new Vector3(.10f+i*.10f,1.0f,-.18f),new Vector3(.09f,.18f,.09f),true,false,new Vector3(90,0,0));RestOn(bundle,.938f);}
    Shears(root,metal,new Vector3(-.20f,.945f,-.19f));
    TailoringLeatherStack(root,new Vector3(0,.285f,0));
   }
   Combine(root);return root;
  }
  private static Material Surface(Color color){
   var mat=Tint(wood,Color.white);var texture=new Texture2D(8,8,TextureFormat.RGBA32,false);
   var pixels=new Color[64];for(int i=0;i<64;i++)pixels[i]=color*(.90f+((i*13)%7)*.025f);
   texture.SetPixels(pixels);texture.Apply();texture.filterMode=FilterMode.Point;
   mat.mainTexture=texture;mat.mainTextureScale=Vector2.one;mat.mainTextureOffset=Vector2.zero;
   mat.DisableKeyword("_NORMALMAP");if(mat.HasProperty("_BumpMap"))mat.SetTexture("_BumpMap",null);
   if(mat.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",Color.black);return mat;
  }
  private static Material LeatherSurface(Color color,int seed){
   var mat=Surface(color);var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);var pixels=new Color[1024];var random=new System.Random(seed);
   for(int y=0;y<32;y++)for(int x=0;x<32;x++){
    float grain=.90f+(float)random.NextDouble()*.15f+.025f*Mathf.Sin(x*.6f+y*.31f+seed);
    var c=color*grain;c.a=1;pixels[y*32+x]=c;
   }
   tex.SetPixels(pixels);tex.Apply();tex.filterMode=FilterMode.Point;mat.mainTexture=tex;return mat;
  }
  private static void Lash(GameObject root,Vector3 center){
   var leather=LeatherSurface(new Color(.32f,.16f,.075f),31);
   for(int side=-1;side<=1;side+=2)Part(root,"DiagonalLeatherLashing",LashingMesh(side),leather,center,Vector3.one,Quaternion.identity);
  }
  private static Mesh LashingMesh(int direction){
   var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
   for(int n=0;n<48;n++){
    float a=n*Mathf.PI/24,b=(n+1)*Mathf.PI/24;
    float ca=Mathf.Cos(a),sa=Mathf.Sin(a),cb=Mathf.Cos(b),sb=Mathf.Sin(b);
    var p=new Vector3(Mathf.Sign(ca)*Mathf.Sqrt(Mathf.Abs(ca))*.096f,direction*.063f*ca,Mathf.Sign(sa)*Mathf.Sqrt(Mathf.Abs(sa))*.086f);
    var q=new Vector3(Mathf.Sign(cb)*Mathf.Sqrt(Mathf.Abs(cb))*.096f,direction*.063f*cb,Mathf.Sign(sb)*Mathf.Sqrt(Mathf.Abs(sb))*.086f);
    var na=new Vector3(ca,0,sa)*.004f;var nb=new Vector3(cb,0,sb)*.004f;var w=Vector3.up*.017f;
    LeatherFace(v,uv,t,p-w+na,q-w+nb,q+w+nb,p+w+na);
    LeatherFace(v,uv,t,p+w-na,q+w-nb,q-w-nb,p-w-na);
    LeatherFace(v,uv,t,p+w+na,q+w+nb,q+w-nb,p+w-na);
    LeatherFace(v,uv,t,p-w-na,q-w-nb,q-w+nb,p-w+na);
   }
   for(int i=0;i<t.Count;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
   return LeatherMesh(v,uv,t);
  }
  private static Mesh RackLeather(int variant){
   const int columns=12,rows=32;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
   float width=variant==0?.35f:variant==1?.40f:.34f;float length=variant==0?1.48f:variant==1?1.27f:1.60f;
   for(int face=0;face<2;face++)for(int row=0;row<=rows;row++)for(int col=0;col<=columns;col++){
    float u=(float)col/columns,sheet=(float)row/rows;float along=(sheet-.5f)*length;
    float edge=1f-.13f*Mathf.Pow(Mathf.Abs(sheet-.5f)*2,4)+.035f*Mathf.Sin(sheet*19+variant*3);
    float x=(u-.5f)*width*edge+.010f*Mathf.Sin(sheet*11+variant);
    // Radius exceeds the crossbeam's 0.071m corner radius, including sheet thickness.
    float radius=.105f,arc=Mathf.Min(Mathf.Abs(along)/radius,Mathf.PI*.5f),sign=Mathf.Sign(along),thickness=face==0?.003f:-.003f;
    float z=sign*(radius+thickness)*Mathf.Sin(arc);
    float y=(radius+thickness)*Mathf.Cos(arc)-Mathf.Max(0,Mathf.Abs(along)-radius*Mathf.PI*.5f);
    float hanging=Mathf.Clamp01((Mathf.Abs(along)-.17f)*4);
    z+=hanging*.014f*Mathf.Sin(u*9+variant*2);y+=hanging*.022f*Mathf.Sin(u*8+variant*1.7f);
    v.Add(new Vector3(x,y,z));uv.Add(new Vector2(u,sheet));
   }
   int stride=columns+1,faceSize=stride*(rows+1);
   for(int row=0;row<rows;row++)for(int col=0;col<columns;col++){
    int a=row*stride+col,b=a+1,c=a+stride+1,d=a+stride;
    t.AddRange(new[]{a,d,c,a,c,b,a+faceSize,b+faceSize,c+faceSize,a+faceSize,c+faceSize,d+faceSize});
   }
   for(int row=0;row<rows;row++)foreach(int col in new[]{0,columns}){
    int a=row*stride+col,b=a+stride;t.AddRange(new[]{a,b,b+faceSize,a,b+faceSize,a+faceSize});
   }
   for(int col=0;col<columns;col++)foreach(int row in new[]{0,rows}){
    int a=row*stride+col,b=a+1;t.AddRange(new[]{a,b+faceSize,b,a,a+faceSize,b+faceSize});
   }
   return LeatherMesh(v,uv,t);
  }
  private static Mesh LeatherMesh(List<Vector3> v,List<Vector2> uv,List<int> t){
   var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.colors=Enumerable.Repeat(Color.white,v.Count).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  private static void FoldedPile(GameObject root,Vector3 pos,float width){
   var shades=new[]{new Color(.45f,.27f,.16f),new Color(.92f,.75f,.52f),new Color(.65f,.40f,.24f)};float top=pos.y;
   for(int i=0;i<3;i++){
    var piece=Item(root,"DeerHide","FoldedLeather",new Vector3(pos.x+(i-1)*.025f,0,pos.z),new Vector3(width-i*.045f,.018f,.65f),false,false,new Vector3(90,i*7-7,0),true,shades[i]);top=RestOn(piece,top+.003f);
   }
  }
  private static void TailoringLeatherStack(GameObject root,Vector3 pos,float width=.58f){
   var colors=new[]{new Color(.30f,.15f,.075f),new Color(.51f,.31f,.16f),new Color(.68f,.47f,.27f)};
   for(int i=0;i<3;i++){
    Part(root,"FoldedLeatherBundle",FoldedLeatherMesh(width-i*.035f,i),LeatherSurface(colors[i],i+41),pos+new Vector3((i-1)*.017f,i*.075f,0),Vector3.one,Quaternion.Euler(0,(i-1)*5,0));
   }
  }
  private static Mesh FoldedLeatherMesh(float width,int variant){
   // A thick continuous leather strip turns around a rounded fold. The open
   // end shows two layers, while closed side faces prevent dark cutout edges.
   var path=new[]{new Vector2(-.18f,.012f),new Vector2(.14f,.012f),new Vector2(.17f,.018f),new Vector2(.185f,.035f),new Vector2(.17f,.052f),new Vector2(.14f,.058f),new Vector2(-.18f,.058f)};
   var outer=new Vector2[path.Length];var inner=new Vector2[path.Length];
   for(int i=0;i<path.Length;i++){
    var direction=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(i-1,0)]).normalized;
    var normal=new Vector2(-direction.y,direction.x)*.006f;outer[i]=path[i]+normal;inner[i]=path[i]-normal;
   }
   var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
   for(int i=0;i<path.Length-1;i++){
    Vector3 a=new Vector3(-width/2,outer[i].y,outer[i].x),b=new Vector3(width/2,outer[i].y,outer[i].x),c=new Vector3(width/2,outer[i+1].y,outer[i+1].x),d=new Vector3(-width/2,outer[i+1].y,outer[i+1].x);
    Vector3 e=new Vector3(-width/2,inner[i].y,inner[i].x),f=new Vector3(width/2,inner[i].y,inner[i].x),g=new Vector3(width/2,inner[i+1].y,inner[i+1].x),h=new Vector3(-width/2,inner[i+1].y,inner[i+1].x);
    LeatherFace(vertices,uv,triangles,a,d,c,b);LeatherFace(vertices,uv,triangles,e,f,g,h);
    LeatherFace(vertices,uv,triangles,a,e,h,d);LeatherFace(vertices,uv,triangles,b,c,g,f);
    if(i==0)LeatherFace(vertices,uv,triangles,a,b,f,e);
    if(i==path.Length-2)LeatherFace(vertices,uv,triangles,d,h,g,c);
   }
   var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
   // Subdivide before shaping: keep shared positions coincident, preserve the
   // closed strip thickness, and give each bundle its own gentle edge profile.
   SubdivideHide(mesh);var points=mesh.vertices;
   for(int i=0;i<points.Length;i++){
    var q=points[i];float u=q.x/(width*.5f),v=(q.z+.18f)/.365f,phase=variant*1.9f;
    float corner=Mathf.Pow(Mathf.Abs(u),8)*Mathf.Pow(Mathf.Abs(v-.5f)*2,6);
    q.x+=Mathf.Sign(u)*(.008f*Mathf.Sin(v*12+phase)+.004f*Mathf.Sin(v*25+phase))-.012f*Mathf.Sign(u)*corner;
    q.z+=.012f*Mathf.Sin(u*6+phase)*Mathf.Pow(Mathf.Abs(v-.5f)*2,4)-.009f*Mathf.Sign(v-.5f)*corner;
    q.y+=.004f*Mathf.Sin(u*4+v*3+phase)-.003f*(1-u*u)*Mathf.Sin(v*Mathf.PI);
    points[i]=q;
   }
   mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  private static void LeatherFace(List<Vector3> vertices,List<Vector2> uv,List<int> triangles,Vector3 a,Vector3 b,Vector3 c,Vector3 d){
   int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
  }
  private static void Shears(GameObject root,Material metal,Vector3 pos){
   var group=Child(root,"TailoringShears");group.transform.localPosition=pos;
   foreach(float side in new[]{-1f,1f}){
    float z=side*.044f;
    for(int n=0;n<20;n++){float a=n*Mathf.PI/10,b=(n+1)*Mathf.PI/10;
     Beam(group,metal,new Vector3(-.10f+Mathf.Cos(a)*.048f,0,z+Mathf.Sin(a)*.030f),new Vector3(-.10f+Mathf.Cos(b)*.048f,0,z+Mathf.Sin(b)*.030f),.010f);}
    Beam(group,metal,new Vector3(-.06f,0,z),Vector3.zero,.015f);
    Part(group,"TaperedBlade",Blade(),metal,new Vector3(0,side*.003f,0),Vector3.one,Quaternion.Euler(0,side*12,0));
   }
   Part(group,"PivotRivet",Cylinder(),metal,new Vector3(0,.009f,0),new Vector3(.025f,.016f,.025f),Quaternion.identity);
  }
  private static Mesh Blade(){
   var m=new Mesh();m.vertices=new[]{new Vector3(-.018f,.003f,-.018f),new Vector3(-.018f,.003f,.018f),new Vector3(.20f,.003f,0),new Vector3(-.018f,-.003f,-.018f),new Vector3(-.018f,-.003f,.018f),new Vector3(.20f,-.003f,0)};
   m.uv=new[]{Vector2.zero,Vector2.up,Vector2.right,Vector2.zero,Vector2.up,Vector2.right};m.triangles=new[]{0,1,2,3,5,4,0,3,4,0,4,1,1,4,5,1,5,2,2,5,3,2,3,0};m.RecalculateNormals();m.RecalculateBounds();return m;
  }
  private static void BlankBoxPatch(GameObject root,Vector3 center){
   var group=Child(root,"BlankStitchedPatch");group.transform.localPosition=center;
   // A cut leather silhouette, not a rectangular image card; intentionally no emblem.
   var rim=new[]{new Vector2(-.065f,-.085f),new Vector2(.065f,-.085f),new Vector2(.085f,-.065f),new Vector2(.085f,.065f),new Vector2(.065f,.085f),new Vector2(-.065f,.085f),new Vector2(-.085f,.065f),new Vector2(-.085f,-.065f)};
   var vertices=new List<Vector3>{new Vector3(0,0,-.002f)};var uv=new List<Vector2>{new Vector2(.5f,.5f)};var triangles=new List<int>();
   foreach(var point in rim){vertices.Add(new Vector3(point.x,point.y,-.002f));uv.Add(new Vector2(point.x/.17f+.5f,point.y/.17f+.5f));}
   for(int i=0;i<rim.Length;i++)triangles.AddRange(new[]{0,(i+1)%rim.Length+1,i+1});
   Part(group,"BlankLeatherFace",LeatherMesh(vertices,uv,triangles),LeatherSurface(new Color(.42f,.23f,.105f),73),Vector3.zero,Vector3.one,Quaternion.identity);
   var stitching=Surface(new Color(.76f,.63f,.40f));
   for(int edge=0;edge<rim.Length;edge++){
    var a=rim[edge]*.84f;var b=rim[(edge+1)%rim.Length]*.84f;int count=Mathf.Max(1,Mathf.RoundToInt(Vector2.Distance(a,b)/.019f));
    for(int n=0;n<count;n++){
     var start=Vector2.Lerp(a,b,(n+.18f)/count);var end=Vector2.Lerp(a,b,(n+.65f)/count);
     Beam(group,stitching,new Vector3(start.x,start.y,-.005f),new Vector3(end.x,end.y,-.005f),.0035f);
    }
   }
  }
  private static void StorageBox(GameObject root,Vector3 center,Vector3 size){
   var box=Child(root,"SupplyBox");box.transform.localPosition=center;
   foreach(float side in new[]{-1f,1f}){
    Part(box,"FrontBack",beam,wood,new Vector3(0,0,side*size.z*.5f),new Vector3(.035f,size.y,size.x),Quaternion.Euler(0,90,0));
    Part(box,"Side",beam,wood,new Vector3(side*size.x*.5f,0,0),new Vector3(.035f,size.y,size.z),Quaternion.identity);
   }
   Part(box,"Lid",beam,wood,new Vector3(0,size.y*.5f,0),new Vector3(size.z+.04f,.035f,size.x+.05f),Quaternion.Euler(0,90,0));
  }
  private static void Beam(GameObject root,Material mat,Vector3 a,Vector3 b,float width){Part(root,"Timber",beam,mat,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),Quaternion.LookRotation(b-a));}
  private static void Loop(GameObject root,Vector3 center,float radius,bool upright){
   for(int n=0;n<16;n++){float a=n*Mathf.PI/8,b=(n+1)*Mathf.PI/8;
    Vector3 va=upright?new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0):new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
    Vector3 vb=upright?new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0):new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
    Beam(root,thread,center+va,center+vb,.012f);
   }
  }
  private static Mesh Normalize(GameObject source,MeshFilter filter){
   var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var v=mesh.vertices;
   var matrix=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
   var bounds=new Bounds(matrix.MultiplyPoint3x4(v[0]),Vector3.zero);
   for(int i=0;i<v.Length;i++){v[i]=matrix.MultiplyPoint3x4(v[i]);bounds.Encapsulate(v[i]);}
   var size=bounds.size;int axis=size.x>=size.y&&size.x>=size.z?0:size.y>=size.z?1:2;
   var rotation=Quaternion.FromToRotation(axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward,Vector3.forward);
   for(int i=0;i<v.Length;i++){var q=v[i]-bounds.center;v[i]=rotation*new Vector3(q.x/size.x,q.y/size.y,q.z/size.z);}
   mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  private static Mesh Quad(){
   var m=new Mesh();m.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
   m.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};m.triangles=new[]{0,1,2,0,2,3};m.RecalculateNormals();m.RecalculateBounds();return m;
  }
  private static Mesh HideMesh(){
   var m=new Mesh();var v=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{new Vector2(.5f,.5f)};var tri=new List<int>();
   for(int n=0;n<24;n++){float a=n*Mathf.PI/12,r=n%3==0?.5f:.44f;var p=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);v.Add(p);uv.Add(new Vector2(p.x+.5f,p.z+.5f));}
   v.AddRange(v.ToArray());uv.AddRange(uv.ToArray());
   for(int n=0;n<24;n++){tri.Add(0);tri.Add((n+1)%24+1);tri.Add(n+1);tri.Add(25);tri.Add(25+n+1);tri.Add(25+(n+1)%24+1);}
   m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();return m;
  }
  private static Mesh Cylinder(){
   var m=new Mesh();var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
   for(int n=0;n<8;n++){
    float a=n*Mathf.PI/4,b=(n+1)*Mathf.PI/4;int k=v.Count;
    v.Add(new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f));v.Add(new Vector3(Mathf.Cos(a)*.5f,.5f,Mathf.Sin(a)*.5f));
    v.Add(new Vector3(Mathf.Cos(b)*.5f,.5f,Mathf.Sin(b)*.5f));v.Add(new Vector3(Mathf.Cos(b)*.5f,-.5f,Mathf.Sin(b)*.5f));
    uv.Add(new Vector2(0,0));uv.Add(new Vector2(0,1));uv.Add(new Vector2(1,1));uv.Add(new Vector2(1,0));
    t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
    k=v.Count;v.Add(new Vector3(0,.5f,0));v.Add(new Vector3(Mathf.Cos(a)*.5f,.5f,Mathf.Sin(a)*.5f));v.Add(new Vector3(Mathf.Cos(b)*.5f,.5f,Mathf.Sin(b)*.5f));
    uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.up);t.AddRange(new[]{k,k+2,k+1});
   }
   m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
  }
  private static GameObject Item(GameObject parent,string prefab,string name,Vector3 pos,Vector3 size,bool uniform=false,bool drape=false,Vector3 rotation=default(Vector3),bool fold=false,Color? shade=null){
   var source=PrefabManager.Instance.GetPrefab(prefab);var filters=source.GetComponentsInChildren<MeshFilter>(true);
   var pieces=new List<Tuple<Mesh,Material[]>>();Bounds bounds=new Bounds();bool first=true;
   foreach(var f in filters){
    var r=f.GetComponent<MeshRenderer>();if(!r||!f.sharedMesh)continue;
    var mesh=UnityEngine.Object.Instantiate(f.sharedMesh);var verts=mesh.vertices;var matrix=source.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;
    for(int i=0;i<verts.Length;i++){verts[i]=matrix.MultiplyPoint3x4(verts[i]);if(first){bounds=new Bounds(verts[i],Vector3.zero);first=false;}else bounds.Encapsulate(verts[i]);}
    mesh.vertices=verts;pieces.Add(Tuple.Create(mesh,r.sharedMaterials));
   }
   var group=Child(parent,name);group.transform.localPosition=pos;group.transform.localRotation=Quaternion.Euler(rotation);
   foreach(var part in pieces){
    var mesh=part.Item1;if(drape||fold)SubdivideHide(mesh);var verts=mesh.vertices;var s=bounds.size;
    for(int i=0;i<verts.Length;i++){var q=verts[i]-bounds.center;var fitted=uniform?q*Mathf.Min(size.x/Mathf.Max(.001f,s.x),Mathf.Min(size.y/Mathf.Max(.001f,s.y),size.z/Mathf.Max(.001f,s.z))):new Vector3(q.x/Mathf.Max(.001f,s.x)*size.x,q.y/Mathf.Max(.001f,s.y)*size.y,q.z/Mathf.Max(.001f,s.z)*size.z);
     if(drape){float excess=-.44f-(pos.z+fitted.z);if(excess>0){float arc=Mathf.Min(excess,.14f*Mathf.PI*.5f);float angle=arc/.14f;float thickness=fitted.y;fitted.z=-.44f-pos.z-(.14f+thickness)*Mathf.Sin(angle);fitted.y=thickness*Mathf.Cos(angle)-.14f*(1-Mathf.Cos(angle))-Mathf.Max(0,excess-arc);}}
     if(fold){float radius=.065f;float d=Mathf.Abs(fitted.z);float sign=Mathf.Sign(fitted.z);float angle=Mathf.Min(d/radius,Mathf.PI*.5f);float thickness=fitted.y;
      fitted.z=sign*(radius+thickness)*Mathf.Sin(angle);fitted.y=(radius+thickness)*Mathf.Cos(angle)-Mathf.Max(0,d-radius*Mathf.PI*.5f);}
     verts[i]=fitted;}
    mesh.vertices=verts;mesh.RecalculateNormals();mesh.RecalculateBounds();
    if(drape||fold)DoubleSided(mesh);
    mesh.RecalculateTangents();
    var go=Child(group,"HideOrTool");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=shade.HasValue?part.Item2.Select(m=>Tint(m,shade.Value)).ToArray():part.Item2;
   }
   return group;
  }
  private static float RestOn(GameObject group,float surface){
   float low=float.PositiveInfinity,high=float.NegativeInfinity;
   foreach(var filter in group.GetComponentsInChildren<MeshFilter>()){
    var matrix=group.transform.parent.worldToLocalMatrix*filter.transform.localToWorldMatrix;
    foreach(var vertex in filter.sharedMesh.vertices){float y=matrix.MultiplyPoint3x4(vertex).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
   }
   if(float.IsInfinity(low))return surface;
   var pos=group.transform.localPosition;pos.y+=surface-low;group.transform.localPosition=pos;return surface+high-low;
  }
  // Native hides have too few triangles to follow a table edge. Add UV-preserving
  // geometry before bending, then render both sides of the hanging leather.
  private static void SubdivideHide(Mesh mesh){
   for(int pass=0;pass<3;pass++){
    var v=new List<Vector3>(mesh.vertices);var uv=new List<Vector2>(mesh.uv);var colors=new List<Color>(mesh.colors);
    while(colors.Count<v.Count)colors.Add(Color.white);
    while(uv.Count<v.Count)uv.Add(Vector2.zero);
    var subs=new List<int[]>();
    for(int sub=0;sub<mesh.subMeshCount;sub++){
     var source=mesh.GetTriangles(sub);var tris=new List<int>();
     for(int i=0;i<source.Length;i+=3){
      int a=source[i],b=source[i+1],c=source[i+2],ab=v.Count,bc=ab+1,ca=ab+2;
      v.Add((v[a]+v[b])*.5f);v.Add((v[b]+v[c])*.5f);v.Add((v[c]+v[a])*.5f);
      uv.Add((uv[a]+uv[b])*.5f);uv.Add((uv[b]+uv[c])*.5f);uv.Add((uv[c]+uv[a])*.5f);
      colors.Add(Color.Lerp(colors[a],colors[b],.5f));colors.Add(Color.Lerp(colors[b],colors[c],.5f));colors.Add(Color.Lerp(colors[c],colors[a],.5f));
      tris.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});
     }
     subs.Add(tris.ToArray());
    }
    mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);
    for(int sub=0;sub<subs.Count;sub++)mesh.SetTriangles(subs[sub],sub);
   }
  }
  private static void DoubleSided(Mesh mesh){
   var v=mesh.vertices;var uv=mesh.uv;var normals=mesh.normals;var colors=mesh.colors;int count=v.Length;
   if(colors.Length!=count)colors=Enumerable.Repeat(Color.white,count).ToArray();
   mesh.vertices=v.Concat(v).ToArray();mesh.uv=uv.Concat(uv).ToArray();
   mesh.normals=normals.Concat(normals.Select(n=>-n)).ToArray();
   mesh.colors=colors.Concat(colors).ToArray();
   for(int sub=0;sub<mesh.subMeshCount;sub++){
    var t=mesh.GetTriangles(sub);var both=new List<int>(t);
    for(int i=0;i<t.Length;i+=3)both.AddRange(new[]{t[i]+count,t[i+2]+count,t[i+1]+count});
    mesh.SetTriangles(both,sub);
   }
   mesh.RecalculateBounds();
  }
  private static void Combine(GameObject root){
   var groups=new Dictionary<Material,List<CombineInstance>>();var old=root.GetComponentsInChildren<MeshFilter>();
   foreach(var f in old){var r=f.GetComponent<MeshRenderer>();if(!r)continue;
    for(int s=0;s<f.sharedMesh.subMeshCount&&s<r.sharedMaterials.Length;s++){
     var mat=r.sharedMaterials[s];if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();
     groups[mat].Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=s,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
    }
   }
   foreach(var group in groups){var mesh=new Mesh();mesh.CombineMeshes(group.Value.ToArray(),true,true);Part(root,"Combined",mesh,group.Key,Vector3.zero,Vector3.one,Quaternion.identity);}
   foreach(var f in old){UnityEngine.Object.DestroyImmediate(f.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(f);}
  }
 }
}
