using Buzzfield.Ads;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using Buzzfield.Save;
using Buzzfield.UI;
using Buzzfield.Upgrades;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Composition root: owns the init order, wires the managers together and drives
    /// the per-frame ticks in a fixed order (flowers regenerate before bees read them).
    /// Saving, loading and offline earnings live in GameManager.Save.cs, rewarded ads in
    /// GameManager.Ads.cs and the store in GameManager.Store.cs.
    /// </summary>
    public sealed partial class GameManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private EconomySettings economySettings;
        [SerializeField] private BeeSettings beeSettings;
        [SerializeField] private BloomSettings bloomSettings;
        [SerializeField] private PrestigeSettings prestigeSettings;
        [SerializeField] private BoostSettings boostSettings;
        [SerializeField] private OfflineSettings offlineSettings;
        [SerializeField] private AdSettings adSettings;
        [SerializeField] private StoreCatalog storeCatalog;
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
        [SerializeField] private WelcomeBackView welcomeBack;
        [SerializeField] private TapBoostView tapBoostView;
        [SerializeField] private RewardedBoostView rewardedBoostView;
        [SerializeField] private ShopPanelView shopPanel;
        [SerializeField] private TapCatcher tapCatcher;
        [SerializeField] private BackButtonHandler backButton;
        [SerializeField] private CameraFitter cameraFitter;
        [SerializeField] private Transform worldRoot;

        private EconomyManager economy;
        private UpgradeManager upgrades;
        private GardenBloomManager bloom;
        private PrestigeManager prestige;
        private BoostManager boosts;
        private AdManager ads;
        private StoreManager store;
        private GardenInstance garden;

        public EconomyManager Economy => economy;
        public UpgradeManager Upgrades => upgrades;
        public BeeManager Bees => beeManager;
        public FlowerManager Flowers => flowerManager;
        public GardenBloomManager Bloom => bloom;
        public PrestigeManager Prestige => prestige;
        public BoostManager Boosts => boosts;
        public AdManager Ads => ads;
        public StoreManager Store => store;
        public LifetimeStats Stats => stats;

        private void Awake()
        {
            Application.targetFrameRate = gameSettings.TargetFrameRate;

            economy = new EconomyManager(economySettings, Time.timeAsDouble);
            beeManager.Init(beeSettings, flowerManager);
            beeManager.OnNectarDeposited += HandleNectarDeposited;
            beeManager.OnBeeEvolved += HandleBeeEvolved;
            upgrades = new UpgradeManager(addBeeUpgrade, speedUpgrade, honeyValueUpgrade, beeSettings, economy, beeManager);
            hud.Init(economy, economySettings);
            bottomBar.Init(upgrades, economy, beeManager, beeSettings);
            bloom = new GardenBloomManager(bloomSettings, flowerManager);
            bloom.OnBloomChanged += HandleBloomChanged;
            bloom.OnFlowerBloomed += HandleFlowerBloomed;
            bloom.OnGardenCompleted += HandleGardenCompleted;
            prestige = new PrestigeManager(prestigeSettings);
            queenPanel.Init(prestige, economy, bloom, TryMoveQueen);
            boosts = new BoostManager(boostSettings, economy);
            tapBoostView.Init(boosts);
            tapCatcher.OnWorldTapped += HandleWorldTapped;
            InitAds();
            InitStore();

            InitSave();
            SaveData data = saveManager.Load();
            if (data != null)
            {
                RestoreGame(data);
            }
            else
            {
                LoadGarden(null);
                SpawnStartingBees();
            }
        }

        private void Start()
        {
            // Views hide their panels in Awake; the Welcome back panel may only open after that.
            StartSession(loadedClock);
            ads.Start();
            store.Start();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            double now = Time.timeAsDouble;
            boosts.Tick(GameClock.DeviceUtc);
            beeManager.BoostSpeedMultiplier = boosts.SpeedMultiplier(now);
            flowerManager.Tick(deltaTime);
            beeManager.Tick(deltaTime);
            bloom.Tick(deltaTime);
            ads.Tick(Time.unscaledDeltaTime);
            store.Tick(Time.unscaledDeltaTime);
            TickAutosave(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (beeManager != null)
            {
                beeManager.OnNectarDeposited -= HandleNectarDeposited;
                beeManager.OnBeeEvolved -= HandleBeeEvolved;
            }
            if (bloom != null)
            {
                bloom.OnBloomChanged -= HandleBloomChanged;
                bloom.OnFlowerBloomed -= HandleFlowerBloomed;
                bloom.OnGardenCompleted -= HandleGardenCompleted;
                bloom.Dispose();
            }
            if (tapCatcher != null)
                tapCatcher.OnWorldTapped -= HandleWorldTapped;
            ads?.Dispose();
            DisposeStore();
            DisposeSave();
        }

        /// <summary>Tap on the world (not UI): starts the tap boost if it is off cooldown.</summary>
        public bool TryTapBoost() => boosts.TryTapBoost(Time.timeAsDouble);

        /// <summary>
        /// "Move the Queen" (design doc section 3b). Resets honey, upgrades, bees, the tap
        /// boost and the garden; keeps Royal Jelly, lifetime stats and the rewarded boost,
        /// and loads the next garden. Store entitlements are not touched. Returns false when the bloom gate or the honey cost is not met.
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
            boosts.ResetTapBoost();
            prestige.CommitMove(jelly);

            LoadGarden(null);
            SpawnStartingBees();
            SaveNow();
            return true;
        }

        /// <summary>Spawns the current garden; <paramref name="data"/> restores its flower slots when given.</summary>
        private void LoadGarden(SaveData data)
        {
            if (garden.Root != null)
                Destroy(garden.Root.gameObject);

            GardenConfig config = prestige.CurrentGarden;
            garden = GardenSpawner.Spawn(config, worldRoot);
            flowerManager.Init(config, prestige.GardenValueMultiplier, garden.Root);
            if (data != null)
                RestoreSlots(data);
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

        private void HandleWorldTapped() => TryTapBoost();

        private void HandleBloomChanged() => hud.ShowBloom(bloom.Percent, bloom.Fraction);

        private void HandleFlowerBloomed(Flower _) => stats.flowersBloomed++;

        private void HandleBeeEvolved(Bee _) => stats.beesEvolved++;

        private void HandleGardenCompleted()
        {
            stats.gardensCompleted++;
            gardenComplete.Play();
        }
    }
}
