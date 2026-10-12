# Testing

Four layers, from fastest to slowest. Run the first two before every commit and the device
pass before every Play upload.

| Layer | Where | Time | Run |
|---|---|---:|---|
| EditMode (409) | `Assets/_Project/Tests/EditMode` | ~40 s | `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults <file>` |
| PlayMode (162) | `Assets/_Project/Tests/PlayMode` | ~7 min | `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <file>` |
| Balance bot | `BalanceReportTests` (explicit) | ~2 min | add `-testFilter BalanceReportTests` to the PlayMode line |
| Device pass | `scripts/android-device-tests.sh` | ~4 min | build the dev APK, connect a phone, run the script |

Batchmode needs the editor closed. Any error or exception logged during a PlayMode test fails it.

## EditMode

Every rule in `Buzzfield.Core` without a scene: the economy formulas, offline earnings and the
trusted clock, saves, migrations and the save envelope, goals, the daily streak, Dewdrops and
their budget, rare bees, ads pacing, store entitlements.

- `StringsTests`: every text in `Strings` is used by some screen, none is empty, placeholders
  run from `{0}`, and every character is in the text font's atlas (`Fredoka SDF` is a static
  ASCII atlas, so anything else would draw as a box). Store prices from the big markets go
  through `PriceLabel` and must come out drawable too.
- `PriceLabelTests`: a store price keeps its own form when the font can draw it; a currency
  sign the font lacks (lira, rupee, ruble, zloty) gives way to the ISO code ("29,99 TRY"),
  no-break spaces turn plain and direction marks go.

## PlayMode

Feature tests press the real buttons of the scene the builder makes: one class per system
(bloom, pollen, goals, Dewdrops and the shop, daily streak, butterfly, rare bees, Queen moves,
settings, ads, saves, feel). `PerformanceTests` measures a full swarm (its zero-allocation test
runs only in a player).

`RobustnessTests` look for what a feature test does not:

- **Save round trip.** Every field of `SaveData` is set off a new game's value, loaded by the game
  and saved again at once. A field the game forgets to restore or capture comes back changed, and
  a new field fails the coverage check until the test gives it a value.
- **Out-of-range save.** Negative balances, levels past their maximum, unknown and missing ids, a
  garden far past the last one, a boost line from the future: the game loads, plays 400 random
  steps and saves again.
- **Far loops.** Garden 601 (600 Queen moves): flower values, honey and the offline rate stay finite.
- **Leaks.** Once the last garden repeats, 8 more Queen moves and 30 rounds of every panel leave
  as many GameObjects, meshes, materials and textures as before, and managed memory grows < 2 MB.
- **Random taps.** A seeded monkey taps reachable buttons and random points, swipes across the
  garden, presses back and taps the butterfly: 3000 steps at 20x speed from a new game, and 2500
  steps late in the game (two Queen moves behind, honey, Dewdrops and the streak open). Every 50
  steps: honey, Royal Jelly, multipliers and honey per second finite and not negative, Dewdrops
  not negative, bees within the cap, levels within their maximum, at most one panel open. Quit,
  the privacy policy link and Delete my data are skipped (each has its own test).
- A character a font cannot draw fails any of these tests.

`LayoutTests` render the camera and the canvas into a texture at 1080x1920, 1080x2340, 1080x2400,
1080x2520, 720x1600, 1440x3200 and two tablets (1200x1920, 1536x2048):

