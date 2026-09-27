using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// Fits its RectTransform to Screen.safeArea (notches, rounded corners, the iOS home
    /// indicator). Re-checks each frame with a cheap compare, so rotation or a resized
    /// window is picked up.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform rect;
        private Rect appliedArea;
        private Vector2Int appliedScreen;

        private void Awake()
        {
            rect = (RectTransform)transform;
        }

        private void OnEnable() => Apply(true);

        private void Update() => Apply(false);

        /// <summary>Anchors (min, max) that cover <paramref name="safeArea"/> on a screen of <paramref name="screenSize"/>.</summary>
        public static (Vector2 min, Vector2 max) ToAnchors(Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return (Vector2.zero, Vector2.one);
            var min = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            var max = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            return (Vector2.Max(Vector2.zero, min), Vector2.Min(Vector2.one, max));
        }

        private void Apply(bool force)
        {
            Rect area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && area == appliedArea && screen == appliedScreen)
                return;
            appliedArea = area;
            appliedScreen = screen;

            (Vector2 min, Vector2 max) = ToAnchors(area, screen);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
