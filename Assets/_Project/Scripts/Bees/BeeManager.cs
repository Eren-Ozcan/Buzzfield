using System;
using System.Collections.Generic;
using Buzzfield.Core;
using Buzzfield.Flowers;
using UnityEngine;

namespace Buzzfield.Bees
{
    /// <summary>
    /// Spawns, pools and moves every bee. Bees have no Update of their own: the game
    /// calls <see cref="Tick"/> once per frame and this loops over all of them.
    /// State flow: Idle, FlyToFlower, Collecting, ReturnToHive, Depositing, Idle.
    /// </summary>
    public sealed class BeeManager : MonoBehaviour
    {
        private const float ArriveDistance = 0.02f;

        private readonly List<Bee> bees = new List<Bee>(160);
        private readonly List<int> tierCounts = new List<int>(4);
        private PrefabPool[] pools;
        private BeeSettings settings;
        private FlowerManager flowers;
        private Vector3 hivePoint;
        private float time;

        public IReadOnlyList<Bee> Bees => bees;
        public int Count => bees.Count;
        public bool IsAtCap => bees.Count >= settings.MaxBees;

        /// <summary>Global flight speed factor from upgrades, the Queen and boosts.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Raised when a bee unloads at the hive: nectar units and honey value per unit.</summary>
        public event Action<double, double> OnNectarDeposited;

        /// <summary>Raised when bees are added or removed.</summary>
        public event Action OnBeesChanged;

        public void Init(BeeSettings beeSettings, FlowerManager flowerManager)
        {
            settings = beeSettings;
            flowers = flowerManager;

            var root = new GameObject("BeePool").transform;
            root.SetParent(transform, false);
            IReadOnlyList<BeeTier> tiers = settings.Tiers;
            pools = new PrefabPool[tiers.Count];
            for (int i = 0; i < tiers.Count; i++)
            {
                tierCounts.Add(0);
                if (tiers[i] != null && tiers[i].Prefab != null)
                    pools[i] = new PrefabPool(tiers[i].Prefab, root, settings.PoolPrewarm);
                else
                    Debug.LogError($"Bee tier {i} has no prefab.", settings);
            }
        }

        /// <summary>Sets the deposit point; call when a garden is spawned.</summary>
        public void SetHive(Vector3 entrancePoint)
        {
            hivePoint = entrancePoint;
        }

        public int CountOfTier(int tierIndex) => tierCounts[tierIndex];

        /// <summary>Spawns a bee of the given tier at the hive. Returns null at the cap.</summary>
        public Bee Spawn(int tierIndex)
        {
            if (IsAtCap || pools[tierIndex] == null)
                return null;

            BeeTier tier = settings.Tiers[tierIndex];
            GameObject instance = pools[tierIndex].Get(hivePoint, Quaternion.identity);
            instance.transform.localScale = Vector3.one * tier.Scale;
            if (tier.Material != null)
            {
                Renderer body = instance.GetComponentInChildren<Renderer>();
                if (body != null)
                    body.sharedMaterial = tier.Material;
            }

            var bee = new Bee(tierIndex, tier, instance, hivePoint, UnityEngine.Random.value * Mathf.PI * 2f);
            bees.Add(bee);
            tierCounts[tierIndex]++;
            OnBeesChanged?.Invoke();
            return bee;
        }

        /// <summary>Returns every bee to the pool, releasing flower reservations.</summary>
        public void DespawnAll()
        {
            for (int i = bees.Count - 1; i >= 0; i--)
                Recycle(bees[i]);
            bees.Clear();
            for (int i = 0; i < tierCounts.Count; i++)
                tierCounts[i] = 0;
            OnBeesChanged?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            time += deltaTime;
            for (int i = 0; i < bees.Count; i++)
                TickBee(bees[i], deltaTime);
        }

