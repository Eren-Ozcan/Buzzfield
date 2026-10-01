using Buzzfield.Core;
using Buzzfield.Flowers;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Pollen shake: strokes from the swipe catcher shake flowers, and every shaken flower
    /// pays honey sized from the bees' current income (<see cref="PollenMath.ShakeNectar"/>)
    /// and grows its bloom by a share of that nectar.
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
        }

        private void DisposePollen()
        {
            if (pollen != null)
                pollen.OnFlowerShaken -= HandleFlowerShaken;
            if (swipeCatcher != null)
                swipeCatcher.OnStroke -= HandleStroke;
        }

        /// <summary>Shakes every flower that has pollen, as one perfect sweep would. Returns how many were shaken.</summary>
        public int ShakeAllFlowers() => pollen.ShakeAll();

        /// <summary>Nectar the next shake is worth; the same on every flower of the garden.</summary>
        public double ShakeNectar() => PollenMath.ShakeNectar(economy.HoneyPerSecond(Time.timeAsDouble),
            pollenSettings.IncomeSeconds, flowerManager.SumActiveValue(), economy.TotalMultiplier, pollenSettings.MinimumNectar);

        /// <summary>Shows the swipe hint until the player has shaken enough flowers.</summary>
        private void RefreshSwipeHint() => swipeHint.SetVisible(stats.flowersShaken < pollenSettings.HintUntilShakes);

        private void HandleStroke(Vector2 from, Vector2 to) =>
            pollen.Stroke(worldCamera.ScreenPointToRay(from), worldCamera.ScreenPointToRay(to));

        private void HandleFlowerShaken(Flower flower)
        {
            double nectar = ShakeNectar();
            economy.Harvest(nectar, flower.Value);
            bloom.AddNectar(flower, nectar * pollenSettings.BloomShare);
            stats.flowersShaken++;
            if (swipeHint.IsVisible)
                RefreshSwipeHint();
        }
    }
}
