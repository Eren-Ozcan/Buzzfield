using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// "Swipe across the flowers" hint: a pill above the bottom bar that breathes gently, and a
    /// hand that sweeps across the flowers, fades out and starts again. The game gives the sweep
    /// in screen pixels and hides the hint for good once the player has shaken a few flowers.
    /// </summary>
    public sealed class SwipeHintView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [Tooltip("Scale swing of the breathing (0.04 = 4%).")]
        [SerializeField, Min(0f)] private float pulseAmount = 0.04f;
        [SerializeField, Min(0.1f)] private float pulsePeriod = 1.6f;

        [Header("Hand")]
        [Tooltip("Full-screen layer the hand moves in; hidden with the hint.")]
        [SerializeField] private RectTransform handLayer;
        [SerializeField] private RectTransform hand;
        [SerializeField] private CanvasGroup handGroup;
        [Tooltip("Seconds of one sweep, then the pause before the next.")]
        [SerializeField, Min(0.1f)] private float sweepSeconds = 1.1f;
        [SerializeField, Min(0f)] private float pauseSeconds = 0.7f;

        private Vector2 screenFrom;
        private Vector2 screenTo;
        private bool hasPath;
        private float cycle;

        public bool IsVisible => panel.activeSelf;

        /// <summary>The hand is sweeping now (the hint shows and has a path).</summary>
        public bool IsHandSweeping => panel.activeSelf && hasPath && handLayer != null && handLayer.gameObject.activeSelf;

        /// <summary>Where the hand is, in the hand layer's space.</summary>
        public Vector2 HandPosition => hand != null ? hand.anchoredPosition : Vector2.zero;

        public void SetVisible(bool visible)
        {
            if (panel.activeSelf != visible)
                panel.SetActive(visible);
            RefreshHand();
        }

        /// <summary>The sweep, from one screen point to another (over the flowers).</summary>
        public void SetPath(Vector2 from, Vector2 to)
        {
            screenFrom = from;
            screenTo = to;
            hasPath = handLayer != null;
            cycle = 0f;
            RefreshHand();
        }

        private void RefreshHand()
        {
            if (handLayer == null)
                return;
            bool show = panel.activeSelf && hasPath;
            if (handLayer.gameObject.activeSelf != show)
                handLayer.gameObject.SetActive(show);
        }

        private void Update()
        {
            if (!panel.activeSelf)
                return;
            float wave = Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / pulsePeriod));
            panel.transform.localScale = Vector3.one * (1f + pulseAmount * wave);

            if (!hasPath || hand == null)
                return;
            // Mapped every frame: the canvas scale may settle after the path was set, or change with the screen.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(handLayer, screenFrom, null, out Vector2 from)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(handLayer, screenTo, null, out Vector2 to))
                return;
            cycle = (cycle + Time.unscaledDeltaTime) % (sweepSeconds + pauseSeconds);
            float t = Mathf.Clamp01(cycle / sweepSeconds);
            float eased = t * t * (3f - 2f * t);
            hand.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            // Fades in at the start of the sweep and out at its end; hidden during the pause.
            float alpha = cycle > sweepSeconds ? 0f : Mathf.Min(1f, Mathf.Min(t / 0.15f, (1f - t) / 0.2f));
            if (handGroup != null)
                handGroup.alpha = alpha;
            // Pressed down while it sweeps.
            hand.localScale = Vector3.one * (t < 0.1f ? Mathf.Lerp(1.15f, 1f, t / 0.1f) : 1f);
        }
    }
}