        private void TickBee(Bee bee, float deltaTime)
        {
            float step = bee.Tier.Speed * SpeedMultiplier * deltaTime;
            switch (bee.State)
            {
                case BeeState.Idle:
                    bee.Timer -= deltaTime;
                    if (bee.Timer <= 0f)
                    {
                        if (TryTarget(bee))
                            break;
                        bee.Timer = settings.RetryInterval;
                        bee.HoverPoint = RandomHoverPoint();
                    }
                    MoveTowards(bee, bee.HoverPoint, step * 0.4f, deltaTime);
                    break;

                case BeeState.FlyToFlower:
                    if (!bee.Target.HasNectar)
                    {
                        // Someone else emptied it on the way; pick another one this frame.
                        flowers.Release(bee.Target);
                        bee.Target = null;
                        bee.State = BeeState.Idle;
                        bee.Timer = 0f;
                        bee.HoverPoint = bee.Position;
                        break;
                    }
                    if (MoveTowards(bee, bee.Target.NectarPoint, step, deltaTime))
                    {
                        flowers.Release(bee.Target);
                        bee.State = BeeState.Collecting;
                        bee.Timer = bee.Tier.CollectDuration;
                    }
                    break;

                case BeeState.Collecting:
                    bee.Timer -= deltaTime;
                    ApplyTransform(bee);
                    if (bee.Timer > 0f)
                        break;
                    bee.Carried = flowers.Collect(bee.Target, bee.Tier.Capacity);
                    bee.CarriedValue = bee.Target.Value;
                    bee.Target = null;
                    if (bee.Carried > 0)
                    {
                        bee.State = BeeState.ReturnToHive;
                    }
                    else
                    {
                        bee.State = BeeState.Idle;
                        bee.Timer = 0f;
                        bee.HoverPoint = bee.Position;
                    }
                    break;

                case BeeState.ReturnToHive:
                    if (MoveTowards(bee, hivePoint, step, deltaTime))
                    {
                        bee.State = BeeState.Depositing;
                        bee.Timer = settings.DepositDuration;
                    }
                    break;

                case BeeState.Depositing:
                    bee.Timer -= deltaTime;
                    ApplyTransform(bee);
                    if (bee.Timer > 0f)
                        break;
                    double nectar = bee.Carried;
                    bee.Carried = 0;
                    bee.State = BeeState.Idle;
                    bee.Timer = 0f;
                    bee.HoverPoint = bee.Position;
                    OnNectarDeposited?.Invoke(nectar, bee.CarriedValue);
                    break;
            }
        }

        private bool TryTarget(Bee bee)
        {
            Flower flower = flowers.FindNearestAvailable(bee.Position);
            if (flower == null)
                return false;
            flowers.Reserve(flower);
            bee.Target = flower;
            bee.State = BeeState.FlyToFlower;
            return true;
        }

        /// <summary>Moves the flight position; returns true on arrival.</summary>
        private bool MoveTowards(Bee bee, Vector3 target, float step, float deltaTime)
        {
            Vector3 delta = target - bee.Position;
            bee.Position = Vector3.MoveTowards(bee.Position, target, step);

            // Face the flight direction on the horizontal plane, so bees do not nose-dive.
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                bee.Rotation = Quaternion.RotateTowards(bee.Rotation, Quaternion.LookRotation(delta), settings.TurnSpeed * deltaTime);

            ApplyTransform(bee);
            return (target - bee.Position).sqrMagnitude <= ArriveDistance * ArriveDistance;
        }

        private void ApplyTransform(Bee bee)
        {
            float bob = Mathf.Sin(time * settings.BobFrequency + bee.BobPhase) * settings.BobAmplitude;
            bee.Transform.SetPositionAndRotation(bee.Position + new Vector3(0f, bob, 0f), bee.Rotation);
        }

        private Vector3 RandomHoverPoint()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * settings.HoverRadius;
            return hivePoint + new Vector3(offset.x, 0.3f, offset.y);
        }

        private void Recycle(Bee bee)
        {
            if (bee.Target != null && bee.State == BeeState.FlyToFlower)
                flowers.Release(bee.Target);
            bee.Target = null;
            pools[bee.TierIndex].Release(bee.Instance);
        }
    }
}
