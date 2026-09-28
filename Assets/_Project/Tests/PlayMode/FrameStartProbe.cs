using UnityEngine.Profiling;
using UnityEngine;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Runs before every other script: marks the start of the frame's script work for <see cref="FrameEndProbe"/>.</summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class FrameStartProbe : MonoBehaviour
    {
        public long StartBytes;

        private void Update() => StartBytes = Profiler.GetMonoUsedSizeLong();
    }
}
