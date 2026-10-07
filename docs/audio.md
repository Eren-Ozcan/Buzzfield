# Audio

Every sound the game needs: when it plays, how it should feel, and a prompt for generating it.
The game already plays every one of them (`SoundManager`, `SoundLibrary`); until a file exists the
sound is silent. Drop a file named as below into its folder and it is linked on import.

Sound has to tell the same story as the art: a sleepy grey garden that wakes up. It starts quiet
and sparse (a breeze, one bee) and grows warmer and busier with every bloom. Cozy, soft and
cute; never shrill, never busy enough to tire a player who leaves the game open.

## Making the files

- **Tools.** Any generator works: ElevenLabs Sound Effects or Stable Audio for effects, Google's
  Lyria (MusicFX), Suno or Udio for music; the Coplay plugin in the editor has SFX and music
  generators too. Use a plan whose terms allow commercial use: several free tiers do not.
- **Generate several takes**, keep the best, and name them by id (`sfx_ui_tap_1.wav`, `_2`...).
- **Trim** silence before the first sound (effects must hit on the frame they are triggered),
  add a 10-30 ms fade-out, and check that loops loop: generated loops rarely do. Fix the seam with
  a short crossfade in Audacity.
- **Key.** The music is in F major. Tonal effects (chimes, jingles) use the same key, and the
  bloom chime is played on the F major pentatonic (F G A C D) by pitching one clip, so random
  pitches always sound right.

### Format and loudness

| Kind | Format | Loudness |
| --- | --- | --- |
| Effects | WAV, 48 kHz, 16-bit, mono | peak -3 dBFS; UI clicks peak -12 dBFS |
| Loops (ambience, swarm) | WAV, 48 kHz, 16-bit, stereo, seamless | about -24 LUFS |
| Music | WAV, 48 kHz, 16-bit, stereo, seamless or with loop points | about -16 LUFS |

Files go to `Assets/_Project/Audio/{Music,Ambience,SFX}/` (the swarm loops go in Ambience).
`SoundClipLinker` links them to the `SoundLibrary` by name (variants `_1`, `_2`... all play at
random) and sets the import settings: effects Decompress On Load (ADPCM, mono), loops and music
Streaming (Vorbis, quality 65). *Buzzfield > Audio > Link Clips* runs the linking by hand.

### Shared prompt spine for effects

Start every effect prompt with this, then the description:

> Sound effect for a cozy, cute casual mobile game about bees and a flower garden. Soft, warm,
> rounded and clean, gentle attack, no harsh high frequencies, dry with at most a very short
> natural tail, no music, no voices, no background noise.

---

## Music

### mus_garden_loop

- **When:** the whole game, under everything; ducks under the stingers and full-screen ads.
- **Length:** 2-3 min seamless loop.

> Cozy, relaxing instrumental loop for a casual idle garden game. Warm fingerpicked acoustic
> guitar, soft wooden marimba melody, light pizzicato strings, gentle glockenspiel sparkles, soft
> shaker and brushed percussion, no drum kit. 92 BPM, F major, sunny, carefree and unhurried,
> like a slow afternoon in a meadow. Simple memorable melody that does not tire on repeat, no
> vocals, no big build-ups, seamless loop.

### mus_garden_loop_b (later)

- **When:** alternates with the first loop from garden 3 on, so long sessions do not repeat.
- **Length:** 2-3 min seamless loop.

> Same cozy garden game style as a warm acoustic loop: ukulele and soft felt piano carry the
> melody, marimba answers, pizzicato strings and light glockenspiel, soft brushed percussion.
> 92 BPM, F major, a touch dreamier and more open, no vocals, seamless loop.

### stinger_garden_complete

- **When:** Garden Complete banner and confetti. The music ducks for it.
- **Length:** 3-4 s.

> Short joyful celebration jingle for a cozy garden game: a rising marimba and glockenspiel
> arpeggio over pizzicato strings, ending on a bright F major chord with a soft sparkling chime
> and a light shaker roll. Gentle triumph, not a fanfare, no brass, no vocals.

### stinger_queen_move

- **When:** the Queen moves to the next garden (prestige), as the new garden appears.
- **Length:** 3-5 s.

