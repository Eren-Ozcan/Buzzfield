using Buzzfield.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// Press feel for one button: shrinks a little while held, springs back on release and
    /// wiggles when tapped while it cannot be used (for example not enough honey). Views call
    /// <see cref="Confirm"/> or <see cref="Reject"/> when the action behind the tap succeeds or fails.
    /// </summary>
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Button button;

        private Tweener tweener;
        private UiFeedbackSettings settings;
        private bool pressed;
        private bool interactableAtPress;

        public void Init(Tweener tweenRunner, UiFeedbackSettings feedbackSettings)
        {
            tweener = tweenRunner;
            settings = feedbackSettings;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // The click that follows may change the button (a purchase greys it out), so decide now.
            interactableAtPress = button.IsInteractable();
            if (tweener == null || !interactableAtPress)
                return;
            pressed = true;
            tweener.Hold(transform, settings.PressScale, settings.PressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pressed)
                return;
            pressed = false;
            tweener.Pop(transform, settings.ReleasePop, settings.ReleaseDuration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactableAtPress)
                Reject();
        }

        /// <summary>The action went through: a bigger pop.</summary>
        public void Confirm() => tweener?.Pop(transform, settings.PurchasePop, settings.PurchasePopDuration);

        /// <summary>The action was refused: a short wiggle.</summary>
        public void Reject() => tweener?.Wiggle(transform, settings.ShakeDegrees, settings.ShakeCycles, settings.ShakeDuration);

        private void OnDisable()
        {
            // A panel closing mid-press never sends pointer up; snap back to the rest pose.
            pressed = false;
            tweener?.Stop(transform);
        }
    }
}
