using System;
using Buzzfield.Core;

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

        /// <summary>The player is in a region (EEA, UK) that needs a way back to the consent choices.</summary>
        bool PrivacyOptionsRequired { get; }

        /// <summary>Raised when the consent choices are settled or changed, with the Consent Mode flags.</summary>
        event Action<TcfConsent> ConsentChanged;

        /// <summary>Updates the consent state and shows the form when required; <paramref name="onDone"/> runs once.</summary>
        void RequestConsent(Action onDone);

        /// <summary>Reopens the consent choices; <paramref name="onDone"/> runs once when the form closes.</summary>
        void ShowPrivacyOptions(Action onDone);
    }

    /// <summary>Editor stand-in: consent is granted at once and no privacy options are needed.</summary>
    public sealed class StubConsentService : IConsentService
    {
        public bool CanRequestAds { get; private set; }
        public bool PrivacyOptionsRequired => false;

        public event Action<TcfConsent> ConsentChanged;

        public void RequestConsent(Action onDone)
        {
            CanRequestAds = true;
            ConsentChanged?.Invoke(TcfConsent.Granted);
            onDone?.Invoke();
        }

        public void ShowPrivacyOptions(Action onDone) => onDone?.Invoke();
    }
}
