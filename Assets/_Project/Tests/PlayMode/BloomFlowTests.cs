using System.Collections;
using Buzzfield.Flowers;
using Buzzfield.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Flower bloom, sprouting, ground tiles and Garden Complete against the real Main scene.</summary>
    public class BloomFlowTests
    {
        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        [Test]
        public void NewGarden_StartsAtZeroBloom()
        {
            Assert.That(game.Bloom.BloomedCount, Is.Zero);
            Assert.That(game.Bloom.Percent, Is.Zero);
            Assert.That(game.Bloom.IsComplete, Is.False);
            Assert.That(game.Bloom.TotalSlots, Is.EqualTo(game.Flowers.Flowers.Count));
        }

        [Test]
        public void CollectedNectar_RaisesBloomUntilFlowerBlooms()
        {
            Flower flower = FirstActive();
            double collected = game.Flowers.Collect(flower, 1);
            Assert.That(collected, Is.EqualTo(1));
            Assert.That(flower.Bloom, Is.EqualTo(1f / flower.Type.NectarToBloom).Within(1e-6f));

            double last = 0;
            for (int visits = 0; !flower.IsBloomed && visits < 1000; visits++)
            {
                last = TestBloom.Visit(game, flower);
                collected += last;
            }
            Assert.That(flower.IsBloomed);
            Assert.That(collected, Is.GreaterThanOrEqualTo(flower.Type.NectarToBloom - 1e-3), "Bloomed before enough nectar was collected.");
            Assert.That(collected - last, Is.LessThan(flower.Type.NectarToBloom), "Should have bloomed on an earlier visit.");
            Assert.That(game.Bloom.BloomedCount, Is.EqualTo(1));

            // Bloom never goes back and extra nectar changes nothing.
            TestBloom.Visit(game, flower);
            Assert.That(flower.Bloom, Is.EqualTo(1f));
            Assert.That(game.Bloom.BloomedCount, Is.EqualTo(1));
        }

        [Test]
        public void Bloom_ActivatesNearestSprouts()
        {
            int activeBefore = ActiveCount();
            Flower flower = FirstActive();
            Flower nearestSprout = NearestInactive(flower);
            Assert.That(nearestSprout, Is.Not.Null, "Garden 1 should start with sprout slots.");

            TestBloom.UntilBloomed(game, flower);

            Assert.That(nearestSprout.IsActive);
            Assert.That(nearestSprout.View.gameObject.activeSelf);
            Assert.That(ActiveCount(), Is.EqualTo(activeBefore + 1), "One sprout per bloom (BloomSettings).");
        }

        [UnityTest]
        public IEnumerator Bloom_TurnsHeadFromGreyToFullColour()
        {
            Flower flower = FirstActive();
            var head = flower.View.HeadRenderer.sharedMaterial;
            Color bloomed = flower.Type.BloomedColor;
            Assert.That(ColorDistance(head.GetColor("_BaseColor"), bloomed), Is.GreaterThan(0.1f), "Unbloomed head should be grey.");

            TestBloom.UntilBloomed(game, flower);
            yield return new WaitForSeconds(1f);

            Assert.That(ColorDistance(flower.View.HeadRenderer.sharedMaterial.GetColor("_BaseColor"), bloomed), Is.LessThan(1e-3f));
            Assert.That(flower.View.transform.localScale.x, Is.EqualTo(1f).Within(1e-3f), "Bloom pop should settle back.");
        }

        [UnityTest]
        public IEnumerator Bloom_GreensGroundOnlyNearTheFlower()
        {
            Texture2D tiles = Object.FindAnyObjectByType<GroundView>().Tiles;
            Assert.That(tiles, Is.Not.Null);
            Assert.That(tiles.width, Is.EqualTo(12));
            Assert.That(tiles.height, Is.EqualTo(18));
            Color cornerBefore = tiles.GetPixel(0, tiles.height - 1);

            Flower flower = FirstActive();
            Vector2Int tile = TileUnder(flower, tiles);
            Color before = tiles.GetPixel(tile.x, tile.y);
            TestBloom.UntilBloomed(game, flower);
            yield return null;

            Color after = tiles.GetPixel(tile.x, tile.y);
            Assert.That(after.g - after.r, Is.GreaterThan(before.g - before.r + 0.1f), "Tile under a bloomed flower should turn green.");
            Color cornerAfter = tiles.GetPixel(0, tiles.height - 1);
            Assert.That(cornerAfter.g - cornerAfter.r, Is.LessThan(after.g - after.r), "Far corner should stay greyer.");
            Assert.That(ColorDistance(cornerBefore, cornerAfter), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator AllFlowersBloomed_CompletesGarden()
        {
            int completed = 0;
            game.Bloom.OnGardenCompleted += () => completed++;

            // Blooming wakes sprouts, so keep sweeping until nothing is left.
            for (int pass = 0; pass < 64 && !game.Bloom.IsComplete; pass++)
            {
                foreach (Flower flower in game.Flowers.Flowers)
                {
                    if (flower.IsActive && !flower.IsBloomed)
                        TestBloom.UntilBloomed(game, flower);
                }
            }

            Assert.That(game.Bloom.IsComplete, "Every slot should be reachable through sprouting.");
            Assert.That(game.Bloom.Percent, Is.EqualTo(100));
            Assert.That(completed, Is.EqualTo(1));
            foreach (Flower flower in game.Flowers.Flowers)
                Assert.That(flower.IsActive && flower.IsBloomed, $"{flower.View.name} did not bloom.");

            yield return null;
            var banner = Object.FindAnyObjectByType<Buzzfield.UI.GardenCompleteView>();
            Assert.That(banner.IsShowing);
            var text = GameObject.Find("BloomText").GetComponent<TMPro.TMP_Text>();
            Assert.That(text.text, Is.EqualTo(string.Format(Buzzfield.Core.Strings.BloomFormat, 100)));
        }

        Flower FirstActive()
        {
            foreach (Flower flower in game.Flowers.Flowers)
                if (flower.IsActive && !flower.IsBloomed)
                    return flower;
            return null;
        }

        Flower NearestInactive(Flower from)
        {
            Flower best = null;
            float bestDistance = float.MaxValue;
            foreach (Flower flower in game.Flowers.Flowers)
            {
                if (flower.IsActive)
                    continue;
                float distance = (flower.GardenPosition - from.GardenPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = flower;
                }
            }
            return best;
        }

        int ActiveCount()
        {
            int count = 0;
            foreach (Flower flower in game.Flowers.Flowers)
                if (flower.IsActive)
                    count++;
            return count;
        }

        static Vector2Int TileUnder(Flower flower, Texture2D tiles)
        {
            // Garden 1: 12x18 ground centred on the origin, one tile per world unit.
            int x = Mathf.Clamp(Mathf.FloorToInt(flower.GardenPosition.x + 6f), 0, tiles.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(flower.GardenPosition.y + 9f), 0, tiles.height - 1);
            return new Vector2Int(x, y);
        }

        static float ColorDistance(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}