> Short magical transition sting for a cozy garden game: a soft airy whoosh rising into a warm
> harp glissando and celesta chimes, settling on a calm F major chord with a gentle shimmer.
> Feels like a royal fresh start, regal but soft, no brass, no vocals.

---

## Ambience

Two loops crossfaded by the garden's bloom: the dry loop at 0%, the bloom loop at 100%.

### amb_dry_loop

- **Length:** 40-60 s seamless, stereo, very quiet under the music.

> Quiet ambience of a sleepy, dry meadow on a calm morning: a soft breeze through dry grass,
> faint rustling, one very distant bird now and then. Sparse and calm, no insects buzzing, no
> water, no wind gusts, seamless loop.

### amb_bloom_loop

- **Length:** 40-60 s seamless, stereo, very quiet under the music.

> Gentle ambience of a lush flower garden on a sunny afternoon: soft cheerful songbirds at a
> distance, leaves rustling in a light breeze, a calm warm atmosphere. No insects buzzing, no
> water, no people, seamless loop.

---

## Bees

### bee_swarm_loop_small / _medium / _large

- **When:** always; crossfaded by bee count (about 1-10, 10-50, 50-150). The swarm is a bed, not
  a sound per bee.
- **Length:** 8-15 s seamless loops, stereo.

> Small: a soft, warm, low humming of two or three cute bees flying around nearby, rounded and
> gentle like a purr, no sharp buzz, seamless loop.

> Medium: a soft busy hum of a small swarm of cute bees in a garden, warm low tone with gentle
> movement, rounded, never harsh, seamless loop.

> Large: a full but soft hum of a big friendly bee swarm at a distance, warm and smooth like a
> gentle drone with slow movement, no sharp buzzing, seamless loop.

### sfx_bee_spawn

- **When:** Add Bee; a bee appears at the hive.
- **Length:** 0.3-0.5 s, 3 variants.

> A tiny cheerful bee takes off: a quick soft "bzzt" rising in pitch with a little bubbly pop,
> cute and short.

### sfx_honey_deposit

- **When:** a bee unloads at the hive. Very frequent: at most 4 per second, quiet, pitch
  randomised ±8%.
- **Length:** 0.15-0.3 s, 4 variants.

> A tiny soft drop of honey falling into a jar: a gentle round "plip" with a faint sweet sparkle,
> very short and quiet.

### sfx_bee_evolve

- **When:** three bees merge into one of the next tier. Played a fifth higher for a Golden bee.
- **Length:** 1-1.5 s.

> Three little bees swirl together and merge: a short soft swirling whoosh that gathers into a
> bright magical "pop" with rising sparkling chimes, rewarding and cute.

---

## Pollen and flowers

### sfx_pollen_shake

- **When:** a swipe shakes a flower. A swipe can shake many at once: at most 8 per second, pitch
  randomised ±6%, and slightly higher for each flower in the same stroke.
- **Length:** 0.25-0.4 s, 4 variants.

> A small flower is brushed by a finger: a soft leafy rustle with a light airy "poof" of pollen
> dust and a tiny sparkle, short, playful and satisfying.

### sfx_flower_bloom

- **When:** a flower reaches full bloom and bursts into colour. One clip, pitched up the F major
  pentatonic as blooms follow each other.
- **Length:** 0.6-1 s.

> A flower bursts into bloom: a soft round "pop" followed by a single clear glockenspiel note
> on F with a gentle shimmer, joyful and light.

### sfx_sprout_pop

- **When:** a new sprout grows in from the ground.
- **Length:** 0.3-0.5 s, 2 variants.

> A tiny green sprout pops out of soft soil: a gentle muffled earthy pop with a small bouncy
> pluck, cute and short.

---

## Queen

### sfx_jelly_gain

- **When:** Royal Jelly is added after a Queen move and the Queen level rises.
- **Length:** 0.8-1.2 s.

> Royal reward: a warm, soft shimmering sparkle with a short ascending harp phrase ending on a
> bright note, precious and gentle.

### sfx_ability_buy

- **When:** a Queen ability is bought.
- **Length:** 0.6-1 s.

> A royal upgrade unlocks: a soft wooden click followed by a short ascending celesta and harp
> flourish with a light sparkle, elegant and satisfying.

