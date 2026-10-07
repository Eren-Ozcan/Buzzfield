using System.Collections.Generic;
using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Flowers;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Pollen shake: strokes from the swipe catcher shake flowers, and every shaken flower
    /// pays honey sized from the bees' current income (<see cref="PollenMath.ShakeNectar"/>)
    /// and grows its bloom by a share of that nectar. A glowing trail follows the finger, and the
    /// same strokes can catch the butterfly.
    /// </summary>
    public sealed partial class GameManager
    {
        private PollenShaker pollen;
        private Camera worldCamera;

        public PollenShaker Pollen => pollen;

        /// <summary>Needs the tweener, economy, flower and bloom managers.</summary>
        private void InitPollen()
        {
            worldCamera = cameraFitter.GetComponent<Camera>();
            pollen = new PollenShaker(pollenSettings, flowerManager, tweener, worldRoot);
            pollen.OnFlowerShaken += HandleFlowerShaken;
            swipeCatcher.OnStroke += HandleStroke;
            swipeCatcher.OnPress += HandlePress;
            swipeCatcher.OnRelease += HandleRelease;
        }

        private void DisposePollen()
        {
            if (pollen != null)
                pollen.OnFlowerShaken -= HandleFlowerShaken;
            if (swipeCatcher == null)
                return;
            swipeCatcher.OnStroke -= HandleStroke;
            swipeCatcher.OnPress -= HandlePress;
            swipeCatcher.OnRelease -= HandleRelease;
        }

        /// <summary>Shakes every flower that has pollen, as one perfect sweep would. Returns how many were shaken.</summary>
        public int ShakeAllFlowers() => pollen.ShakeAll();

        /// <summary>Nectar the next shake is worth; the same on every flower of the garden.</summary>
        public double ShakeNectar() => PollenMath.ShakeNectar(economy.HoneyPerSecond(Time.timeAsDouble),
            pollenSettings.IncomeSeconds, flowerManager.SumActiveValue(), economy.TotalMultiplier, pollenSettings.MinimumNectar);

        /// <summary>Shows the swipe hint until the player has shaken enough flowers.</summary>
        private void RefreshSwipeHint() => swipeHint.SetVisible(stats.flowersShaken < pollenSettings.HintUntilShakes);

        /// <summary>
        /// Lays the hint's hand sweep across the garden, through the active flower nearest the
        /// middle of the screen, about half the screen wide.
        /// </summary>
        private void RefreshSwipeHintPath()
        {
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            var middle = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 best = middle;
            float nearest = float.MaxValue;
            for (int i = 0; i < flowers.Count; i++)
            {
                if (!flowers[i].IsActive)
                    continue;
                Vector2 point = ScreenPoint(flowers[i].NectarPoint);
                float distance = (point - middle).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    best = point;
                }
            }
            float half = Screen.width * 0.22f;
            swipeHint.SetPath(new Vector2(best.x - half, best.y), new Vector2(best.x + half, best.y));
        }

        private void HandlePress(Vector2 screen) => pollen.BeginTrail(worldCamera.ScreenPointToRay(screen));

        private void HandleRelease() => pollen.EndTrail();

        private void HandleStroke(Vector2 from, Vector2 to)
        {
            Ray start = worldCamera.ScreenPointToRay(from);
            Ray end = worldCamera.ScreenPointToRay(to);
            visitors.TryCatch(start, end);
            pollen.Stroke(start, end);
        }

        private void HandleFlowerShaken(Flower flower)
        {
            double nectar = ShakeNectar();
            BigNumber honey = economy.Harvest(nectar, flower.Value);
            bloom.AddNectar(flower, nectar * pollenSettings.BloomShare);
            stats.flowersShaken++;
            uiEffects.FloatHoney(ScreenPoint(flower.NectarPoint), honey);
            sound.PlayClimb(Sfx.PollenShake);
            Pulse(false);
            if (swipeHint.IsVisible)
                RefreshSwipeHint();
        }
    }
}
