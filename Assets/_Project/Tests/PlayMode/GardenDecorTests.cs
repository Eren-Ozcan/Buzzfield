using System.Collections;
using System.Collections.Generic;
using Buzzfield.Flowers;
using Buzzfield.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Garden decor against the real Main scene: merged meshes, clear of the flowers, bloom tint.</summary>
    public class GardenDecorTests
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Decor_IsMergedPerMaterial()
        {
            var decor = Object.FindAnyObjectByType<GardenDecorView>();
            Assert.That(decor, Is.Not.Null, "Every authored garden has decor.");
            Assert.That(decor.PieceCount, Is.GreaterThan(60));
            Assert.That(decor.RendererCount, Is.LessThanOrEqualTo(4), "One renderer per decor material.");
            Assert.That(decor.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(decor.RendererCount));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Decor_StaysClearOfFlowersAndHive()
        {
            GardenConfig garden = game.Prestige.CurrentGarden;
            GardenDecor decor = garden.Decor;
            var clearance = new Dictionary<GameObject, float>();
            foreach (DecorScatter scatter in decor.Scatters)
                clearance[scatter.prefab] = scatter.clearance;

            int checkedPieces = 0;
            foreach (DecorPiece piece in DecorLayout.Place(garden))
            {
                if (!clearance.TryGetValue(piece.Prefab, out float free))
                    continue;
                var point = new Vector2(piece.Position.x, piece.Position.z);
                Assert.That((point - garden.HivePosition).magnitude, Is.GreaterThanOrEqualTo(decor.HiveClearance - 1e-4f));
                foreach (FlowerSlot slot in garden.Slots)
                    Assert.That((point - slot.position).magnitude, Is.GreaterThanOrEqualTo(free - 1e-4f), $"{piece.Prefab.name} too close to a flower.");
                checkedPieces++;
            }
            Assert.That(checkedPieces, Is.GreaterThan(60));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Decor_SameGardenGetsTheSameLayout()
        {
            GardenConfig garden = game.Prestige.CurrentGarden;
            List<DecorPiece> first = DecorLayout.Place(garden);
            List<DecorPiece> second = DecorLayout.Place(garden);
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
                Assert.That(second[i].Position, Is.EqualTo(first[i].Position));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Decor_GreensAsTheGardenBlooms()
        {
            var decor = Object.FindAnyObjectByType<GardenDecorView>();
            Material tinted = null;
            foreach (MeshRenderer renderer in decor.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.sharedMaterial.name.Contains("DecorGrass"))
                    tinted = renderer.sharedMaterial;
            }
            Assert.That(tinted, Is.Not.Null, "Grass is tinted.");
            GardenDecor data = game.Prestige.CurrentGarden.Decor;
            Color grass = data.Scatters[0].prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetColor(BaseColorId);
            AssertColor(tinted.GetColor(BaseColorId), data.Dry(grass));
            yield return Shot("decor_dry");

            for (int pass = 0; pass < 64 && !game.Bloom.IsComplete; pass++)
            {
                foreach (Flower flower in game.Flowers.Flowers)
                {
                    if (flower.IsActive && !flower.IsBloomed)
                        TestBloom.UntilBloomed(game, flower);
                }
            }
            Assert.That(game.Bloom.IsComplete);
            yield return null;

            AssertColor(tinted.GetColor(BaseColorId), grass);
            yield return Shot("decor_bloomed");
        }

        static IEnumerator Shot(string name)
        {
            yield return null;
            TestShots.SaveIfRequested($"{name}_1080x1920.png", 1080, 1920);
        }

        /// <summary>Material colours go through a colour space round trip, so compare with a tolerance.</summary>
        static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1e-3f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1e-3f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1e-3f));
        }
    }
}
