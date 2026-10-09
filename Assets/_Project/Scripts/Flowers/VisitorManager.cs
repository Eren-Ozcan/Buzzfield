using System;
using Buzzfield.Core;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// Sends the butterfly across the garden now and then and checks the player's strokes
    /// against it. One butterfly at a time, created once and hidden between visits; when it sets
    /// off, the game decides whether it carries the Dewdrops pouch. Plain class: GameManager calls
    /// <see cref="Tick"/> and feeds it the strokes.
    /// </summary>
    public sealed class VisitorManager
    {
        private readonly VisitorSettings settings;
        private readonly ButterflyView butterfly;
        private readonly ParticlePool bursts;
        private readonly GameObject pouch;
        private readonly Func<bool> carriesPouch;
        private readonly System.Random random;

        private VisitorTimer timer;
        private float flightTime;
        private float startX;
        private float endX;
        private float baseZ;
        private float phase;

        /// <param name="parent">Outlives every garden: the butterfly and its sparkles live here.</param>
        /// <param name="nextCarriesPouch">Asked once as each butterfly sets off: true when it carries the pouch.</param>
        public VisitorManager(VisitorSettings settings, Transform parent, int seed, Func<bool> nextCarriesPouch = null)
        {
            this.settings = settings;
            carriesPouch = nextCarriesPouch;
            random = new System.Random(seed);
            if (settings.Prefab != null)
            {
                butterfly = UnityEngine.Object.Instantiate(settings.Prefab, parent);
                butterfly.name = settings.Prefab.name;
                butterfly.transform.localScale = Vector3.one * settings.Scale;
                butterfly.gameObject.SetActive(false);
                if (settings.PouchPrefab != null)
                {
                    pouch = UnityEngine.Object.Instantiate(settings.PouchPrefab, butterfly.transform);
                    pouch.name = settings.PouchPrefab.name;
                    pouch.transform.localPosition = settings.PouchOffset;
                    pouch.SetActive(false);
                }
            }
            if (settings.CatchBurstPrefab != null)
                bursts = new ParticlePool(settings.CatchBurstPrefab, parent, 1);
            RestartTimer();
        }

        /// <summary>The butterfly is crossing the garden now.</summary>
        public bool IsFlying { get; private set; }

        /// <summary>The flying butterfly carries the Dewdrops pouch instead of honey.</summary>
        public bool HasPouch { get; private set; }

        public Vector3 Position => butterfly != null ? butterfly.transform.position : Vector3.zero;

        /// <summary>Calm seconds until the next butterfly once the first-visit gate is open.</summary>
        public double SecondsToNext => timer.Remaining;

        public event Action OnAppeared;

        /// <summary>Raised with the world position where the butterfly was caught and whether it carried the pouch.</summary>
        public event Action<Vector3, bool> OnCaught;

        /// <summary>
        /// Counts down while <paramref name="calm"/> and once <paramref name="playSeconds"/> passed
        /// the first-visit gate, then flies the butterfly across <paramref name="garden"/>.
        /// </summary>
        public void Tick(float deltaTime, bool calm, double playSeconds, Bounds garden)
        {
            if (butterfly == null)
                return;
            if (IsFlying)
            {
                Fly(deltaTime);
                return;
            }
            if (playSeconds < settings.FirstAfterPlaySeconds)
                return;
            if (timer.Tick(deltaTime, calm))
                Launch(garden);
        }

        /// <summary>Sends the butterfly now (play test tools); false when it is already flying.</summary>
        /// <param name="withPouch">Null asks the game as usual; true or false decides here.</param>
        public bool LaunchNow(Bounds garden, bool? withPouch = null)
        {
            if (butterfly == null || IsFlying)
                return false;
            Launch(garden, withPouch);
            return true;
        }

        /// <summary>
        /// Catches the butterfly when the stroke between two screen rays passes within the hit
        /// radius of it, measured on the plane at its height. A tap is a zero-length stroke.
        /// </summary>
        public bool TryCatch(Ray from, Ray to)
        {
            if (!IsFlying)
                return false;
            Vector3 position = butterfly.transform.position;
            var plane = new Plane(Vector3.up, position);
            if (!plane.Raycast(from, out float fromDistance) || !plane.Raycast(to, out float toDistance))
                return false;
            Vector3 a = from.GetPoint(fromDistance);
            Vector3 b = to.GetPoint(toDistance);
            if (!PollenMath.StrokeHits(a.x, a.z, b.x, b.z, position.x, position.z, settings.HitRadius))
                return false;

            bursts?.Play(position);
            bool hadPouch = HasPouch;
            Land();
            OnCaught?.Invoke(position, hadPouch);
            return true;
        }

        /// <summary>Takes the butterfly away without a gift (the garden changes).</summary>
        public void Cancel()
        {
            if (IsFlying)
                Land();
        }

        private void Launch(Bounds garden, bool? withPouch = null)
        {
            HasPouch = withPouch ?? (carriesPouch != null && carriesPouch());
            if (pouch != null)
                pouch.SetActive(HasPouch);
            bool leftToRight = random.NextDouble() < 0.5;
            float left = garden.min.x - settings.EdgeMargin;
            float right = garden.max.x + settings.EdgeMargin;
            startX = leftToRight ? left : right;
            endX = leftToRight ? right : left;
            // Through the middle of the garden, where the camera sees it best.
            baseZ = Mathf.Lerp(garden.min.z, garden.max.z, 0.35f + 0.35f * (float)random.NextDouble());
            phase = (float)(random.NextDouble() * Math.PI * 2.0);
            flightTime = 0f;
            IsFlying = true;
            butterfly.gameObject.SetActive(true);
            Place(0f, 0f);
            OnAppeared?.Invoke();
        }

        private void Fly(float deltaTime)
        {
            float previousT = flightTime / settings.CrossSeconds;
            flightTime += deltaTime;
            float t = flightTime / settings.CrossSeconds;
            if (t >= 1f)
            {
                Land();
                return;
            }
            Place(t, previousT);
        }

        private void Place(float t, float previousT)
        {
            Vector3 position = PathPoint(t);
            Transform body = butterfly.transform;
            Vector3 heading = position - PathPoint(previousT < t ? previousT : t - 0.01f);
            heading.y = 0f;
            body.position = position;
            if (heading.sqrMagnitude > 1e-6f)
                body.rotation = Quaternion.LookRotation(heading, Vector3.up);
            butterfly.Flap(settings.FlapDegrees * Mathf.Abs(Mathf.Sin(flightTime * Mathf.PI * settings.FlapRate)));
        }

        private Vector3 PathPoint(float t)
        {
            float x = Mathf.Lerp(startX, endX, t);
            float z = baseZ + settings.Swing * Mathf.Sin(phase + t * settings.SwingWaves * 2f * Mathf.PI);
            float y = settings.FlightHeight + settings.Bob * Mathf.Sin(phase + t * settings.CrossSeconds * 2.4f);
            return new Vector3(x, y, z);
        }

        private void Land()
        {
            IsFlying = false;
            HasPouch = false;
            butterfly.gameObject.SetActive(false);
            RestartTimer();
        }

        private void RestartTimer() =>
            timer.Restart(random.NextDouble(), settings.MinIntervalSeconds, settings.MaxIntervalSeconds);
    }
}
