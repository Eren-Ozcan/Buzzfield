using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Core
{
    /// <summary>
    /// Reuses instances of one prefab. Released objects are deactivated and kept under
    /// the pool parent, so spawning at runtime does not allocate once the pool is warm.
    /// </summary>
    public sealed class PrefabPool
    {
        readonly GameObject prefab;
        readonly Transform parent;
        readonly Stack<GameObject> free;

        public PrefabPool(GameObject prefab, Transform parent, int prewarm)
        {
            this.prefab = prefab;
            this.parent = parent;
            free = new Stack<GameObject>(Mathf.Max(prewarm, 4));
            for (int i = 0; i < prewarm; i++)
                free.Push(Create());
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject instance = free.Count > 0 ? free.Pop() : Create();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Release(GameObject instance)
        {
            instance.SetActive(false);
            free.Push(instance);
        }

        GameObject Create()
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.SetActive(false);
            return instance;
        }
    }
}
