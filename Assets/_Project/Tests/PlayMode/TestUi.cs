using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Presses and measures the code-built UI the way a finger would: hit tests through the
    /// EventSystem, pointer events on what was hit, rectangles in screen pixels.
    /// </summary>
    internal static class TestUi
    {
        static readonly List<RaycastResult> hits = new List<RaycastResult>();
        static readonly Vector3[] corners = new Vector3[4];

        /// <summary>Presses the active button with this name through its onClick; fails when it is missing or disabled.</summary>
        public static void Press(string name)
        {
            Button button = Find(name);
            Assert.That(button, Is.Not.Null, $"'{name}' not found");
            Assert.That(button.interactable, Is.True, $"'{name}' is not interactable");
            button.onClick.Invoke();
        }

        /// <summary>The first active button with this name, or null.</summary>
        public static Button Find(string name)
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.InstanceID))
            {
                if (button.name == name && button.isActiveAndEnabled)
                    return button;
            }
            return null;
        }

        /// <summary>The topmost UI object under a screen point, or null when nothing takes rays there.</summary>
        public static GameObject TopHit(Vector2 screen)
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return null;
            hits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = screen }, hits);
            return hits.Count > 0 ? hits[0].gameObject : null;
        }

        /// <summary>A finger could press it: the topmost hit at its centre is the control or a child of it.</summary>
        public static bool Reachable(Selectable control)
        {
            GameObject hit = TopHit(Center((RectTransform)control.transform));
            return hit != null && (hit.transform == control.transform || hit.transform.IsChildOf(control.transform));
        }

        /// <summary>
        /// A tap at a screen point as the EventSystem sends it: down, up and click on whatever is
        /// topmost there. Returns what was hit.
        /// </summary>
        public static GameObject Tap(Vector2 screen)
        {
            GameObject hit = TopHit(screen);
            if (hit == null)
                return null;
            var data = new PointerEventData(EventSystem.current)
            {
                pointerId = 0, position = screen, pressPosition = screen, button = PointerEventData.InputButton.Left,
            };
            GameObject down = ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerDownHandler);
            data.pointerPress = down;
            data.rawPointerPress = hit;
            data.eligibleForClick = true;
            ExecuteEvents.ExecuteHierarchy(down != null ? down : hit, data, ExecuteEvents.pointerUpHandler);
            GameObject click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            if (click != null && (down == null || click == down))
                ExecuteEvents.Execute(click, data, ExecuteEvents.pointerClickHandler);
            return hit;
        }

        public static Vector2 Center(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Camera camera = CanvasCamera(rect);
            return (RectTransformUtility.WorldToScreenPoint(camera, corners[0]) + RectTransformUtility.WorldToScreenPoint(camera, corners[2])) / 2f;
        }

        /// <summary>The rectangle in screen pixels, for an overlay canvas or one rendered by a camera.</summary>
        public static Rect ScreenRect(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Camera camera = CanvasCamera(rect);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>
        /// The part of the screen that takes a press for this control: its target graphic's rect
        /// grown or shrunk by the graphic's raycast padding, in screen pixels.
        /// </summary>
        public static Rect HitRect(Selectable control)
        {
            Graphic graphic = control.targetGraphic;
            var rect = (RectTransform)(graphic != null ? graphic.transform : control.transform);
            Rect screen = ScreenRect(rect);
            if (graphic == null)
                return screen;
            Vector4 padding = graphic.raycastPadding;
            // Padding is in the graphic's own units.
            float scale = screen.width / Mathf.Max(1e-3f, rect.rect.width);
            return Rect.MinMaxRect(screen.xMin + padding.x * scale, screen.yMin + padding.y * scale,
                screen.xMax - padding.z * scale, screen.yMax - padding.w * scale);
        }

        static Camera CanvasCamera(Transform transform)
        {
            Canvas canvas = transform.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;
            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        /// <summary>Hierarchy path below the canvas, for messages.</summary>
        public static string Path(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null && parent.GetComponent<Canvas>() == null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
