using UnityEngine;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Screenshots for a visual check; nothing is written unless BZ_SHOT_DIR is set.</summary>
    internal static class TestShots
    {
        /// <summary>Set BZ_SHOT_DIR to save camera renders (world plus the overlay UI) for a visual check.</summary>
        public static void SaveIfRequested(string fileName, int width, int height)
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
    }
}
