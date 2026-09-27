using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Buzzfield.UI
{
    /// <summary>
    /// Invisible full-screen graphic behind every other UI element. The EventSystem sends a
    /// press here only when no button or panel is under the pointer, so each call is a tap
    /// on the game world.
    /// </summary>
    public sealed class TapCatcher : MonoBehaviour, IPointerDownHandler
    {
        public event Action OnWorldTapped;

        public void OnPointerDown(PointerEventData eventData) => OnWorldTapped?.Invoke();
    }
}
