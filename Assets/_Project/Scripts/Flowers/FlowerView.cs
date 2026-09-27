using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Visual part of a flower prefab. Has no Update; FlowerManager drives it.</summary>
    public sealed class FlowerView : MonoBehaviour
    {
        [Tooltip("Point bees fly to when collecting.")]
        [SerializeField] private Transform nectarPoint;
        [SerializeField] private Renderer headRenderer;

        public Vector3 NectarPoint => nectarPoint != null ? nectarPoint.position : transform.position;
        public Renderer HeadRenderer => headRenderer;
    }
}
