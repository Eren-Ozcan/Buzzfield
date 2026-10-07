using Buzzfield.Core;
using TMPro;
using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// Feedback drawn over the garden: "+honey" numbers that rise and fade where honey was
    /// earned, honey drops (or Dewdrop crystals) that fly into their counter for a gift, and a ring
    /// of sparkles around a card that was bought. Fixed pools made by the scene builder, reused oldest first; the
    /// numbers are written into reused buffers, so nothing allocates while it plays.
    /// </summary>
    public sealed class UiEffectsView : MonoBehaviour
    {
        [SerializeField] private RectTransform layer;
        [SerializeField] private TMP_Text[] floatTexts;
        [SerializeField] private RectTransform[] drops;
        [SerializeField] private RectTransform[] sparks;
        [Tooltip("Where flying honey lands: the drop icon of the honey counter.")]
        [SerializeField] private RectTransform honeyTarget;
        [SerializeField] private RectTransform[] dewDrops;
        [Tooltip("Where flying Dewdrops land: the Dewdrops counter.")]
        [SerializeField] private RectTransform dewTarget;

        [Header("Rising numbers")]
        [SerializeField, Min(0.1f)] private float floatSeconds = 0.9f;
        [SerializeField] private float floatRise = 140f;

        [Header("Flying honey")]
        [SerializeField, Min(0.1f)] private float flySeconds = 0.65f;
        [Tooltip("Seconds between two drops of one gift.")]
        [SerializeField, Min(0f)] private float dropStagger = 0.06f;
        [Tooltip("Sideways bulge of the flight path, in canvas units.")]
        [SerializeField] private float flyArc = 160f;

        [Header("Sparkles")]
        [SerializeField, Min(0.1f)] private float sparkSeconds = 0.5f;
        [SerializeField] private float sparkDistance = 120f;

        private struct Motion
        {
            public float Time;
            public float Delay;
            public Vector2 From;
            public Vector2 To;
            public bool Active;
        }

        private static readonly BigNumber MinimumShown = BigNumber.FromDouble(0.1);

        /// <summary>One kind of flying icon: its pool, motions and the counter it lands on.</summary>
        private sealed class Flock
        {
            public RectTransform[] Items;
            public Motion[] Motions;
            public RectTransform Target;
            public int Next;
            public int Flying;
        }

        private Motion[] floats;
        private Flock honey;
        private Flock dew;
        private Motion[] sparkMotions;
        private char[][] buffers;
        private readonly char[] scratch = new char[NumberFormat.MaxLength];
        private int nextFloat;
        private int nextSpark;
        private Tweener tweener;
        private UiFeedbackSettings feedback;

        /// <summary>Honey drops in flight that have not landed yet.</summary>
        public int FlyingDrops => honey != null ? honey.Flying : 0;

        /// <summary>Dewdrop crystals in flight that have not landed yet.</summary>
        public int FlyingDew => dew != null ? dew.Flying : 0;

        /// <summary>Rising numbers on screen now.</summary>
        public int FloatingTexts { get; private set; }

        public void Init(Tweener tweenRunner, UiFeedbackSettings feedbackSettings)
        {
            tweener = tweenRunner;
            feedback = feedbackSettings;
            floats = new Motion[floatTexts.Length];
            honey = CreateFlock(drops, honeyTarget);
            dew = CreateFlock(dewDrops, dewTarget);
            sparkMotions = new Motion[sparks.Length];
            buffers = new char[floatTexts.Length][];
            for (int i = 0; i < floatTexts.Length; i++)
            {
                buffers[i] = new char[NumberFormat.MaxLength + 1];
                NumberLabel.Reserve(floatTexts[i], NumberFormat.MaxLength + 1);
                floatTexts[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < sparks.Length; i++)
                sparks[i].gameObject.SetActive(false);
        }

        /// <summary>"+amount" rising from a screen position; amounts under 0.1 are not worth a number.</summary>
        public void FloatHoney(Vector2 screenPosition, BigNumber amount)
        {
            if (floats == null || floatTexts.Length == 0 || amount < MinimumShown || !ToLayer(screenPosition, out Vector2 at))
                return;
            int i = nextFloat;
            nextFloat = (nextFloat + 1) % floatTexts.Length;
            char[] buffer = buffers[i];
            buffer[0] = '+';
            int digits = NumberFormat.WriteGain(amount, scratch);
            System.Array.Copy(scratch, 0, buffer, 1, digits);
            int length = 1 + digits;
            TMP_Text text = floatTexts[i];
            text.SetCharArray(buffer, 0, length);
            text.gameObject.SetActive(true);
            text.rectTransform.anchoredPosition = at;
            SetAlpha(text, 1f);
            if (!floats[i].Active)
                FloatingTexts++;
            floats[i] = new Motion { From = at, To = at + new Vector2(0f, floatRise), Active = true };
        }

        /// <summary>Honey drops flying from a screen position into the counter, which pops as they land.</summary>
        public void FlyHoney(Vector2 screenPosition, int count) => Launch(honey, screenPosition, count);

        /// <summary>Honey flying from a UI element (the goal pill, a panel button) into the counter.</summary>
        public void FlyHoney(RectTransform source, int count) => Launch(honey, ScreenCenter(source), count);

        /// <summary>Dewdrop crystals flying from a screen position into the Dewdrops counter.</summary>
        public void FlyDew(Vector2 screenPosition, int count) => Launch(dew, screenPosition, count);

        /// <summary>Dewdrop crystals flying from a UI element into the Dewdrops counter.</summary>
        public void FlyDew(RectTransform source, int count) => Launch(dew, ScreenCenter(source), count);

        /// <summary>A ring of sparkles thrown out around a UI element.</summary>
        public void Sparkle(RectTransform source)
        {
            if (sparkMotions == null || sparks.Length == 0
                || !ToLayer(ScreenCenter(source), out Vector2 at))
                return;
            int count = Mathf.Min(8, sparks.Length);
            for (int n = 0; n < count; n++)
            {
                int i = nextSpark;
                nextSpark = (nextSpark + 1) % sparks.Length;
                float angle = (n + 0.5f) * Mathf.PI * 2f / count;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                sparkMotions[i] = new Motion { From = at, To = at + direction * sparkDistance, Active = true };
                sparks[i].anchoredPosition = at;
                sparks[i].gameObject.SetActive(true);
            }
        }

        private void LateUpdate()
        {
            if (floats == null)
                return;
            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < floats.Length; i++)
            {
                if (!floats[i].Active)
                    continue;
                ref Motion m = ref floats[i];
                m.Time += dt;
                float t = m.Time / floatSeconds;
                TMP_Text text = floatTexts[i];
                if (t >= 1f)
                {
                    m.Active = false;
                    FloatingTexts--;
                    text.gameObject.SetActive(false);
                    continue;
                }
                text.rectTransform.anchoredPosition = Vector2.LerpUnclamped(m.From, m.To, 1f - (1f - t) * (1f - t));
                // Pops in, holds, then fades over the last half.
                float scale = t < 0.15f ? Mathf.Lerp(0.6f, 1.1f, t / 0.15f) : 1f;
                text.rectTransform.localScale = Vector3.one * scale;
                SetAlpha(text, t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f);
            }

            Tick(honey, dt);
            Tick(dew, dt);

            for (int i = 0; i < sparkMotions.Length; i++)
            {
                if (!sparkMotions[i].Active)
                    continue;
                ref Motion m = ref sparkMotions[i];
                m.Time += dt;
                float t = m.Time / sparkSeconds;
                RectTransform spark = sparks[i];
                if (t >= 1f)
                {
                    m.Active = false;
                    spark.gameObject.SetActive(false);
                    continue;
                }
                spark.anchoredPosition = Vector2.LerpUnclamped(m.From, m.To, 1f - (1f - t) * (1f - t));
                spark.localScale = Vector3.one * (1f - t);
                spark.localRotation = Quaternion.Euler(0f, 0f, t * 180f);
            }
        }

        private bool ToLayer(Vector2 screenPosition, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPosition, null, out local);

        private static Flock CreateFlock(RectTransform[] items, RectTransform target)
        {
            items = items ?? new RectTransform[0];
            for (int i = 0; i < items.Length; i++)
                items[i].gameObject.SetActive(false);
            return new Flock { Items = items, Motions = new Motion[items.Length], Target = target };
        }

        private void Launch(Flock flock, Vector2 screenPosition, int count)
        {
            if (flock == null || flock.Items.Length == 0 || flock.Target == null
                || !ToLayer(screenPosition, out Vector2 from) || !ToLayer(ScreenCenter(flock.Target), out Vector2 to))
                return;
            for (int n = 0; n < count; n++)
            {
                int i = flock.Next;
                flock.Next = (flock.Next + 1) % flock.Items.Length;
                if (!flock.Motions[i].Active)
                    flock.Flying++;
                flock.Motions[i] = new Motion { From = from, To = to, Delay = n * dropStagger, Active = true };
                RectTransform item = flock.Items[i];
                item.anchoredPosition = from;
                item.localScale = Vector3.zero;
                item.gameObject.SetActive(true);
            }
        }

        /// <summary>Moves a flock along its arcs; each icon that lands pops its counter.</summary>
        private void Tick(Flock flock, float dt)
        {
            for (int i = 0; i < flock.Motions.Length; i++)
            {
                if (!flock.Motions[i].Active)
                    continue;
                ref Motion m = ref flock.Motions[i];
                m.Time += dt;
                float t = (m.Time - m.Delay) / flySeconds;
                RectTransform item = flock.Items[i];
                if (t < 0f)
                    continue;
                if (t >= 1f)
                {
                    m.Active = false;
                    flock.Flying--;
                    item.gameObject.SetActive(false);
                    if (tweener != null)
                        tweener.Pop(flock.Target, feedback.HoneyPop, feedback.HoneyPopDuration);
                    continue;
                }
                float eased = t * t * (3f - 2f * t);
                Vector2 straight = Vector2.LerpUnclamped(m.From, m.To, eased);
                Vector2 side = Vector2.Perpendicular(m.To - m.From).normalized;
                item.anchoredPosition = straight + side * (flyArc * Mathf.Sin(t * Mathf.PI) * (i % 2 == 0 ? 1f : -0.6f));
                item.localScale = Vector3.one * (t < 0.2f ? t / 0.2f : 1f - 0.4f * (t - 0.2f));
            }
        }

        private static void SetAlpha(TMP_Text text, float alpha)
        {
            Color color = text.color;
            if (!Mathf.Approximately(color.a, alpha))
            {
                color.a = alpha;
                text.color = color;
            }
        }

        /// <summary>Screen position of the middle of a UI element on the overlay canvas.</summary>
        private static Vector2 ScreenCenter(RectTransform rect) =>
            RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
}
