using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Look and pacing of the garden bloom: flower tint, animations, sprouting and ground colours.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Bloom Settings", fileName = "BloomSettings")]
    public sealed class BloomSettings : ScriptableObject
    {
        [Header("Flower")]
        [Tooltip("Head colour of a flower that has not bloomed yet.")]
        [SerializeField] private Color unbloomedHeadColor = new Color(0.62f, 0.62f, 0.6f);
        [Tooltip("Share of the bloomed colour shown at 99% progress; the rest arrives with the bloom.")]
        [SerializeField, Range(0f, 1f)] private float progressTint = 0.35f;
        [SerializeField, Min(0.05f)] private float bloomDuration = 0.6f;
        [Tooltip("Extra head scale at the peak of the bloom pop (0.35 = +35%).")]
        [SerializeField, Min(0f)] private float bloomScalePop = 0.35f;
        [SerializeField] private ParticleSystem bloomBurstPrefab;
        [Tooltip("Burst instances kept alive and reused in turn.")]
        [SerializeField, Min(1)] private int burstPoolSize = 6;

        [Header("Sprouting")]
        [Tooltip("Nearest inactive sprout slots activated by one bloom.")]
        [SerializeField, Range(0, 4)] private int sproutsPerBloom = 2;
        [SerializeField, Min(0.05f)] private float sproutGrowDuration = 0.8f;

        [Header("Ground")]
        [SerializeField] private Color groundGrey = new Color(0.56f, 0.56f, 0.53f);
        [SerializeField] private Color groundGreen = new Color(0.38f, 0.68f, 0.3f);
        [Tooltip("Green every tile gets at 100% garden bloom, so gaps between flowers fill in too.")]
        [SerializeField, Range(0f, 1f)] private float gardenWideGreen = 0.6f;

        [Header("Garden Complete")]
        [SerializeField] private ParticleSystem confettiPrefab;
        [SerializeField, Min(0.05f)] private float completePulseDuration = 0.8f;
        [SerializeField, Min(0f)] private float completePulseScale = 0.25f;

        public Color UnbloomedHeadColor => unbloomedHeadColor;
        public float ProgressTint => progressTint;
        public float BloomDuration => bloomDuration;
        public float BloomScalePop => bloomScalePop;
        public ParticleSystem BloomBurstPrefab => bloomBurstPrefab;
        public int BurstPoolSize => burstPoolSize;
        public int SproutsPerBloom => sproutsPerBloom;
        public float SproutGrowDuration => sproutGrowDuration;
        public Color GroundGrey => groundGrey;
        public Color GroundGreen => groundGreen;
        public float GardenWideGreen => gardenWideGreen;
        public ParticleSystem ConfettiPrefab => confettiPrefab;
        public float CompletePulseDuration => completePulseDuration;
        public float CompletePulseScale => completePulseScale;
    }
}
