# Buzzfield: Bloom Idle

Portrait low-poly 3D idle game by Yilk Games. Unity 6000.3.25f1 (6.3 LTS), URP 3D.
Android first (`com.yilkgames.buzzfield`), iOS second. English only; every user-visible
string lives in `Buzzfield.Core.Strings`.

## Layout

- `Assets/_Project/Scripts/` — one assembly per folder:
  - `Core/` (`Buzzfield.Core`, `noEngineReferences: true`): `BigNumber`, `NumberFormat`,
    `RollingRate`, `HoneyFormula`, `BloomMath`, `ForagingMath`, `PrestigeMath`, `QueenMath`, `IncomeMath`, `OfflineEarnings`,
    `TapBoost`, `TimeFormat`, `TweenMath`, `AdPacing`, `TcfConsent`, `Entitlements`, `SaveData` + `SaveMigration` + `SaveEnvelope` +
    `SaveFileStore`, `Strings`.
    Pure logic only, EditMode-tested.
  - `Core/Runtime/` (`Buzzfield.Core.Runtime`): Unity helpers shared by systems
    (`PrefabPool`, `ParticlePool`, `Tweener`, `CameraFitter`). `Tweener` is our own small tween
    runner (scale pop, press hold, wiggle), ticked by `GameManager`; effects come from `ParticlePool`.
  - `Flowers/`, `Bees/`, `Economy/`, `Upgrades/`, `Save/`, `UI/`, `Ads/` — one system each,
    ScriptableObject definitions next to the code that reads them.
  - `Ads/` (`Buzzfield.Ads`): `AdManager` and `StoreManager` over `IAdService`/`IConsentService`/
    `IStoreService`, plus the editor mocks. `AdSettings.asset` holds the live AdMob unit ids and
    the interstitial pacing; development builds always use Google's test units.
  - `Platform/` (`Buzzfield.Platform`): the device SDKs behind those interfaces: `AdMobAdService`,
    `UmpConsentService` (consent before any ad request, Privacy button in the shop for EEA/UK),
    `UnityIapStoreService` (Unity IAP 5 `StoreController`), `FirebaseServices` (Analytics,
    Crashlytics, Consent Mode). `PlatformServices` picks them on Android/iOS players only; the
    editor, play mode tests and desktop players get the mocks.
  - `Save/` (`Buzzfield.Save`): `SaveManager` (JsonUtility, persistentDataPath, backup fallback).
    Bump `SaveMigration.CurrentVersion` and add a step whenever the `SaveData` layout changes.
  - `Game/` (`Buzzfield.Game`): `GameManager` (+ `GameManager.Save.cs`: autosave, offline; `GameManager.Ads.cs`: rewarded placements and interstitial breaks; `GameManager.Store.cs`: purchases and entitlements; `GameManager.Queen.cs`: Queen level and ability bonuses) and `GameClock`, the composition root. It owns init order,
    wires managers with plain C# events and drives all per-frame ticks.
  - `UI/`: every `Button` gets a `ButtonFeedback` (press scale, wiggle when refused), set up by
    the scene builder and initialised by `GameManager`; feel lives in `UiFeedbackSettings`.
    Per-frame number labels go through `NumberLabel` (no string allocations).
  - `Editor/`: menu *Buzzfield > Create Default Data* (materials, placeholder prefabs, all
    SO assets; never overwrites existing ones) and *Buzzfield > Build Greybox Scene*
    (`Assets/_Project/Scenes/Main.unity`). Batchmode:
    `Unity.exe -batchmode -quit -projectPath . -executeMethod Buzzfield.Editor.GreyboxSceneBuilder.Build`.
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
- Every visual is a prefab (primitives for now) so art can be swapped without code changes.
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
  excluded through `.git/info/exclude`; never stage it.

## Store / marketing assets

Store listing images, feature graphic, icon and screenshots are **never committed**
to this repo.

1. Local, gitignored copy: `docs/store-assets-originals/`.
2. Private backup: `C:\Projects\pictures\buzzfield\` (local clone of the private
   `Eren-Ozcan/pictures` repo). When adding or updating a store asset, put it in both
   places, then commit and push in the `pictures` repo.

Studio-wide accounts, domain and Play Console details: `C:\Projects\pictures\STUDIO.md`.