- **Every panel** (HUD new and late, Queen panel and its confirmation, both shop tabs, settings
  and its delete confirmation, Hive, daily, Welcome back, quit dialog, butterfly offers): no
  control off screen, every control of the front layer reachable by a finger (nothing on top), no
  text spilling out of its box, and on the 1080 px phones no tap target under 48 dp (Android's
  minimum, a 1080 px screen taken as 411 dp; a graphic's raycast padding counts).
- **The garden**: in every authored garden and on every screen the flower heads and the hive are
  on screen and not under a HUD control, so a swipe or a tap reaches them. The flowers' place on
  screen is logged for comparing framings.

Set `BZ_SHOT_DIR` to an existing folder to save the screens (`layout_*`, `garden_*`).

## Device pass

`scripts/android-device-tests.sh [apk] [monkey-events]` backs up the phone's save (the app's
external files folder), installs the APK and runs: cold start with the time to the first frame,
the save written when the app goes home, warm start, a critical memory trim, the process killed
in the background, no network (Wi-Fi and data off), system font scale 1.3, a forced rotation,
Android's monkey (taps, swipes and pinches only; it may not leave the app, so store sheets and ad
click-throughs are refused) and memory after the cold start and after the monkey (reported, and
failed over `BZ_PSS_GAME_MB` / `BZ_PSS_ADS_MB` when set). Each step scans logcat for fatal
exceptions, ANRs and logged Unity exceptions and leaves a log and a screenshot in
`Builds/Android/device-tests/`. The save, font scale, rotation and network are put back at the end
even when a step fails.

## Results, 2026-10-03

- Editor: EditMode 409/409, PlayMode 160 passed and 2 skipped (the balance bot is explicit; the
  zero-allocation test runs only in a player). The monkey reached garden 3 from a new game in 3000
  steps and pressed 22 different buttons; late in the game it caught 8 butterflies.
- Leaks: 8 Queen moves and 30 rounds of panels left 729 GameObjects, 181 meshes, 130 materials and
  199 textures, as before.
- Framing: unchanged on every phone shape; on a 4:3 tablet the flowers now span x 0.25-0.75 and
  y 0.35-0.73 of the screen (from 0.20-0.81 and 0.37-0.82, under the goal row).
- Device (POT-LX1, Android 10, development APK): every step passed, no crash, ANR or Unity
  exception. First frame 2.3 s after a cold start, 121 ms back from home. PSS 346-371 MB while
  playing, 382 MB after 5000 monkey events with test ads.

## Found and fixed by these tests

- **Store prices drew boxes.** The text font is a static ASCII atlas, and the store's localized
  price carries signs it lacks (lira, euro, pound, rupee) and no-break spaces, so in most
  countries the buy button showed boxes. `PriceLabel` now keeps the price when the font can draw
  it and otherwise writes the number with the ISO code ("29,99 TRY"); the store hands the code and
  amount through `StoreProductInfo`.
- **Far loops broke the economy.** The garden value factor went through a `float`, so after about
  390 Queen moves the loop bonus overflowed to infinity and every honey deposit threw. It is a
  `double` now (good for about 3000 loops past the last garden).
- **Flowers under the goal row on tablets.** `CameraFitter` kept fixed insets that predate the row
  under the top bar; on a 4:3 tablet the top flowers sat under the goal pill. It now measures the
  top bar, the row under it and the bottom bar on the canvas and frames the garden between them,
  keeping its old place on phones (the logged framing is unchanged there).
- **The daily panel covered the butterfly's offer.** The panel (and an interstitial) waited only
  for panels and celebrations, so it could open over the offer while its time ran out. Both wait
  for the offer now (`IsScreenFree`).
- **Tap targets under 48 dp**: the gear, calendar and hive buttons (42-44 dp), the Dewdrops
  counter, the boost chip (35), the butterfly offer's close button (29), Close buttons (40-47),
  shop tabs, and the buy buttons in the Hive, Queen and Boosts lists. `ButtonFeedback` now grows a
  small button's touch area to `UiFeedbackSettings.minTapSize` (120 canvas units, 48 dp on the
  common phones) through its graphic's raycast padding; nothing moves on screen.
- Nine texts in `Strings` no screen used (old tier and flower names, a honey label).

## Known gaps

- **Firebase does not start in local builds**: `Assets/google-services.json` has not been
  downloaded yet (see `C:\Projects\pictures\STUDIO.md`), so Analytics and Crashlytics stay off
  and the device pass notes it.
- **No memory budget** is set for this game yet; the device pass reports PSS without judging it.
- **Real purchases and real ads** run only on a device: the editor uses the mock store and mock
  ads. The device pass shows test ads in a development build.
- **Clock tampering on a device** needs a rooted phone; the trusted clock is covered by EditMode.
- **The store's real price strings** are covered by sample strings, not by every currency Play has.
