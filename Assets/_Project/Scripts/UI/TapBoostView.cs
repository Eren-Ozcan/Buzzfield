using Buzzfield.Core;
using Buzzfield.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// Small ring above the bottom bar: full and bright when a tap will boost, draining
    /// while the boost runs, refilling dim during the cooldown.
    /// </summary>
    public sealed class TapBoostView : MonoBehaviour
    {
        private enum State { Ready, Active, Cooldown }

        [Tooltip("Filled (radial) image.")]
        [SerializeField] private Image ring;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color readyColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color activeColor = new Color(0.45f, 0.9f, 0.35f);
        [SerializeField] private Color cooldownColor = new Color(0.55f, 0.52f, 0.48f, 0.8f);

        private TapBoost boost;
        private string activeText;
        private State shown = (State)(-1);

        public void Init(BoostManager boosts)
        {
            boost = boosts.Tap;
            activeText = string.Format(Strings.TapBoostActiveFormat, boosts.TapSpeedMultiplier.ToString("0.#"));
            shown = (State)(-1);
        }

        private void Update()
        {
            if (boost == null)
                return;
            double now = Time.timeAsDouble;
            State state = boost.IsActive(now) ? State.Active : boost.IsReady(now) ? State.Ready : State.Cooldown;
            ring.fillAmount = state switch
            {
                State.Active => (float)boost.ActiveFraction(now),
                State.Cooldown => (float)boost.CooldownFraction(now),
                _ => 1f,
            };
            if (state == shown)
                return;
            shown = state;
            ring.color = state == State.Active ? activeColor : state == State.Ready ? readyColor : cooldownColor;
            label.text = state == State.Active ? activeText : state == State.Ready ? Strings.TapBoostReady : string.Empty;
        }
    }
}
