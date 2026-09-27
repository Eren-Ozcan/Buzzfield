using System.Collections;
using Buzzfield.Flowers;
using Buzzfield.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Loads Main.unity and checks the core loop end to end. Any logged error fails the test.
    /// Needs the scene in the build settings (Buzzfield > Build Greybox Scene).
    /// </summary>
    public class GreyboxSmokeTests
    {
        const float SimulatedSeconds = 20f;

        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null, "Main scene has no GameManager.");
        }

        [UnityTearDown]
        public IEnumerator ResetTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartingBee_EarnsHoney()
        {
            Assert.That(game.Bees.Count, Is.EqualTo(1));
            Assert.That(game.Economy.Honey.IsZero);

            Time.timeScale = 4f;
            float end = Time.time + SimulatedSeconds;
            while (Time.time < end)
                yield return null;

            Assert.That(game.Economy.Honey.ToDouble(), Is.GreaterThan(0));
            Assert.That(game.Economy.HoneyPerSecond(Time.timeAsDouble).ToDouble(), Is.GreaterThan(0));
            SaveShotIfRequested("main_1080x1920.png", 1080, 1920);
            SaveShotIfRequested("main_1440x1920.png", 1440, 1920);
        }

        /// <summary>Set BZ_SHOT_DIR to save camera renders (world only, no overlay UI) for a visual check.</summary>
        static void SaveShotIfRequested(string fileName, int width, int height)
        {
            string directory = System.Environment.GetEnvironmentVariable("BZ_SHOT_DIR");
            if (string.IsNullOrEmpty(directory))
                return;

            Camera cam = Camera.main;
            var target = new RenderTexture(width, height, 24);
            float oldAspect = cam.aspect;
            cam.aspect = width / (float)height;
            cam.SendMessage("Refit", SendMessageOptions.DontRequireReceiver);
            cam.targetTexture = target;
            // Overlay canvases skip camera renders; switch to camera space for the shot.
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            RenderMode oldMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = cam.nearClipPlane + 0.1f;
                Canvas.ForceUpdateCanvases();
            }
            cam.Render();
            if (canvas != null)
                canvas.renderMode = oldMode;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, fileName), image.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            cam.aspect = oldAspect;
            Object.Destroy(image);
            target.Release();
        }

        [UnityTest]
        public IEnumerator ManyBees_ShareFlowersWithinLimits()
        {
            for (int i = 0; i < 40; i++)
                game.Bees.Spawn(0);

            Time.timeScale = 4f;
            float end = Time.time + SimulatedSeconds;
            while (Time.time < end)
            {
                foreach (Flower flower in game.Flowers.Flowers)
                {
                    Assert.That(flower.AssignedBees, Is.LessThanOrEqualTo(flower.Type.MaxBeesTargeting));
                    Assert.That(flower.Nectar, Is.InRange(0, flower.Type.MaxNectar));
                    if (!flower.IsActive)
                        Assert.That(flower.AssignedBees, Is.Zero, "A sprout slot was targeted.");
                }
                yield return null;
            }
            Assert.That(game.Economy.Honey.ToDouble(), Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Camera_ShowsWholeGarden()
        {
            yield return null;
            Camera cam = Camera.main;
            foreach (Flower flower in game.Flowers.Flowers)
            {
                Vector3 viewport = cam.WorldToViewportPoint(flower.View.transform.position);
                Assert.That(viewport.x, Is.InRange(0f, 1f), flower.View.name);
                Assert.That(viewport.y, Is.InRange(0f, 1f), flower.View.name);
                Assert.That(viewport.z, Is.GreaterThan(0f), flower.View.name);
            }
        }
    }
}
