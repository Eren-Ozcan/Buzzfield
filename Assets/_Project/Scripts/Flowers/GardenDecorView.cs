using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// A garden's decor, merged into one mesh per material so a hundred grass tufts cost one
    /// draw call. Tinted decor turns from dry to its own colour as the garden blooms
    /// (<see cref="SetBloom"/>). Built by <see cref="GardenSpawner"/>; owns its meshes and materials.
    /// </summary>
    public sealed class GardenDecorView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> tinted = new List<Material>();
        private readonly List<Color> tintedColors = new List<Color>();
        private readonly List<Color> dryColors = new List<Color>();

        public int PieceCount { get; private set; }
        public int RendererCount => meshes.Count;

        /// <summary>Lays out and merges the garden's decor under <paramref name="parent"/>; null when the garden has none.</summary>
        public static GardenDecorView Build(GardenConfig garden, Transform parent)
        {
            if (garden.Decor == null)
                return null;
            var root = new GameObject("Decor");
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<GardenDecorView>();
            view.Merge(DecorLayout.Place(garden), garden.Decor);
            return view;
        }

        /// <summary>0 = dry, 1 = every tinted piece in its own colour.</summary>
        public void SetBloom(float fraction)
        {
            for (int i = 0; i < tinted.Count; i++)
                tinted[i].SetColor(BaseColorId, Color.Lerp(dryColors[i], tintedColors[i], fraction));
        }

        private void Merge(List<DecorPiece> pieces, GardenDecor decor)
        {
            PieceCount = pieces.Count;
            var groups = new Dictionary<(Material material, bool tinted), List<CombineInstance>>();
            var filters = new List<MeshFilter>();
            for (int p = 0; p < pieces.Count; p++)
            {
                DecorPiece piece = pieces[p];
                Matrix4x4 placement = Matrix4x4.TRS(piece.Position, piece.Rotation, piece.Scale);
                Transform prefabRoot = piece.Prefab.transform;
                piece.Prefab.GetComponentsInChildren(true, filters);
                for (int f = 0; f < filters.Count; f++)
                {
                    MeshFilter filter = filters[f];
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer == null || renderer.sharedMaterial == null || filter.sharedMesh == null)
                        continue;
                    var key = (renderer.sharedMaterial, piece.Tinted);
                    if (!groups.TryGetValue(key, out List<CombineInstance> list))
                        groups[key] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        transform = placement * prefabRoot.worldToLocalMatrix * filter.transform.localToWorldMatrix,
                    });
                }
            }

            foreach (KeyValuePair<(Material material, bool tinted), List<CombineInstance>> group in groups)
            {
                Material material = group.Key.material;
                var mesh = new Mesh { name = $"Decor_{material.name}" };
                int vertexCount = 0;
                for (int i = 0; i < group.Value.Count; i++)
                    vertexCount += group.Value[i].mesh.vertexCount;
                if (vertexCount > 65535)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                meshes.Add(mesh);

                if (group.Key.tinted)
                {
                    Color own = material.GetColor(BaseColorId);
                    tintedColors.Add(own);
                    dryColors.Add(decor.Dry(own));
                    material = new Material(material) { name = $"{material.name} (decor tint)" };
                    tinted.Add(material);
                }

                var child = new GameObject(mesh.name);
                child.transform.SetParent(transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = child.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            SetBloom(0f);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < meshes.Count; i++)
                Destroy(meshes[i]);
            for (int i = 0; i < tinted.Count; i++)
                Destroy(tinted[i]);
        }
    }
}
