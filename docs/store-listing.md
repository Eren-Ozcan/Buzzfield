# Store listing and console setup

Working notes for the Play Console app (`com.yilkgames.buzzfield`), AdMob and Firebase.
Graphics (icon, feature graphic, screenshots) are never committed: they live in the gitignored
`docs/store-assets-originals/` and the private pictures repo (`pictures/buzzfield/`).
Studio-wide accounts and the new-game checklist: `C:\Projects\pictures\STUDIO.md`.
Ad placement rules: `C:\Projects\pictures\ADS_POLICY.md`.

## Status

| Step | State |
| --- | --- |
| Upload key (`android-keystore/`, backup in pictures) | done 2026-09-29, SHA-1 `D8:AA:D7:2C:56:5C:8C:56:7D:AE:45:92:99:D4:F0:BC:FA:19:74:07` |
| Release AAB (`Buzzfield > Android > Build Release AAB`) | todo |
| AdMob app, rewarded + interstitial units, 2/hour cap on the interstitial | done 2026-09-29 |
| AdMob GDPR message | todo |
| Firebase project `buzzfield-bloom-idle`, Android app, upload + debug SHA-1 | done 2026-09-29 |
| `Assets/google-services.json` (download from Firebase, backup in pictures) | todo |
| Google Analytics linked to the Firebase project (property 556653411, account 409352671) | done 2026-09-29 |
| AdMob app linked to Firebase (ad revenue in Analytics) | todo |
| Firebase Android API key restricted to package + SHA-1 (upload, debug); unused Browser key deleted | done 2026-09-29; add the Play app signing SHA-1 once it exists |
| Play Console app created | todo |
| Internal test release uploaded (registers the package for developer verification) | todo |
| In-app products created in Play Console | todo |
| Store listing text | ready (below) |
| Store graphics | waiting for art |
| Content rating, target audience, data safety, ads declaration | todo |
| Closed test with 12+ testers for 14+ days | todo |
| yilkgames.com game card | waiting for card art |

## Main store listing (en-US, default language)

The game is English only, so the listing is English only.

**App name** (30 max, 21 used)

    Buzzfield: Bloom Idle

**Short description** (80 max)

    Raise a buzzing swarm, make honey and bring every garden into full bloom.

**Full description** (4000 max)

    A quiet meadow, one little bee and a hive that is waiting to fill up with honey.

    Buzzfield is a relaxed idle game about bees and flowers. Your bees fly out to the
    flowers, bring nectar home and turn it into honey. Spend the honey on more bees and
    faster wings, and watch the garden come alive around your hive.

    GROW YOUR SWARM
    • Add bees one by one until the sky over the meadow is full of them
    • Merge three bees into a stronger one: Workers become Foragers, Foragers become Golden bees
    • Make them fly faster and make every drop of honey worth more

    BRING THE GARDEN INTO BLOOM
    • Every flower opens as your bees collect its nectar
    • Daisies, lavender and orchids, each worth more than the last
    • Fill the whole garden with flowers to complete it

    MOVE THE QUEEN
    • When a garden is in bloom, move your Queen to a new, richer one
    • Earn Royal Jelly for every move and spend it on lasting Queen abilities
    • Start every garden stronger than the one before

    PLAYS WHILE YOU ARE AWAY
    • Your bees keep working after you close the game
    • Come back to a pot of honey, or double it

    NO RUSH
    • Swipe across the flowers to shake out extra pollen when you feel like it
    • One-handed, portrait play that fits a few spare minutes

**Category**: Game › Simulation (idle). **Tags**: Idle, Casual, Simulation.

**Contact**: `yilkgamesstudio@gmail.com`, website `https://yilkgames.com/`.

**Privacy policy**: `https://yilkgames.com/privacy-policy/` (studio-wide page; add Buzzfield's
data to it before pointing Play at it, see STUDIO.md "Gizlilik ve veri silme").

## Graphics to make

