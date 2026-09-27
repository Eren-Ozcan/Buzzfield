using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Visual part of the hive prefab; marks where bees deposit nectar.</summary>
    public sealed class HiveView : MonoBehaviour
    {
        [SerializeField] private Transform entrance;

        public Vector3 EntrancePoint => entrance != null ? entrance.position : transform.position;
    }
}
