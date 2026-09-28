# Buzzfield: Bloom Idle

Portrait low-poly 3D idle game by Yilk Games. Unity 6000.3.25f1 (6.3 LTS), URP 3D.
Android first (`com.yilkgames.buzzfield`), iOS second. English only; every user-visible
string lives in `Buzzfield.Core.Strings`.

## Layout

- `Assets/_Project/Scripts/` — one assembly per folder:
  - `Core/` (`Buzzfield.Core`, `noEngineReferences: true`): `BigNumber`, `NumberFormat`,
    `RollingRate`, `HoneyFormula`, `BloomMath`, `PrestigeMath`, `QueenMath`, `IncomeMath`, `OfflineEarnings`,
    `TapBoost`, `TimeFormat`, `TweenMath`, `AdPacing`, `Entitlements`, `SaveData` + `SaveMigration` + `SaveEnvelope` +
    `SaveFileStore`, `Strings`.
    Pure logic only, EditMode-tested.
  - `Core/Runtime/` (`Buzzfield.Core.Runtime`): Unity helpers shared by systems
    (`PrefabPool`, `ParticlePool`, `Tweener`, `CameraFitter`). `Tweener` is our own small tween
    runner (scale pop, press hold, wiggle), ticked by `GameManager`; effects come from `ParticlePool`.
  - `Flowers/`, `Bees/`, `Economy/`, `Upgrades/`, `Save/`, `UI/`, `Ads/` — one system each,
    ScriptableObject definitions next to the code that reads them.
  - `Ads/` (`Buzzfield.Ads`): `AdManager` and `StoreManager` over `IAdService`/`IStoreService`;
    mock services stand in until the AdMob and Unity IAP SDKs are added.
  - `Save/` (`Buzzfield.Save`): `SaveManager` (JsonUtility, persistentDataPath, backup fallback).
    Bump `SaveMigration.CurrentVersion` and add a step whenever the `SaveData` layout changes.
  - `Game/` (`Buzzfield.Game`): `GameManager` (+ `GameManager.Save.cs`: autosave, offline; `GameManager.Ads.cs`: rewarded placements; `GameManager.Store.cs`: purchases and entitlements; `GameManager.Queen.cs`: Queen level and ability bonuses) and `GameClock`, the composition root. It owns init order,
    wires managers with plain C# events and drives all per-frame ticks.
  - `UI/`: every `Button` gets a `ButtonFeedback` (press scale, wiggle when refused), set up by
    the scene builder and initialised by `GameManager`; feel lives in `UiFeedbackSettings`.
    Per-frame number labels go through `NumberLabel` (no string allocations).
  - `Editor/`: menu *Buzzfield > Create Default Data* (materials, placeholder prefabs, all
    SO assets; never overwrites existing ones) and *Buzzfield > Build Greybox Scene*
    (`Assets/_Project/Scenes/Main.unity`). Batchmode:
    `Unity.exe -batchmode -quit -projectPath . -executeMethod Buzzfield.Editor.GreyboxSceneBuilder.Build`.
    *Buzzfield > Build Android Dev APK* writes `Builds/Android/Buzzfield-dev.apk` (gitignored):
    `Unity.exe -batchmode -quit -buildTarget Android -projectPath . -executeMethod Buzzfield.Editor.AndroidBuild.BuildDevApk`.
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
- Packages: URP, Input System, uGUI/TextMeshPro, Test Framework only. Ask before adding one.
  No DOTween.
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
