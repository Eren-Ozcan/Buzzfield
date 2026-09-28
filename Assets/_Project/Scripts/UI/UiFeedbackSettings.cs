using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>Feel of the UI feedback tweens: button press, purchase pop, "cannot buy" wiggle, honey and badge pops.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/UI Feedback Settings", fileName = "UiFeedbackSettings")]
    public sealed class UiFeedbackSettings : ScriptableObject
    {
        [Tooltip("Tweens that can run at once; extra requests are dropped.")]
        [SerializeField, Min(1)] private int tweenCapacity = 48;

        [Header("Button press")]
        [Tooltip("Scale while a button is held down.")]
        [SerializeField, Range(0.5f, 1f)] private float pressScale = 0.93f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.08f;
        [Tooltip("Small overshoot when the button is let go.")]
        [SerializeField, Min(0f)] private float releasePop = 0.05f;
        [SerializeField, Min(0.01f)] private float releaseDuration = 0.18f;

        [Header("Purchase")]
        [SerializeField, Min(0f)] private float purchasePop = 0.14f;
        [SerializeField, Min(0.01f)] private float purchasePopDuration = 0.26f;

        [Header("Cannot buy")]
        [Tooltip("Peak rotation of the wiggle in degrees.")]
        [SerializeField, Min(0f)] private float shakeDegrees = 7f;
        [SerializeField, Min(0.5f)] private float shakeCycles = 3f;
        [SerializeField, Min(0.01f)] private float shakeDuration = 0.35f;

        [Header("Honey counter")]
        [SerializeField, Min(0f)] private float honeyPop = 0.1f;
        [SerializeField, Min(0.01f)] private float honeyPopDuration = 0.2f;
        [Tooltip("Shortest time between two honey pops, so a swarm of deposits does not keep the counter pulsing.")]
        [SerializeField, Min(0f)] private float honeyPopInterval = 0.35f;

        [Header("Badges")]
        [Tooltip("Overshoot when a badge appears; it grows from zero.")]
        [SerializeField, Min(0f)] private float badgePop = 0.35f;
        [SerializeField, Min(0.01f)] private float badgePopDuration = 0.35f;

        public int TweenCapacity => tweenCapacity;
        public float PressScale => pressScale;
        public float PressDuration => pressDuration;
        public float ReleasePop => releasePop;
        public float ReleaseDuration => releaseDuration;
        public float PurchasePop => purchasePop;
        public float PurchasePopDuration => purchasePopDuration;
        public float ShakeDegrees => shakeDegrees;
        public float ShakeCycles => shakeCycles;
        public float ShakeDuration => shakeDuration;
        public float HoneyPop => honeyPop;
        public float HoneyPopDuration => honeyPopDuration;
        public float HoneyPopInterval => honeyPopInterval;
        public float BadgePop => badgePop;
        public float BadgePopDuration => badgePopDuration;
    }
}
