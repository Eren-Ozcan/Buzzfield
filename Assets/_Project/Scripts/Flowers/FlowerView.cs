using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Visual part of a flower prefab. Has no Update; FlowerManager and GardenBloomManager drive it.</summary>
    public sealed class FlowerView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Point bees fly to when collecting.")]
        [SerializeField] private Transform nectarPoint;
        [SerializeField] private Renderer headRenderer;

        // Own material copy instead of a MaterialPropertyBlock: property blocks opt the
        // renderer out of the SRP Batcher, while a material per flower stays batched.
        private Material headMaterial;
        private Vector3 baseScale = Vector3.one;

        public Vector3 NectarPoint => nectarPoint != null ? nectarPoint.position : transform.position;
        public Renderer HeadRenderer => headRenderer;

        /// <summary>Called once after spawning; creates the head material this flower tints.</summary>
        public void InitVisuals(Color headColor)
        {
            baseScale = transform.localScale;
            if (headRenderer != null && headMaterial == null)
            {
                headMaterial = new Material(headRenderer.sharedMaterial) { name = $"{headRenderer.sharedMaterial.name} ({name})" };
                headRenderer.sharedMaterial = headMaterial;
            }
            SetHeadColor(headColor);
        }

        public void SetHeadColor(Color color)
        {
            if (headMaterial != null)
                headMaterial.SetColor(BaseColorId, color);
        }

        /// <summary>Scale relative to the prefab's own scale (1 = normal).</summary>
        public void SetScale(float scale) => transform.localScale = baseScale * scale;

        private void OnDestroy()
        {
            if (headMaterial != null)
                Destroy(headMaterial);
        }
    }
}
