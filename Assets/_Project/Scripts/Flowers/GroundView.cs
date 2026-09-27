using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// Ground prefab with a tile-coloured top surface. The tile colours live in a tiny
    /// texture (one texel per tile) on the surface material, so the whole grid is one
    /// renderer and one SRP-Batcher-friendly material. Bilinear filtering blends
    /// neighbouring tiles into soft grass edges.
    /// </summary>
    public sealed class GroundView : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Flat 1x1 quad on top; its UV u runs along +X and v along +Z.")]
        [SerializeField] private Renderer surface;

        private Texture2D tiles;
        private Material surfaceMaterial;

        public Texture2D Tiles => tiles;

        /// <summary>Creates the tile texture; texel (x, y) is tile column x from -X and row y from -Z.</summary>
        public void InitTiles(Vector2Int grid)
        {
            if (surface == null)
            {
                Debug.LogError($"Ground '{name}' has no surface renderer.", this);
                return;
            }
            tiles = new Texture2D(Mathf.Max(1, grid.x), Mathf.Max(1, grid.y), TextureFormat.RGBA32, false)
            {
                name = "GroundTiles",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            surfaceMaterial = new Material(surface.sharedMaterial) { name = "GroundSurface (runtime)" };
            surfaceMaterial.SetTexture(BaseMapId, tiles);
            surfaceMaterial.SetColor(BaseColorId, Color.white);
            surface.sharedMaterial = surfaceMaterial;
        }

        /// <summary>Uploads one colour per tile, row by row from the -Z edge.</summary>
        public void SetTiles(Color32[] colors)
        {
            if (tiles == null)
                return;
            tiles.SetPixels32(colors);
            tiles.Apply(false);
        }

        private void OnDestroy()
        {
            if (surfaceMaterial != null)
                Destroy(surfaceMaterial);
            if (tiles != null)
                Destroy(tiles);
        }
    }
}
