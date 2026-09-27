using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Objects created for one garden, removed together when the Queen moves.</summary>
    public readonly struct GardenInstance
    {
        public readonly Transform Root;
        public readonly HiveView Hive;
        public readonly GroundView Ground;
        public readonly Bounds Bounds;

        public GardenInstance(Transform root, HiveView hive, GroundView ground, Bounds bounds)
        {
            Root = root;
            Hive = hive;
            Ground = ground;
            Bounds = bounds;
        }
    }

    /// <summary>Builds the ground and hive of a garden from its config. Flowers come from <see cref="FlowerManager"/>.</summary>
    public static class GardenSpawner
    {
        /// <summary>Height of the box the camera keeps in view (tallest flower or hive plus bees).</summary>
        const float ViewHeight = 2f;

        public static GardenInstance Spawn(GardenConfig garden, Transform parent)
        {
            var root = new GameObject($"Garden_{garden.name}").transform;
            root.SetParent(parent, false);

            Vector2 size = garden.GroundSize;
            GroundView ground = null;
            if (garden.GroundView != null)
            {
                // The ground prefab is 1x1 world units, so its scale is the garden size.
                ground = Object.Instantiate(garden.GroundView, root);
                ground.name = "Ground";
                ground.transform.localScale = new Vector3(size.x, ground.transform.localScale.y, size.y);
                ground.InitTiles(garden.TileGrid);
            }
            else
            {
                Debug.LogError($"Garden '{garden.name}' has no ground prefab.", garden);
            }

            HiveView hive = null;
            if (garden.HivePrefab != null)
            {
                hive = Object.Instantiate(garden.HivePrefab, GardenConfig.ToWorld(garden.HivePosition), Quaternion.identity, root);
                hive.name = "Hive";
            }
            else
            {
                Debug.LogError($"Garden '{garden.name}' has no hive prefab.", garden);
            }

            var bounds = new Bounds(new Vector3(0f, ViewHeight * 0.5f, 0f), new Vector3(size.x, ViewHeight, size.y));
            return new GardenInstance(root, hive, ground, bounds);
        }
    }
}
