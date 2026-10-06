using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// The land around a garden (meadow, hills, trees). It starts a little faded and reaches its
    /// full colours as the garden blooms (<see cref="SetBloom"/>). Spawned with the garden by
    /// <see cref="GardenSpawner"/>; tints its own copies of the materials.
    /// </summary>
    public sealed class SceneryView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] tinted;
        [Tooltip("Multiplies the scenery colours while the garden has not bloomed at all.")]
        [SerializeField] private Color fadedTint = new Color(0.86f, 0.86f, 0.8f);

        private Material[] materials;

        /// <summary>0 = faded, 1 = full colour.</summary>
        public void SetBloom(float fraction)
        {
            if (materials == null)
                CopyMaterials();
            Color tint = Color.Lerp(fadedTint, Color.white, fraction);
            for (int i = 0; i < materials.Length; i++)
                materials[i].SetColor(BaseColorId, tint);
        }

        private void CopyMaterials()
        {
            materials = new Material[tinted.Length];
            for (int i = 0; i < tinted.Length; i++)
            {
                materials[i] = new Material(tinted[i].sharedMaterial) { name = $"{tinted[i].sharedMaterial.name} (scenery tint)" };
                tinted[i].sharedMaterial = materials[i];
            }
        }

        private void OnDestroy()
        {
            if (materials == null)
                return;
            for (int i = 0; i < materials.Length; i++)
                Destroy(materials[i]);
        }
    }
}
