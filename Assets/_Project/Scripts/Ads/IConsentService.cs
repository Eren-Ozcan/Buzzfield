using System;

namespace Buzzfield.Ads
{
    /// <summary>
    /// Privacy consent before any ad request. The device implementation wraps Google's
    /// User Messaging Platform (<c>ConsentInformation.Update</c>, then
    /// <c>ConsentForm.LoadAndShowConsentFormIfRequired</c>).
    /// </summary>
    public interface IConsentService
    {
        /// <summary>Ads may be requested; false until <see cref="RequestConsent"/> has finished.</summary>
        bool CanRequestAds { get; }

        /// <summary>Updates the consent state and shows the form when required; <paramref name="onDone"/> runs once.</summary>
        void RequestConsent(Action onDone);
    }

    /// <summary>Stand-in until the UMP SDK is added: consent is granted at once.</summary>
    public sealed class StubConsentService : IConsentService
    {
        public bool CanRequestAds { get; private set; }

        public void RequestConsent(Action onDone)
        {
            CanRequestAds = true;
            onDone?.Invoke();
        }
    }
}
