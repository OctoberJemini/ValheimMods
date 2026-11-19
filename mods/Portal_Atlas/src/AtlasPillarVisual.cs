using System;
using System.IO;
using System.Reflection;
using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;
namespace OctoberJemini.PortalAtlas
{
    internal static class AtlasPillarVisual
    {
        internal static void Build(GameObject prefab)
        {
            prefab.transform.localRotation=Quaternion.identity;
            foreach(var lod in prefab.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(lod);
            foreach(var old in prefab.GetComponentsInChildren<Renderer>(true))Object.DestroyImmediate(old);
            foreach(var old in prefab.GetComponentsInChildren<MeshFilter>(true))Object.DestroyImmediate(old);
            foreach(var old in prefab.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(old);
            var source=PrefabManager.Instance.GetPrefab("stone_wall_2x1");
            var native=source.GetComponent<WearNTear>().m_new.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
            var stone=new Material(native){name="Atlas native stone finish"};
            stone.mainTexture=PaintedStone();stone.color=Color.white;stone.mainTextureScale=Vector2.one;stone.mainTextureOffset=Vector2.zero;
            var flat=new Texture2D(1,1,TextureFormat.RGBA32,false,true);flat.SetPixel(0,0,new Color(.5f,.5f,1,.5f));flat.Apply();
            stone.SetTexture("_BumpMap",flat);stone.SetFloat("_Glossiness",0);stone.SetColor("_EmissionColor",Color.black);stone.DisableKeyword("_EMISSION");
            stone.SetFloat("_ValueNoise",0);stone.SetFloat("_ValueNoiseVertex",0);stone.SetFloat("_AddRain",0);stone.SetFloat("_RippleDistance",0);stone.SetFloat("_SpecularHighlights",0);stone.SetFloat("_GlossyReflections",0);
            var paint=(Texture2D)stone.mainTexture;var glowPixels=paint.GetPixels32();
            for(int i=0;i<glowPixels.Length;i++){var c=glowPixels[i];if(!(c.r>180 && c.r-c.b>100 && c.g-c.b>35))glowPixels[i]=new Color32(0,0,0,255);}
            var glow=new Texture2D(paint.width,paint.height,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};glow.SetPixels32(glowPixels);glow.Apply();
            stone.SetTexture("_EmissionMap",glow);stone.SetColor("_EmissionColor",new Color(1.2f,1.2f,1.2f,1));stone.EnableKeyword("_EMISSION");
            var recess=new Material(stone){name="Atlas painted carved stone"};
            var ember=new Material(stone){name="Atlas painted orange insets"};
            var masonry=new Material(stone){name="Atlas painted masonry"};masonry.mainTexture=PaintedStone("waystone-masonry");masonry.mainTexture.wrapMode=TextureWrapMode.Repeat;masonry.SetColor("_EmissionColor",Color.black);masonry.DisableKeyword("_EMISSION");
            WaystonePart[] parts;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("PortalAtlas.waystone.mesh"))parts=WaystoneModel.Read(stream);
            foreach(var part in parts)
            {
                var mesh=new Mesh{name="Atlas waystone "+part.name};mesh.vertices=part.vertices;mesh.uv=part.uv;mesh.triangles=part.triangles;
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                var go=new GameObject("AtlasWaystone_"+part.name);go.layer=prefab.layer;go.transform.SetParent(prefab.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial=part.name=="masonry"?masonry:part.name=="orange"?ember:part.name=="recess"?recess:stone;
            }
            Collider(prefab,new Vector3(0,.035f,0),new Vector3(.75f,.07f,.75f));
            Collider(prefab,new Vector3(0,.135f,0),new Vector3(.69f,.13f,.69f));
            Collider(prefab,new Vector3(0,.25f,0),new Vector3(.60f,.10f,.60f));
            Collider(prefab,new Vector3(0,.50f,0),new Vector3(.526f,.40f,.526f));
            Collider(prefab,new Vector3(0,.735f,0),new Vector3(.62f,.075f,.62f));
            Collider(prefab,new Vector3(0,1.08f,0),new Vector3(.44f,.62f,.44f));
            Collider(prefab,new Vector3(0,1.445f,0),new Vector3(.49f,.11f,.49f));
            // The base stone pillar supplied snap points. Remove them entirely:
            // native placement then has no automatic or manual snap candidates,
            // and other pieces cannot snap themselves onto this decoration.
            foreach(var child in prefab.GetComponentsInChildren<Transform>(true))
                if(child && child!=prefab.transform && child.CompareTag("snappoint"))Object.DestroyImmediate(child.gameObject);
            try{AddCrownWisp(prefab);}catch(Exception e){Debug.LogWarning("[Portal Atlas] Crown wisp could not render: "+e);}
            Light(prefab,"AtlasWindowGlow",new Vector3(0,.94f,0),1.2f,.45f);
            Light(prefab,"AtlasBaseGlow",new Vector3(0,.18f,0),.75f,.25f);
            Debug.Log("[Portal Atlas] Approved waystone built: 1.50 m shoulder height, 0.75 m footprint; native wisp crown and orange insets.");
        }
        private static void Collider(GameObject parent,Vector3 center,Vector3 size)
        {var c=parent.AddComponent<BoxCollider>();c.center=center;c.size=size;}
        private static void Light(GameObject parent,string name,Vector3 position,float range,float intensity)
        {var go=new GameObject(name);go.transform.SetParent(parent.transform,false);go.transform.localPosition=position;var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.33f,.05f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;}
        private static Texture2D PaintedStone(string resource="waystone-painted")
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Atlas approved painted finish",wrapMode=TextureWrapMode.Clamp};
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("PortalAtlas."+resource+".png"))
            using(var memory=new MemoryStream()){stream.CopyTo(memory);ImageConversion.LoadImage(texture,memory.ToArray());}
            var pixels=texture.GetPixels32();
            for(int i=0;i<pixels.Length;i++)pixels[i]=WaystoneColours.StoneGrey(pixels[i]);
            texture.SetPixels32(pixels);texture.Apply();
            return texture;
        }
        private static void AddCrownWisp(GameObject parent)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var item=PrefabManager.Instance.GetPrefab("Demister");
            if(!item && ObjectDB.instance)item=ObjectDB.instance.GetItemPrefab("Demister");
            var effect=item?item.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect as SE_Demister:null;
            var source=effect?effect.m_ballPrefab:PrefabManager.Instance.GetPrefab("demister_ball");
            if(!source)throw new InvalidOperationException("Native wisplight visual prefab is unavailable");
            var host=new GameObject("AtlasCrownWisp");host.SetActive(false);host.transform.SetParent(parent.transform,false);host.transform.localPosition=new Vector3(0,WaystoneModel.CrownWispHeight,0);
            // Copy only the authored core, small sparks and halo, never the parent
            // effects hierarchy (which includes a screen-distortion particle effect).
            CopyWispVisual(source.transform.Find("demister_ball"),host.transform,"AtlasWispCore");
            var sparks=CopyWispVisual(source.transform.Find("effects/flame/sparcs_front"),host.transform,"AtlasWispSparks");
            if(sparks)ConfigureSparks(sparks);
            CopyWispVisual(source.transform.Find("effects/flame/flare"),host.transform,"AtlasWispHalo");
            var light=host.AddComponent<Light>();light.color=new Color(.35f,.8f,1);light.range=.8f;light.intensity=.55f;light.shadows=LightShadows.None;
            host.AddComponent<AtlasCrownWispBob>();host.SetActive(true);
        }
        private static GameObject CopyWispVisual(Transform source,Transform parent,string name)
        {
            if(!source)return null;
            var clone=Object.Instantiate(source.gameObject,parent,false);clone.name=name;
            clone.transform.localRotation=Quaternion.identity;clone.transform.localScale=source.localScale*WaystoneModel.SparkCloneScale;
            var mesh=clone.GetComponent<MeshFilter>();
            clone.transform.localPosition=mesh && mesh.sharedMesh?-Vector3.Scale(mesh.sharedMesh.bounds.center,clone.transform.localScale):Vector3.zero;
            foreach(var script in clone.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var c in clone.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var body in clone.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
            foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(materials[i])
                {
                    materials[i]=new Material(materials[i]);materials[i].DisableKeyword("_DISTORTION_ON");
                    foreach(var property in new[]{"_DistortionEnabled","_DistortionStrength","_DistortionBlend"})
                        if(materials[i].HasProperty(property))materials[i].SetFloat(property,0);
                    if(materials[i].shader && materials[i].shader.name.ToLowerInvariant().Contains("distort"))renderer.enabled=false;
                }
                renderer.sharedMaterials=materials;
            }
            clone.SetActive(true);return clone;
        }
        private static void ConfigureSparks(GameObject sparks)
        {
            foreach(var particles in sparks.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;main.loop=true;main.playOnAwake=true;main.simulationSpace=ParticleSystemSimulationSpace.Local;
                main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.startColor=Color.white;main.startLifetime=new ParticleSystem.MinMaxCurve(WaystoneModel.SparkLifetimeMin,WaystoneModel.SparkLifetimeMax);main.startSize=new ParticleSystem.MinMaxCurve(.012f,.022f);main.startSpeed=new ParticleSystem.MinMaxCurve(WaystoneModel.SparkSpeedMin,WaystoneModel.SparkSpeedMax);main.gravityModifier=0;main.maxParticles=12;
                var emission=particles.emission;emission.enabled=true;emission.rateOverTime=5;emission.rateOverDistance=0;
                var shape=particles.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=WaystoneModel.SparkSpawnRadius;shape.position=Vector3.zero;shape.scale=Vector3.one;
                var noise=particles.noise;noise.enabled=false;
                var colour=particles.colorOverLifetime;colour.enabled=true;
                var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.85f,.70f),new GradientAlphaKey(0,1)});colour.color=fade;
                var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.25f));
                var forces=particles.externalForces;forces.enabled=false;
                var velocity=particles.velocityOverLifetime;velocity.enabled=false;
                var inheritance=particles.inheritVelocity;inheritance.enabled=false;
                particles.Play(true);
            }
            foreach(var renderer in sparks.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
            {
                var white=new Color(1.5f,1.5f,1.5f,1);
                foreach(var property in new[]{"_Color","_TintColor","_EmissionColor"})if(material.HasProperty(property))material.SetColor(property,white);
            }
        }
    }
    internal sealed class AtlasCrownWispBob:MonoBehaviour
    {
        private void Update(){transform.localPosition=WaystoneModel.CrownPosition(Mathf.Sin(Time.time*1.4f)*.02f);}
    }
}
