using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// The butterfly prefab: a body and two wings that beat around the body's long axis. No
    /// Update of its own; <see cref="VisitorManager"/> moves it and calls <see cref="Flap"/>.
    /// </summary>
    public sealed class ButterflyView : MonoBehaviour
    {
        [SerializeField] private Transform leftWing;
        [SerializeField] private Transform rightWing;

        /// <summary>Raises the wings by <paramref name="degrees"/> (0 = flat out to the sides).</summary>
        public void Flap(float degrees)
        {
            // The left wing reaches along -X and the right along +X, so the same lift turns them opposite ways.
            leftWing.localRotation = Quaternion.Euler(0f, 0f, -degrees);
            rightWing.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
