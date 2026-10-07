using Buzzfield.Core;
using UnityEngine;

namespace Buzzfield.Platform
{
    /// <summary>
    /// Short vibrations through Android's Vibrator: a light tick or a firmer pulse. Phones below
    /// Android 8 (no VibrationEffect) only get Unity's fixed buzz for important moments, which
    /// also makes Unity add the VIBRATE permission to the manifest. Any failure turns it off.
    /// </summary>
    public sealed class AndroidHaptics : IHapticService
    {
        private const long TickMs = 12;
        private const int TickAmplitude = 60;
        private const long ImportantMs = 40;
        private const int ImportantAmplitude = 190;
        private const int OneShotApi = 26;

        private bool broken;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator;
        private int sdk = -1;
#endif

        public void Pulse(bool important)
        {
            if (broken)
                return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (sdk < 0)
                {
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        sdk = version.GetStatic<int>("SDK_INT");
                }
                if (sdk < OneShotApi)
                {
                    if (important)
                        Handheld.Vibrate();
                    return;
                }
                if (vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject effect = effects.CallStatic<AndroidJavaObject>("createOneShot",
                           important ? ImportantMs : TickMs, important ? ImportantAmplitude : TickAmplitude))
                    vibrator?.Call("vibrate", effect);
            }
            catch (AndroidJavaException e)
            {
                Debug.LogWarning("Vibration failed: " + e.Message);
                broken = true;
            }
#endif
        }
    }
}
