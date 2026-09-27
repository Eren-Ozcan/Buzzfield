using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using Buzzfield.UI;
using Buzzfield.Upgrades;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Composition root: owns the init order, wires the managers together and drives
    /// the per-frame ticks in a fixed order (flowers regenerate before bees read them).
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private EconomySettings economySettings;
        [SerializeField] private BeeSettings beeSettings;
        [SerializeField] private BloomSettings bloomSettings;
        [SerializeField] private PrestigeSettings prestigeSettings;
        [SerializeField] private UpgradeDefinition addBeeUpgrade;
        [SerializeField] private UpgradeDefinition speedUpgrade;
        [SerializeField] private UpgradeDefinition honeyValueUpgrade;

        [Header("Scene")]
        [SerializeField] private FlowerManager flowerManager;
        [SerializeField] private BeeManager beeManager;
        [SerializeField] private HudView hud;
        [SerializeField] private BottomBarView bottomBar;
        [SerializeField] private GardenCompleteView gardenComplete;
        [SerializeField] private CameraFitter cameraFitter;
        [SerializeField] private Transform worldRoot;

        private EconomyManager economy;
        private UpgradeManager upgrades;
        private GardenBloomManager bloom;
        private GardenInstance garden;

        public EconomyManager Economy => economy;
        public UpgradeManager Upgrades => upgrades;
        public BeeManager Bees => beeManager;
        public FlowerManager Flowers => flowerManager;
        public GardenBloomManager Bloom => bloom;

        private void Awake()
        {
            Application.targetFrameRate = gameSettings.TargetFrameRate;

            economy = new EconomyManager(economySettings, Time.timeAsDouble);
            beeManager.Init(beeSettings, flowerManager);
            beeManager.OnNectarDeposited += HandleNectarDeposited;
            upgrades = new UpgradeManager(addBeeUpgrade, speedUpgrade, honeyValueUpgrade, beeSettings, economy, beeManager);
            hud.Init(economy, economySettings);
            bottomBar.Init(upgrades, economy, beeManager, beeSettings);
            bloom = new GardenBloomManager(bloomSettings, flowerManager);
            bloom.OnBloomChanged += HandleBloomChanged;
            bloom.OnGardenCompleted += HandleGardenCompleted;

            LoadGarden(0);
            for (int i = 0; i < beeSettings.StartingBees; i++)
                beeManager.Spawn(0);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            flowerManager.Tick(deltaTime);
            beeManager.Tick(deltaTime);
            bloom.Tick(deltaTime);
        }

        private void OnDestroy()
        {
            if (beeManager != null)
                beeManager.OnNectarDeposited -= HandleNectarDeposited;
            if (bloom != null)
            {
                bloom.OnBloomChanged -= HandleBloomChanged;
                bloom.OnGardenCompleted -= HandleGardenCompleted;
                bloom.Dispose();
            }
        }

        private void LoadGarden(int index)
        {
            if (garden.Root != null)
                Destroy(garden.Root.gameObject);

            GardenConfig config = prestigeSettings.Gardens[Mathf.Min(index, prestigeSettings.Gardens.Count - 1)];
            garden = GardenSpawner.Spawn(config, worldRoot);
            flowerManager.Init(config, garden.Root);
            bloom.Load(config, garden);
            if (garden.Hive != null)
                beeManager.SetHive(garden.Hive.EntrancePoint);
            cameraFitter.Fit(garden.Bounds);
        }

        private void HandleNectarDeposited(double nectar, double flowerValue)
        {
            economy.Deposit(nectar, flowerValue, Time.timeAsDouble);
        }

        private void HandleBloomChanged() => hud.ShowBloom(bloom.Percent, bloom.Fraction);

        private void HandleGardenCompleted() => gardenComplete.Play();
    }
}
