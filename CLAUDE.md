# Buzzfield: Bloom Idle

Portrait low-poly 3D idle game by Yilk Games. Unity 6000.3.25f1 (6.3 LTS), URP 3D.
Android first (`com.yilkgames.buzzfield`), iOS second. English only; every user-visible
string lives in `Buzzfield.Core.Strings`.

## Layout

- `Assets/_Project/Scripts/` — one assembly per folder:
  - `Core/` (`Buzzfield.Core`, `noEngineReferences: true`): `BigNumber`, `NumberFormat`,
    `RollingRate`, `HoneyFormula`, `BloomMath`, `PrestigeMath`, `Strings`. Pure logic only, EditMode-tested.
  - `Core/Runtime/` (`Buzzfield.Core.Runtime`): Unity helpers shared by systems
    (`PrefabPool`, `CameraFitter`).
  - `Flowers/`, `Bees/`, `Economy/`, `Upgrades/`, `Save/`, `UI/`, `Ads/` — one system each,
    ScriptableObject definitions next to the code that reads them.
  - `Game/` (`Buzzfield.Game`): `GameManager`, the composition root. It owns init order,
    wires managers with plain C# events and drives all per-frame ticks.
  - `Editor/`: menu *Buzzfield > Create Default Data* (materials, placeholder prefabs, all
    SO assets; never overwrites existing ones) and *Buzzfield > Build Greybox Scene*
    (`Assets/_Project/Scenes/Main.unity`). Batchmode:
    `Unity.exe -batchmode -quit -projectPath . -executeMethod Buzzfield.Editor.GreyboxSceneBuilder.Build`.
- `Assets/_Project/Tests/EditMode/` — NUnit tests for `Core`
  (Test Runner window, or `-runTests -testPlatform EditMode`).
- `Assets/_Project/Tests/PlayMode/` — smoke and upgrade-flow tests against the real
  `Main.unity`. Batchmode needs the editor closed:
  `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <file>`.
  Set `BZ_SHOT_DIR` to also save 1080x1920 and 1440x1920 screenshots (HUD included).
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
