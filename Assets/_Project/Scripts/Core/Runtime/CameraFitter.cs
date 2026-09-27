using UnityEngine;

namespace Buzzfield.Core
{
    /// <summary>
    /// Fixed perspective camera that keeps the whole garden on screen for any aspect ratio.
    /// The garden box is fitted into the part of the screen that HUD bars leave free,
    /// at startup and whenever the resolution changes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        const int SearchSteps = 40;

        [SerializeField, Range(20f, 80f)] private float pitch = 50f;
        [Tooltip("Screen fraction covered by the top HUD bar.")]
        [SerializeField, Range(0f, 0.4f)] private float topInset = 0.14f;
        [Tooltip("Screen fraction covered by the bottom upgrade bar.")]
        [SerializeField, Range(0f, 0.4f)] private float bottomInset = 0.16f;
        [Tooltip("Extra world units kept free around the garden.")]
        [SerializeField] private float margin = 0.4f;

        private Camera cam;
        private Bounds bounds;
        private bool hasBounds;
        private int lastWidth;
        private int lastHeight;
        private readonly Vector3[] corners = new Vector3[8];

        /// <summary>Sets the world box to keep visible and fits the camera to it.</summary>
        public void Fit(Bounds garden)
        {
            bounds = garden;
            hasBounds = true;
            Refit();
        }

        private void LateUpdate()
        {
            if (hasBounds && (Screen.width != lastWidth || Screen.height != lastHeight))
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
            // Usable vertical band in tangent space once the HUD bars are taken out.
            float high = tanV * (1f - 2f * topInset);
            float low = tanV * (-1f + 2f * bottomInset);

            // Distance along -forward (d) and shift along up (s) are found together:
            // the smallest d for which some s puts every corner inside the band.
            float minD = 0f;
            float maxD = 1000f;
            for (int step = 0; step < SearchSteps; step++)
            {
                float mid = (minD + maxD) * 0.5f;
                if (Fits(mid, right, up, forward, tanH, low, high, out _))
                    maxD = mid;
                else
                    minD = mid;
            }

            Fits(maxD, right, up, forward, tanH, low, high, out float shift);
            transform.SetPositionAndRotation(bounds.center - forward * maxD + up * shift, rotation);
        }

        private bool Fits(float d, Vector3 right, Vector3 up, Vector3 forward, float tanH, float low, float high, out float shift)
        {
            float minShift = float.MinValue;
            float maxShift = float.MaxValue;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 p = corners[i];
                float depth = Vector3.Dot(p, forward) + d;
                if (depth <= cam.nearClipPlane || Mathf.Abs(Vector3.Dot(p, right)) > tanH * depth)
                {
                    shift = 0f;
                    return false;
                }
                float y = Vector3.Dot(p, up);
                // Camera-space height after shifting by s is y - s; it must stay in [low, high] * depth.
                minShift = Mathf.Max(minShift, y - high * depth);
                maxShift = Mathf.Min(maxShift, y - low * depth);
            }
            shift = (minShift + maxShift) * 0.5f;
            return minShift <= maxShift;
        }
    }
}
