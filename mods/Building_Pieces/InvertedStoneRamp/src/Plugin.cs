using System;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace OctoberJemini.InvertedStoneRamp
{
    [BepInPlugin(Guid, "Inverted Stone Ramp", "1.1.2")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "OctoberJemini.InvertedStoneRamp";
        private const string SourceName = "stone_wall_2x1";
        private const string PrefabName = "oct_stone_ramp_2x1_inverted";
        private GameObject ramp;
        private bool iconRendered;

        private void Awake()
        {
            PrefabManager.OnVanillaPrefabsAvailable += RegisterPiece;
            PieceManager.OnPiecesRegistered += RenderIcon;
        }

        private void RegisterPiece()
        {
            if (ramp) return;
            try
            {
                var source = PrefabManager.Instance.GetPrefab(SourceName);
                if (!source) throw new InvalidOperationException("Vanilla stone wall unavailable.");
                // Use the normal vanilla stone wall's new/worn/broken materials directly.
                var sourceWear = source.GetComponent<WearNTear>();
                var material = StoneMaterial(sourceWear.m_new, null);
                if (!material) throw new InvalidOperationException("Vanilla stone material unavailable.");
                var wornMaterial = StoneMaterial(sourceWear.m_worn, material);
                var brokenMaterial = StoneMaterial(sourceWear.m_broken, material);
                var clone = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source);
                for (int index = clone.transform.childCount - 1; index >= 0; index--)
                    DestroyImmediate(clone.transform.GetChild(index).gameObject);
                foreach (var collider in clone.GetComponents<Collider>()) DestroyImmediate(collider);
                foreach (var renderer in clone.GetComponents<Renderer>()) DestroyImmediate(renderer);
                foreach (var filter in clone.GetComponents<MeshFilter>()) DestroyImmediate(filter);
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;
                var mesh = CreateWedge();
                var wear = clone.GetComponent<WearNTear>();
                wear.m_new = Visual(clone, "new", mesh, material);
                wear.m_worn = Visual(clone, "worn", mesh, wornMaterial);
                wear.m_broken = Visual(clone, "broken", mesh, brokenMaterial);
                wear.m_worn.SetActive(false);
                wear.m_broken.SetActive(false);
                wear.m_fragmentRoots = new[] { wear.m_new };
                wear.m_comOffset = mesh.bounds.center;
                var collision = clone.AddComponent<MeshCollider>();
                collision.sharedMesh = mesh;
                collision.convex = true;
                // Match the original inverted prefab's six corner mounts and two side midpoints.
                foreach (float x in new[] { -.5f, .5f })
                    foreach (var mount in new[] {
                        new Vector3(x, -.5f, -1f), new Vector3(x, .5f, -1f),
                        new Vector3(x, .5f, 1f), new Vector3(x, 0f, 0f) }) {
                        var snap = new GameObject("Snap");
                        snap.tag = "snappoint";
                        snap.transform.SetParent(clone.transform, false);
                        snap.transform.localPosition = mount;
                    }
                var config = new PieceConfig
                {
                    Name = "Inverted Stone Ramp 2x1",
                    Description = "A stone wedge with a flat top and sloped underside.",
                    PieceTable = "Hammer",
                    Category = "BuildingStonecutter",
                    Usage = new[] { "Building", "Wall" },
                    CraftingStation = "piece_stonecutter",
                    AllowedInDungeons = false,
                    Requirements = new[] { new RequirementConfig { Item = "Stone", Amount = 2, Recover = true } }
                };
                if (!PieceManager.Instance.AddPiece(new CustomPiece(clone, false, config)))
                    throw new InvalidOperationException("Could not register inverted ramp.");
                ramp = clone;
                PrefabManager.OnVanillaPrefabsAvailable -= RegisterPiece;
                Logger.LogInfo("Registered Inverted Stone Ramp 2x1: 2 Stone, Stonecutter, separate Hammer piece.");
            }
            catch (Exception ex) { Logger.LogError("Inverted ramp registration failed: " + ex); }
        }

        private static Material StoneMaterial(GameObject state, Material fallback)
        {
            if (!state) return fallback;
            var renderer = state.GetComponentInChildren<MeshRenderer>(true);
            return renderer && renderer.sharedMaterial ? renderer.sharedMaterial : fallback;
        }

        private static GameObject Visual(GameObject parent, string name, Mesh mesh, Material material)
        {
            var visual = new GameObject(name);
            visual.layer = parent.layer;
            visual.transform.SetParent(parent.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial = material;
            return visual;
        }

        private static Mesh CreateWedge()
        {
            var corners = new[] {
                new Vector3(-1f, -.5f, -.5f), new Vector3(-1f, .5f, -.5f),
                new Vector3(1f, .5f, -.5f), new Vector3(-1f, -.5f, .5f),
                new Vector3(-1f, .5f, .5f), new Vector3(1f, .5f, .5f)
            };
            // Closed triangular prism. Each face has independent vertices for crisp normals.
            var faces = new[] { 0,1,2, 3,5,4, 0,3,4, 0,4,1,
                                1,4,5, 1,5,2, 0,2,5, 0,5,3 };
            var vertices = new Vector3[faces.Length];
            var uvs = new Vector2[faces.Length];
            var colors = new Color[faces.Length];
            var triangles = new int[faces.Length];
            for (int index = 0; index < faces.Length; index++) {
                var point = corners[faces[index]];
                // Map texture coordinates in authored space, before the historical prefab rotation.
                // Keep every face inside the solid lower-left stone atlas panel.
                // Horizontal and sloped faces use X/Z; vertical ends use Z/Y.
                float u = index < 6 || index >= 12 ? (point.x + 1f) * .225f : (point.z + .5f) * .45f;
                float v = index < 12 ? (point.y + .5f) * .225f : (point.z + .5f) * .225f;
                uvs[index] = new Vector2(.025f + u, .025f + v);
                // Original prefab includes a +90 Y child rotation before the old mod's 180 Z flip.
                // Inverted wedge must span local Z, with the full-height end at -Z.
                // Bake this into the mesh so saved root rotations remain untouched.
                vertices[index] = new Vector3(-point.z, point.y, point.x);
                colors[index] = Color.white;
                triangles[index] = index;
            }
            var mesh = new Mesh { name = "OCT_InvertedStoneWedge", vertices = vertices,
                triangles = triangles, uv = uvs, colors = colors };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void RenderIcon()
        {
            if (!ramp || iconRendered || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            try {
                using (var stream = typeof(Plugin).Assembly.GetManifestResourceStream("inverted-ramp-icon.png"))
                using (var buffer = new System.IO.MemoryStream()) {
                    if (stream == null) throw new InvalidOperationException("Inverted wedge icon missing.");
                    stream.CopyTo(buffer);
                    var texture = AssetUtils.LoadImage(buffer.ToArray());
                    texture.filterMode = FilterMode.Bilinear;
                    ramp.GetComponent<Piece>().m_icon = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                }
                iconRendered = true;
            } catch (Exception ex) { Logger.LogWarning("Inverted ramp thumbnail: " + ex.Message); }
        }

        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterPiece;
            PieceManager.OnPiecesRegistered -= RenderIcon;
        }
    }
}
