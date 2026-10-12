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
    /// A button drawn smaller than <see cref="UiFeedbackSettings.MinTapSize"/> takes taps in a
    /// margin around it (its graphic's raycast padding), so it looks the same but is easy to hit.
    /// </summary>
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Button button;

        private Tweener tweener;
        private UiFeedbackSettings settings;
        private IUiCues cues;
        private bool pressed;
        private bool interactableAtPress;

        /// <param name="uiCues">Sound and vibration for presses; optional.</param>
        public void Init(Tweener tweenRunner, UiFeedbackSettings feedbackSettings, IUiCues uiCues = null)
        {
            tweener = tweenRunner;
            settings = feedbackSettings;
            cues = uiCues;
            FitTouchArea();
        }

        // Layout rows and stretched buttons change size with the screen.
        private void OnRectTransformDimensionsChange() => FitTouchArea();

        private void FitTouchArea()
        {
            if (settings == null || button == null)
                return;
            Graphic graphic = button.targetGraphic;
            if (graphic == null)
                return;
            Vector2 size = graphic.rectTransform.rect.size;
            float min = settings.MinTapSize;
            // Negative padding grows the area that takes the press beyond the drawn rect.
            float x = -Mathf.Max(0f, min - size.x) * 0.5f;
            float y = -Mathf.Max(0f, min - size.y) * 0.5f;
            graphic.raycastPadding = new Vector4(x, y, x, y);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // The click that follows may change the button (a purchase greys it out), so decide now.
            interactableAtPress = button.IsInteractable();
            if (tweener == null || !interactableAtPress)
                return;
            cues?.Tap();
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
        public void Reject()
        {
            cues?.Refused();
            tweener?.Wiggle(transform, settings.ShakeDegrees, settings.ShakeCycles, settings.ShakeDuration);
        }

        private void OnDisable()
        {
            // A panel closing mid-press never sends pointer up; snap back to the rest pose.
            pressed = false;
            tweener?.Stop(transform);
        }
    }
}
