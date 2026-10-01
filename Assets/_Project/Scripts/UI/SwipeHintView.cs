using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// "Swipe across the flowers" hint above the bottom bar. It breathes gently while shown;
    /// the game hides it for good once the player has shaken a few flowers.
    /// </summary>
    public sealed class SwipeHintView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [Tooltip("Scale swing of the breathing (0.04 = 4%).")]
        [SerializeField, Min(0f)] private float pulseAmount = 0.04f;
        [SerializeField, Min(0.1f)] private float pulsePeriod = 1.6f;

        public bool IsVisible => panel.activeSelf;

        public void SetVisible(bool visible)
        {
            if (panel.activeSelf != visible)
                panel.SetActive(visible);
        }

        private void Update()
        {
            if (!panel.activeSelf)
                return;
            float wave = Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / pulsePeriod));
            panel.transform.localScale = Vector3.one * (1f + pulseAmount * wave);
        }
    }
}
