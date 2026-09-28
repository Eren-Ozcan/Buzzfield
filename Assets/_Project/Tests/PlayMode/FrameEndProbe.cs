using UnityEngine.Profiling;
using UnityEngine;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Runs after every other script: managed heap growth over Update and LateUpdate this frame (no collection runs in between, so growth = allocations).</summary>
    [DefaultExecutionOrder(32000)]
    public sealed class FrameEndProbe : MonoBehaviour
    {
        public FrameStartProbe StartProbe;
        public long LastScriptBytes;

        private void LateUpdate() => LastScriptBytes = Profiler.GetMonoUsedSizeLong() - StartProbe.StartBytes;
    }
}
