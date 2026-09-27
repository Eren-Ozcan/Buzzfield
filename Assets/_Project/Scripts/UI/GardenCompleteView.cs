using Buzzfield.Core;
using TMPro;
using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// "Garden Complete!" banner: pops in, holds, fades out. The component is disabled
    /// while hidden, so it only updates during the few seconds it is on screen.
    /// </summary>
    public sealed class GardenCompleteView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform banner;
        [SerializeField] private TMP_Text titleText;
        [SerializeField, Min(0.05f)] private float popDuration = 0.35f;
        [SerializeField, Min(0f)] private float holdDuration = 2.2f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.6f;

        private float time;

        public bool IsShowing => enabled;

        private void Awake()
        {
            titleText.text = Strings.GardenComplete;
            Hide();
        }

        public void Play()
        {
            time = 0f;
            group.alpha = 1f;
            banner.localScale = Vector3.zero;
            gameObject.SetActive(true);
            enabled = true;
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            if (time < popDuration)
            {
                float t = time / popDuration;
                // Overshoot a little, then settle at full size.
                banner.localScale = Vector3.one * (t < 0.7f ? Mathf.Lerp(0f, 1.15f, t / 0.7f) : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f));
                return;
            }
            banner.localScale = Vector3.one;

            float fadeStart = popDuration + holdDuration;
            if (time < fadeStart)
                return;
            float fade = (time - fadeStart) / fadeDuration;
            if (fade >= 1f)
            {
                Hide();
                return;
            }
            group.alpha = 1f - fade;
        }

        private void Hide()
        {
            group.alpha = 0f;
            enabled = false;
        }
    }
}
