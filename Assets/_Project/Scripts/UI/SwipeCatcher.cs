using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Buzzfield.UI
{
    /// <summary>
    /// Invisible full-screen graphic behind every other UI element. The EventSystem sends a
    /// press here only when no button or panel is under the pointer, so every press and drag
    /// it gets is on the game world. Reports the finger's path as strokes in screen pixels:
    /// the press as a zero-length stroke, then one stroke per drag event. One finger at a
    /// time; a new press takes over.
    /// </summary>
    public sealed class SwipeCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler
    {
        private const int NoPointer = int.MinValue;

        private int pointerId = NoPointer;
        private Vector2 last;

        /// <summary>Screen positions where the stroke started and ended.</summary>
        public event Action<Vector2, Vector2> OnStroke;

        /// <summary>A finger touched the world, at this screen position; raised before its first stroke.</summary>
        public event Action<Vector2> OnPress;

        /// <summary>The finger that was swiping lifted.</summary>
        public event Action OnRelease;

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerId = eventData.pointerId;
            last = eventData.position;
            OnPress?.Invoke(last);
            OnStroke?.Invoke(last, last);
        }

        // Strokes follow the finger from the first pixel instead of after the drag threshold.
        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
                return;
            Vector2 position = eventData.position;
            OnStroke?.Invoke(last, position);
            last = position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
                return;
            pointerId = NoPointer;
            OnRelease?.Invoke();
        }

        private void OnDisable()
        {
            if (pointerId != NoPointer)
                OnRelease?.Invoke();
            pointerId = NoPointer;
        }
    }
}
