using UnityEngine;

namespace Buzzfield.Core
{
    /// <summary>
    /// Fixed perspective camera that keeps the whole garden on screen for any aspect ratio.
    /// The garden box is fitted into the part of the screen that the HUD leaves free: below the
    /// lowest top HUD row and above the bottom bar, as measured on the canvas, or inside the
    /// fixed insets where those reach further. Where the screen has height to spare, the garden
    /// sits centred between the fixed insets as long as that keeps it clear of the HUD. Refits at
    /// startup, when the resolution changes and when a HUD row moves (a notch, a new row).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        const int SearchSteps = 40;

        /// <summary>Change in a measured HUD inset (screen fraction) that refits the camera.</summary>
        const float InsetTolerance = 0.002f;

        [SerializeField, Range(20f, 80f)] private float pitch = 50f;
        [Tooltip("Screen fraction kept free at the top; the garden is centred between this and Bottom Inset where there is room.")]
        [SerializeField, Range(0f, 0.4f)] private float topInset = 0.14f;
        [Tooltip("Screen fraction kept free at the bottom.")]
        [SerializeField, Range(0f, 0.4f)] private float bottomInset = 0.16f;
        [Tooltip("HUD rows along the top of the screen, on the root canvas: the garden stays below all of them.")]
        [SerializeField] private RectTransform[] topHud = new RectTransform[0];
        [Tooltip("HUD along the bottom of the screen, on the root canvas: the garden stays above it.")]
        [SerializeField] private RectTransform[] bottomHud = new RectTransform[0];
        [Tooltip("Extra world units kept free around the garden.")]
        [SerializeField] private float margin = 0.4f;

        private Camera cam;
        private Bounds bounds;
        private bool hasBounds;
        private int lastWidth;
        private int lastHeight;
        private float lastTop;
        private float lastBottom;
        private RectTransform canvasRect;
        private readonly Vector3[] corners = new Vector3[8];
        private readonly Vector3[] rectCorners = new Vector3[4];

        /// <summary>Sets the world box to keep visible and fits the camera to it.</summary>
        public void Fit(Bounds garden)
        {
            bounds = garden;
            hasBounds = true;
            Refit();
        }

        private void LateUpdate()
        {
            if (!hasBounds)
                return;
            MeasureHud(out float top, out float bottom);
            if (Screen.width != lastWidth || Screen.height != lastHeight
                || Mathf.Abs(top - lastTop) > InsetTolerance || Mathf.Abs(bottom - lastBottom) > InsetTolerance)
                Refit();
        }

        private void Refit()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            Vector3 extents = bounds.extents + new Vector3(margin, 0f, margin);
            int n = 0;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                corners[n++] = Vector3.Scale(extents, new Vector3(x, y, z));

            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * cam.aspect;
            // Usable vertical band in tangent space once the HUD is taken out.
            MeasureHud(out lastTop, out lastBottom);
            float high = tanV * (1f - 2f * lastTop);
            float low = tanV * (-1f + 2f * lastBottom);

            // Distance along -forward (d) and shift along up (s) are found together:
            // the smallest d for which some s puts every corner inside the band.
            float minD = 0f;
            float maxD = 1000f;
            for (int step = 0; step < SearchSteps; step++)
            {
                float mid = (minD + maxD) * 0.5f;
                if (Fits(mid, right, up, forward, tanH, low, high, out _, out _))
                    maxD = mid;
                else
                    minD = mid;
            }

            Fits(maxD, right, up, forward, tanH, low, high, out float minShift, out float maxShift);
            // Centred between the fixed insets, unless that would put part of the garden under the HUD.
            Fits(maxD, right, up, forward, tanH, tanV * (-1f + 2f * bottomInset), tanV * (1f - 2f * topInset),
                out float minCentred, out float maxCentred);
            float shift = Mathf.Clamp((minCentred + maxCentred) * 0.5f, minShift, maxShift);
            transform.SetPositionAndRotation(bounds.center - forward * maxD + up * shift, rotation);
        }

        /// <summary>
        /// Screen fractions the HUD covers at the top and the bottom, never less than the fixed insets.
        /// Measured on the canvas, so it follows the safe area and the canvas scale.
        /// </summary>
        private void MeasureHud(out float top, out float bottom)
        {
            top = topInset;
            bottom = bottomInset;
            for (int i = 0; i < topHud.Length; i++)
            {
                if (TryCanvasSpan(topHud[i], out float yMin, out _))
                    top = Mathf.Max(top, 1f - yMin);
            }
            for (int i = 0; i < bottomHud.Length; i++)
            {
                if (TryCanvasSpan(bottomHud[i], out _, out float yMax))
                    bottom = Mathf.Max(bottom, yMax);
            }
        }

        /// <summary>The rect's lowest and highest point as fractions of the canvas height (0 = bottom edge).</summary>
        private bool TryCanvasSpan(RectTransform rect, out float yMin, out float yMax)
        {
            yMin = 1f;
            yMax = 0f;
            if (rect == null || !rect.gameObject.activeInHierarchy)
                return false;
            if (canvasRect == null)
            {
                Canvas canvas = rect.GetComponentInParent<Canvas>();
                if (canvas == null)
                    return false;
                canvasRect = (RectTransform)canvas.rootCanvas.transform;
            }
            Rect area = canvasRect.rect;
            if (area.height <= 0f)
                return false;
            rect.GetWorldCorners(rectCorners);
            for (int i = 0; i < rectCorners.Length; i++)
            {
                float y = (canvasRect.InverseTransformPoint(rectCorners[i]).y - area.yMin) / area.height;
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }
            return true;
        }

        private bool Fits(float d, Vector3 right, Vector3 up, Vector3 forward, float tanH, float low, float high,
            out float minShift, out float maxShift)
        {
            minShift = float.MinValue;
            maxShift = float.MaxValue;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 p = corners[i];
                float depth = Vector3.Dot(p, forward) + d;
                if (depth <= cam.nearClipPlane || Mathf.Abs(Vector3.Dot(p, right)) > tanH * depth)
                    return false;
                float y = Vector3.Dot(p, up);
                // Camera-space height after shifting by s is y - s; it must stay in [low, high] * depth.
                minShift = Mathf.Max(minShift, y - high * depth);
                maxShift = Mathf.Min(maxShift, y - low * depth);
            }
            return minShift <= maxShift;
        }
    }
}
