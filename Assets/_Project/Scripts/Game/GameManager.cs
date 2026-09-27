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
        [SerializeField] private QueenPanelView queenPanel;
        [SerializeField] private CameraFitter cameraFitter;
        [SerializeField] private Transform worldRoot;

        private EconomyManager economy;
        private UpgradeManager upgrades;
        private GardenBloomManager bloom;
        private PrestigeManager prestige;
        private GardenInstance garden;

        public EconomyManager Economy => economy;
        public UpgradeManager Upgrades => upgrades;
        public BeeManager Bees => beeManager;
        public FlowerManager Flowers => flowerManager;
        public GardenBloomManager Bloom => bloom;
        public PrestigeManager Prestige => prestige;

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
            prestige = new PrestigeManager(prestigeSettings);
            queenPanel.Init(prestige, economy, bloom, TryMoveQueen);

            LoadGarden();
            SpawnStartingBees();
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

        /// <summary>
        /// "Move the Queen" (design doc section 3b). Resets honey, upgrades, bees and the
        /// garden; keeps Royal Jelly, lifetime stats and timed boosts, and loads the next
        /// garden. Returns false when the bloom gate or the honey cost is not met.
        /// </summary>
        public bool TryMoveQueen()
        {
            float fraction = bloom.Fraction;
            if (!prestige.CanMove(fraction, economy.Honey))
                return false;

            // The honey cost is a gate only: the whole balance resets right after.
            BigNumber jelly = prestige.PreviewJelly(economy.RunHoneyEarned, fraction);
            beeManager.DespawnAll();
            upgrades.ResetLevels();
            economy.ResetRun(Time.timeAsDouble);
            prestige.CommitMove(jelly);

            LoadGarden();
            SpawnStartingBees();
            return true;
        }

        private void LoadGarden()
        {
            if (garden.Root != null)
                Destroy(garden.Root.gameObject);

            GardenConfig config = prestige.CurrentGarden;
            garden = GardenSpawner.Spawn(config, worldRoot);
            flowerManager.Init(config, prestige.GardenValueMultiplier, garden.Root);
            bloom.Load(config, garden);
            if (garden.Hive != null)
                beeManager.SetHive(garden.Hive.EntrancePoint);
            cameraFitter.Fit(garden.Bounds);
        }

        private void SpawnStartingBees()
        {
            for (int i = 0; i < beeSettings.StartingBees; i++)
                beeManager.Spawn(0);
        }

        private void HandleNectarDeposited(double nectar, double flowerValue)
        {
            economy.Deposit(nectar, flowerValue, Time.timeAsDouble);
        }

        private void HandleBloomChanged() => hud.ShowBloom(bloom.Percent, bloom.Fraction);

        private void HandleGardenCompleted() => gardenComplete.Play();
    }
}
