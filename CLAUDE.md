# Buzzfield: Bloom Idle

Portrait low-poly 3D idle game by Yilk Games. Unity 6000.3.25f1 (6.3 LTS), URP 3D.
Android first (`com.yilkgames.buzzfield`), iOS second. English only; every user-visible
string lives in `Buzzfield.Core.Strings`.

## Layout

- `Assets/_Project/Scripts/` — one assembly per folder:
  - `Core/` (`Buzzfield.Core`, `noEngineReferences: true`): `BigNumber`, `NumberFormat`,
    `RollingRate`, `HoneyFormula`, `BloomMath`, `ForagingMath`, `PrestigeMath`, `QueenMath`, `IncomeMath`, `OfflineEarnings`,
    `PollenMath`, `TimeFormat`, `TweenMath`, `AdPacing`, `TcfConsent`, `Entitlements`, `DewBoostQueue`, `SoundRules`, `Haptics`,
    `Goals`, `RewardMath`, `VisitorTimer`, `LoginStreak`, `DewPouch`, `SaveData` + `SaveMigration` + `SaveEnvelope` +
    `SaveFileStore`, `Strings`.
    Pure logic only, EditMode-tested.
  - `Core/Runtime/` (`Buzzfield.Core.Runtime`): Unity helpers shared by systems
    (`PrefabPool`, `ParticlePool`, `Tweener`, `CameraFitter`). `Tweener` is our own small tween
    runner (scale pop, press hold, wiggle), ticked by `GameManager`; effects come from `ParticlePool`.
  - `Flowers/`, `Bees/`, `Economy/`, `Upgrades/`, `Save/`, `UI/`, `Ads/`, `Audio/` — one system each,
    ScriptableObject definitions next to the code that reads them.
  - Pollen shake (the active-play mechanic): `SwipeCatcher` (UI) sends finger strokes to
    `GameManager.Pollen.cs`, `PollenShaker` (Flowers) shakes the flowers they cross, and each shake
    pays `PollenMath.ShakeNectar` through `EconomyManager.Harvest` (counts for the run, never for
    honey/sec, which sizes the shake). Tuning in `PollenSettings`; `bloomShare` keeps the bloom gate.
    A `SwipeTrail` (TrailRenderer prefab in `PollenSettings`) follows the finger on the swipe plane.
  - Feel (`GameManager.Feel.cs`): `SoundManager` (`Audio/`, plain class) plays the `SoundLibrary`:
    effects on a few pooled voices (rate-limited, extra requests dropped), stingers that duck the music,
    music, dry/bloom ambience crossfaded by bloom and three swarm hums by bee count. A sound without
    a clip is silent; `PlayedCount` lets tests check triggers. Clips are linked by file name
    (`docs/audio.md`) from `Assets/_Project/Audio/{Music,Ambience,SFX}/` by `SoundClipLinker`
    (on import, Create Default Data, *Buzzfield > Audio > Link Clips*), which also sets import
    settings. Vibration: `IHapticService` (`AndroidHaptics` in Platform, silent elsewhere), filtered
    by `VibrationMode`. `UiEffectsView` draws rising "+honey" numbers, honey drops flying to the
    counter and purchase sparkles from fixed pools. Buttons send tap/refused cues through `IUiCues`.
  - Goal chain (`GameManager.Goals.cs`): one goal at a time on the pill under the top bar
    (`GoalView`), from `GoalSettings` (kind, target, honey reward in seconds of the offline rate,
    Dewdrops); after the authored chain `endlessGoals` take turns, each counting from the moment it
    started (`goalBase`), so they only use lifetime counts. `GoalManager` (Economy) walks it; index
    and baseline are saved and survive the Queen move.
  - Dewdrops, the premium currency (`GameManager.Dew.cs`, `DewManager` in Economy): every Dewdrop
    goes through `EarnDew`/`TrySpendDew`, which log `dew_earn`/`dew_spend`; `TrySpendDew` does not
    save, so the caller hands out what was bought and then saves. Sources: goals, finished gardens
    (`DewSettings`), the daily streak, the butterfly's pouch and the Dewdrop packs (`StoreCatalog`: `dewGrant`; the starter pack also pays
    `honeySecondsGrant` seconds of the offline rate). The counter (crystal icon rendered from
    `Dewdrop.prefab`) sits right of the honey pill from the first Dewdrop and opens the shop.
    The shop has two tabs: Dewdrops (store products, restore) and Boosts (`DewBoostsTabView`, offers
    in `DewSettings.offers`, open after the first garden): time skips pay the offline rate in full
    outside the run total, honey boosts join the `DewBoostQueue` in `BoostManager` (strongest runs,
    weaker ones wait with their time, wall clock, also while closed; offline honey pays the
    multiplier for the boosted part). Spends above `confirmAbove` need a second tap. A running boost
    shows on `DewBoostChipView` under the gear. Packs are consumables bought again and again; an
    order the store delivers twice is granted once (`Entitlements.grantedOrders`). The shop badge
    waits `StoreCatalog.badgeAfterPlaySeconds` of play. Royal Jelly stays the prestige currency and
    is never sold. The economy plan is private: `docs/DEWDROPS.md` (excluded like
    `docs/BUILD_PROMPT.md`; never stage it).
  - Butterfly (`GameManager.Visitors.cs`): `VisitorManager` (Flowers) flies `Butterfly.prefab`
    across the garden every few minutes of calm play, never in the first `firstAfterPlaySeconds`;
    a tap or swipe through it pays a gift and `GiftOfferView` offers a rewarded "double it" for a
    few seconds. Tuning in `VisitorSettings`. Once the daily streak is open, every `pouchEvery`th
    butterfly that sets off while a rewarded ad is ready carries `DewPouch.prefab` instead of honey
    (at most `pouchesPerDay` a day; the `DewPouch` tally is saved): caught, it pays nothing until the
    same offer, in pouch mode with an X, plays the rewarded ad that opens it (`pouchMinDew` to
    `pouchMaxDew`). The first Welcome back ever adds a one-time gift (`GameSettings.welcomeGiftSeconds`).
  - Daily streak (`GameManager.Daily.cs`): days come from trusted time only (`GameClock`), in the
    local time zone, turning at `DewSettings.dayStartHour`; `LoginStreak` notes the first known day
    and opens the streak `dailyUnlockDay` days later. `DailyPanelView` (seven tiles from
    `DewSettings.dailyRewards`, a missed day starts over) opens by itself after a launch or a real
    return once the date is known and nothing else is on screen, at most once a day; while today's
    Dewdrops wait, a calendar badge under the goal pill opens it. A claim closes the panel, flies the
    crystals and saves. Without the date the claim waits ("Waiting for the time server").
  - Garden look: each flower slot is a patch prefab (`FlowerPatch_*`), and `GardenDecor` (grass,
    bushes, stones, fence) is laid out from the garden's seed and merged per material by
    `GardenDecorView` when the garden spawns. Placeholder meshes are flat-shaded low-poly, built by
    the editor `LowPolyBuilder` into `Assets/_Project/Meshes/`.
  - Bees and hive follow the Flow concept art (`pictures/buzzfield/masters/flow/`): `CharacterModels`
    builds them as one mesh each, coloured through the `Palette` texture, so every bee is a single
    renderer on the shared `Palette` material. *Buzzfield > Render Model Sheet* (`ModelSheet`,
    batchmode `-executeMethod Buzzfield.Editor.ModelSheet.RenderToFiles -quit`, output in
    `BZ_SHOT_DIR` or `Temp/ModelSheet`) renders the bees, hive, flower patches and the butterfly with
    its pouch up close.
  - Around every garden: `Scenery.prefab` (`SceneryModels`, one palette mesh: meadow flat under and
    in front of the garden, hills behind, trees, rocks, wildflowers, stepping stones), tinted from
    faded to full by `SceneryView` as the garden blooms; the camera still frames the garden only.
    The scene has soft main-light shadows (modelled art casts them), linear fog for the far hills
    and a global post volume (`GardenPost.asset`: colour adjustments, white balance, vignette).
  - `Ads/` (`Buzzfield.Ads`): `AdManager` and `StoreManager` over `IAdService`/`IConsentService`/
    `IStoreService`, plus the editor mocks. `AdSettings.asset` holds the live AdMob unit ids and
    the interstitial pacing; development builds always use Google's test units.
  - `Platform/` (`Buzzfield.Platform`): the device SDKs behind those interfaces: `AdMobAdService`,
    `UmpConsentService` (consent before any ad request; "Ad privacy choices" in Settings and a
    Privacy button in the shop for EEA/UK), `AndroidHaptics`,
    `UnityIapStoreService` (Unity IAP 5 `StoreController`), `FirebaseServices` (Analytics,
    Crashlytics, Consent Mode). `PlatformServices` picks them on Android/iOS players only; the
    editor, play mode tests and desktop players get the mocks.
  - `Save/` (`Buzzfield.Save`): `SaveManager` (JsonUtility, persistentDataPath, backup fallback).
    Bump `SaveMigration.CurrentVersion` and add a step whenever the `SaveData` layout changes.
  - `Game/` (`Buzzfield.Game`): `GameManager` (+ `GameManager.Save.cs`: autosave, offline; `GameManager.Ads.cs`: rewarded placements and interstitial breaks; `GameManager.Store.cs`: purchases and entitlements; `GameManager.Queen.cs`: Queen level and ability bonuses; `.Feel`, `.Goals`, `.Visitors`, `.Settings`: see above and below) and `GameClock`, the composition root. It owns init order,
    wires managers with plain C# events and drives all per-frame ticks.
  - `UI/`: every `Button` gets a `ButtonFeedback` (press scale, wiggle when refused), set up by
    the scene builder and initialised by `GameManager`; feel lives in `UiFeedbackSettings`.
    Per-frame number labels go through `NumberLabel` (no string allocations).
    `SettingsPanelView` (gear under the Queen button): music, sound, vibration, ad privacy choices,
    restore purchases, privacy policy (`GameSettings.privacyPolicyUrl`) and "Delete my data" (second
    tap confirms; deletes the save and the device's analytics data, then reloads as a new game).
    Settings live in `SaveData.settings`.
    The look is built by the scene builder from `UiTheme` (honey, cream and wood palette, button
    colours by role, icon glyphs) and `UiAssets` (Fredoka SDF and Icons SDF font assets, generated
    9-slice sprites in `Assets/_Project/UI/Sprites/`, bee icons rendered from the models; delete a
    file to rebuild it). Fonts and their licences are in `Assets/_Project/UI/Fonts/`:
    `Fredoka-SemiBold.ttf` is a static instance of the Google Fonts variable font, and
    `BuzzfieldIcons.ttf` is a filled weight-600 subset of Material Symbols Rounded holding only the
    `UiTheme` glyphs. A new icon means a new subset (fontTools `varLib.instancer` with
    FILL=1 wght=600 GRAD=0 opsz=48, and `subset` with the codepoints) plus a rebuilt `Icons SDF`.
  - `Editor/`: menu *Buzzfield > Create Default Data* (materials, placeholder prefabs, all
    SO assets; never overwrites existing ones) and *Buzzfield > Build Greybox Scene*
    (`Assets/_Project/Scenes/Main.unity`). Batchmode:
    `Unity.exe -batchmode -quit -projectPath . -executeMethod Buzzfield.Editor.GreyboxSceneBuilder.Build`.
    *Buzzfield > Play Test Tools* (`PlayTestWindow`) is for trying the game by hand in the editor:
    opens Main, deletes the editor save, and in play mode adds honey, blooms or completes the
    garden, refills pollen, sends the butterfly (with or without a pouch), sets game speed and shows the
    swipe hint again. For the daily streak it takes the device clock as trusted time and moves the
    date a day ahead (in memory only; days saved meanwhile stay ahead of the real date). Its shortcuts
    live in `GameManager.Debug.cs`, compiled only in the editor and development builds.
    *Buzzfield > Android > Build Dev APK* writes `Builds/Android/Buzzfield-dev.apk` (gitignored):
    `Unity.exe -batchmode -quit -buildTarget Android -projectPath . -executeMethod Buzzfield.Editor.AndroidBuild.BuildDevApk`.
    *Build Release AAB* (`BuildReleaseAab`) signs with the upload key in the gitignored
    `android-keystore/` (password from `BZ_KEYSTORE_PASS` or `android-keystore/buzzfield-upload.pass`;
    backup and SHA-1 in `C:\Projects\pictures\buzzfield\android-keystore\`) and refuses to
    overwrite an AAB of the same version code: run *Bump Version Code* (`BumpVersionCode`) first.
    Both builds force an External Dependency Manager resolve into `Assets/Plugins/Android/*Template*`.
    Player builds rewrite some settings files (URP asset prefiltering, UnityConnect, batching);
    check `git diff` afterwards and revert what was not meant.
- `Assets/_Project/Tests/EditMode/` — NUnit tests for `Core`
  (Test Runner window, or `-runTests -testPlatform EditMode`).
- `Assets/_Project/Tests/PlayMode/` — smoke and upgrade-flow tests against the real
  `Main.unity`. Batchmode needs the editor closed:
  `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <file>`.
  Tests call `TestSave.Clear()` before loading the scene so they never touch the real save.
  Set `BZ_SHOT_DIR` (folder must exist) to also save 1080x1920 and 1440x1920 screenshots (HUD included).
  `PerformanceTests` fills the bee cap: the report test logs tick CPU, GC and draw calls (set
  `BZ_PERF_OUT` to a file to save it); the zero-allocation test runs only in a player
  (TMP allocates in the editor): `-runTests -testPlatform StandaloneWindows64 -testFilter PerformanceTests`.
  `BalanceReportTests` is explicit: a bot plays three gardens and two loops at fixed 0.1 s steps
  and logs milestone times (`-testFilter BalanceReportTests`, `BZ_BALANCE_OUT` for a file).
  Re-run it after any balance change. Tests bloom flowers through `TestBloom` (bloom follows
  collected nectar, `FlowerType.nectarToBloom`).
- Queen abilities are data: add a `QueenAbility` asset to `QueenSettings.abilities` (reusing a
  `QueenEffect`) and rebuild the scene so the panel gets a row. Saves key ability levels by id.
- Batchmode TMP import: `-executeMethod Buzzfield.Editor.TmpResources.ImportAndExit`
  without `-quit` (the package import finishes after the method returns).
- `Assets/_Project/ScriptableObjects/` — all balance data. No tuning number in code.

## Rules

- All currency (honey, costs, income, Royal Jelly math) is `BigNumber`; `double`/`float`
  only for per-frame movement and bloom math. Display only through `NumberFormat`.
- Bees and flowers have no `Update()`; `BeeManager`/`FlowerManager` tick them from
  `GameManager`. No allocations, LINQ or `GetComponent` in per-frame code. Pool spawned objects.
- No `FindObjectOfType`/`FindObjectsByType` and no singletons; pass references through
  `[SerializeField]` or `Init(...)`. Unsubscribe events in `OnDisable`/`OnDestroy`.
- Every visual is a prefab (primitives and generated low-poly placeholders for now) so art can
  be swapped without code changes. To regenerate a placeholder, delete its prefab (for a decor
  piece also `GardenDecor.asset`) and run Create Default Data.
- Packages: URP, Input System, uGUI/TextMeshPro, Test Framework, Google Mobile Ads (OpenUPM),
  Unity IAP, Firebase App/Analytics/Crashlytics and the External Dependency Manager only. Ask
  before adding one. No DOTween. The Firebase tarballs are not in git: run
  `scripts/fetch-firebase.sh` after cloning. `Assets/google-services.json` is gitignored (backup in
  the pictures repo). Locally `Packages/manifest.json` and `packages-lock.json` are marked
  skip-worktree so the Coplay editor plugin stays out of git; commit package changes without it.
- Ads follow the studio policy (`C:\Projects\pictures\ADS_POLICY.md`): rewarded ads only on the
  player's tap; interstitials only after a natural break the game marks with
  `AdManager.MarkNaturalBreak` (garden completed, Queen move), once no panel, celebration, ad or
  purchase is on screen, never for new players or after `remove_ads`, and never inside the shared
  cooldown. No banners, no app-open ads. A new trigger needs a check against that policy first.
- Never name competitor games or companies in committed files (code, comments, commit
  messages, README). Refer to "the design doc". `docs/BUILD_PROMPT.md` is private and
  excluded through `.git/info/exclude`; never stage it. The same goes for `docs/DEWDROPS.md`
  (premium currency economy and prices).

## Store / marketing assets

Store listing images, feature graphic, icon and screenshots are **never committed**
to this repo.

1. Local, gitignored copy: `docs/store-assets-originals/`.
2. Private backup: `C:\Projects\pictures\buzzfield\` (local clone of the private
   `Eren-Ozcan/pictures` repo). When adding or updating a store asset, put it in both
   places, then commit and push in the `pictures` repo.

Studio-wide accounts, domain and Play Console details: `C:\Projects\pictures\STUDIO.md`.