Play requirements and a starting prompt for each. The prompts describe the game's look (low-poly
3D, soft daylight, warm honey tones); adjust freely. Screenshots are different: Play requires
them to show the real game, so they are captured in game (PlayMode tests with `BZ_SHOT_DIR`
save 1080x1920 frames with the HUD), never generated.

| Asset | Spec | Prompt |
| --- | --- | --- |
| App icon | 512x512 PNG, 32-bit, max 1 MB; Play rounds the corners, keep the subject in the middle 80% | "Game app icon, a cheerful chubby low-poly honey bee in flight in front of a glowing golden honeycomb, soft warm daylight, simple bold shapes, saturated yellow and warm orange on a soft green background, no text, centered, square" |
| Feature graphic | 1024x500 PNG or JPG, no transparency | "Wide banner for a relaxing idle game, low-poly 3D meadow at golden hour, a small wooden beehive on the left, a swarm of cute bees flying toward blooming daisies, lavender and orchids, soft depth of field, warm honey and green palette, empty space on the right third for a title, no text" |
| Phone screenshots | 2 to 8, 9:16, 1080x1920; 4+ for game recommendations | captured in game |
| Promo video (optional) | YouTube link | Flow: "Slow cinematic flight over a low-poly flower meadow, bees leave a small hive and fly to daisies that burst into bloom, warm afternoon light, 8 seconds" |
| yilkgames.com card | same style as the other cards in `yilkgames_web/assets/games/` | reuse the feature graphic art, cropped |

## In-app products (Play Console › Monetize › Products)

Ids must match `StoreCatalog.asset` and `Buzzfield.Core.ProductIds`.

| Id | Type | Price (USD base) | What it does |
| --- | --- | --- | --- |
| `remove_ads` | one-time, non-consumable | 2.99 | removes interstitials only; rewarded ads keep working (ad policy rule 7) |
| `permanent_2x_honey` | one-time, non-consumable | 4.99 | x2 honey forever, stacks with the rewarded boost |
| `starter_pack` | one-time, consumable, offered once | 0.99 | +5000 honey, +5 Royal Jelly |

## Ads (AdMob)

App and unit ids go to `AdSettings.asset` (live ids) once created. Development builds always
use Google's test units.

| Unit | Id | Frequency cap |
| --- | --- | --- |
| App (Android) "Buzzfield" | `ca-app-pub-9709993577664180~6836163163` (in `GoogleMobileAdsSettings.asset`) | |
| Rewarded - All Placements | `ca-app-pub-9709993577664180/2625455418` | none (ad policy rule 10: the game limits it) |
| Interstitial - Natural Break | `ca-app-pub-9709993577664180/8364867250` | 2 per 60 minutes, eCPM floor off |

The app shows "Review required / limited ad serving" until the Play listing is live and linked
in AdMob (App settings › Add store), like every other studio app before launch.

Game-side rules (`AdSettings.asset`): an interstitial only after a garden is completed or the
Queen moves, 1 s after the screen is calm, 4 min after any full-screen ad (rewarded included),
never before 20 min of play and the first Queen move, never after `remove_ads`.

## Firebase

Project `buzzfield-bloom-idle` (number 747796795174), Android app
`1:747796795174:android:f9388140724278de0f3be7`. Analytics and Crashlytics only.
`google-services.json` is gitignored (the repo is public); the backup lives in
`pictures/buzzfield/`. Without it a build still runs, only without Firebase.

## Data safety (draft)

| Data | Collected | Shared | Why |
| --- | --- | --- | --- |
| Device or other IDs (advertising ID) | yes | yes (AdMob) | advertising, analytics |
| App activity: app interactions | yes | no | analytics |
| Crash logs, diagnostics | yes | no | app functionality (Crashlytics) |
| Purchase history | yes | no | app functionality (Google Play billing) |

No account, no personal info, data encrypted in transit, no deletion request needed beyond the
studio page (`https://yilkgames.com/account-deletion/#data-only`). Ads: yes. Target audience:
13+ (not for children), so the Families policy does not apply.