---

## Interface

### sfx_ui_tap

- **When:** every button press (with the press scale).
- **Length:** 0.05-0.1 s, 2 variants.

> A soft wooden button tap: a short muted "tok", warm and tactile, very quiet and dry.

### sfx_ui_refused

- **When:** a press that cannot happen (too expensive, locked); goes with the wiggle.
- **Length:** 0.15-0.25 s.

> A gentle "not yet" for a cute game: two quick soft low wooden knocks, falling slightly in
> pitch, friendly and not harsh, very short.

### sfx_ui_open / sfx_ui_close

- **When:** Queen panel, shop, Welcome back and quit dialog open and close.
- **Length:** 0.15-0.3 s each.

> Open: a soft airy whoosh rising into a tiny bubbly pop, light and quick.

> Close: a soft airy whoosh falling away, light and quick.

### sfx_upgrade_buy

- **When:** Speed or Honey Value is bought.
- **Length:** 0.3-0.5 s, 2 variants.

> A satisfying small purchase in a cozy honey game: a thick sweet honey "glug" with a light
> bright coin chime on top, short and rewarding.

### sfx_honey_collect

- **When:** Collect on the Welcome back panel (offline honey).
- **Length:** 1-1.5 s.

> Collecting a big pot of honey: a warm rich pour of honey with a cascade of soft bright coin
> chimes and a sparkling finish, generous and happy.

### sfx_goal_claim

- **When:** the player claims a finished goal on the pill under the top bar; honey drops fly
  into the counter.
- **Length:** 0.8-1.2 s.

> A small achievement is collected: a bright cheerful two-note chime rising (C then F) on
> glockenspiel with a soft sparkle and a light honey "glug" underneath, rewarding and short.

### sfx_reward

- **When:** a rewarded ad grants its reward (x2 honey boost, doubled offline honey).
- **Length:** 0.8-1.2 s.

> A bonus is granted: an uplifting short sparkle sweep with bright glockenspiel and a soft
> "whoosh-ding", cheerful and light.

### sfx_purchase_success

- **When:** a store purchase completes (the Thank you message).
- **Length:** 1.5-2 s.

> A heartfelt thank-you sting: a warm, bright sparkle with a short rising marimba and
> glockenspiel phrase ending on a happy F major chord, celebratory but gentle.

---

## Dewdrops

### sfx_dew_gain

- **When:** Dewdrops are earned (a goal, a finished garden) and the crystals fly into their
  counter. Rarer and more precious than honey: it should sound like a small treasure.
- **Length:** 0.6-1 s.

> A few small crystal gems land in a jar: bright clear glassy chimes, a soft water-drop "plink"
> and a gentle shimmer in F major, precious and light, short.

---

## Butterfly

### sfx_butterfly_appear

- **When:** the butterfly enters the garden (every few minutes). It has to draw the eye without
  startling: quiet, airy, once.
- **Length:** 0.6-1 s.

> A delicate butterfly flutters in: a soft airy fluttering of light wings with a gentle rising
> harp shimmer, magical and very soft, short.

### sfx_butterfly_catch

- **When:** a tap or swipe catches the butterfly; it bursts into sparkles and its honey gift flies
  into the counter.
- **Length:** 0.8-1.2 s.

> A lucky catch in a cozy garden game: a bright playful "pop" bursting into a cascade of soft
> sparkling chimes in F major and a light fluttering whoosh, joyful and generous.

---

## Mixing rules

| Bus | Default | Notes |
| --- | --- | --- |
| Music | 0.6 | ducks to 0.3 under stingers |
| Ambience | 0.35 | dry and bloom loops crossfaded by bloom fraction |
| Swarm | 0.3 | three loops crossfaded by bee count |
| Effects | 0.8 | frequent effects rate-limited as listed |
| UI | 0.7 | |

- Everything pauses while a full-screen ad shows and resumes after it closes.
- Music and sound (effects, interface, ambience, swarm) have separate switches in the settings panel.
- Frequent effects (deposit, shake) use a small pool of sources and drop extra requests; they
  never queue up.
- The game keeps playing with the sound off; nothing in it depends on hearing a cue.
