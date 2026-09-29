namespace Buzzfield.Core
{
    /// <summary>
    /// Firebase Consent Mode v2 flags read from the IAB TCF v2 strings the consent form
    /// writes. Purpose 1 is storing and reading on the device, 3 and 4 personalised ads,
    /// 7 ad measurement. Outside the EEA and the UK everything is granted.
    /// </summary>
    public readonly struct TcfConsent
    {
        public readonly bool AnalyticsStorage;
        public readonly bool AdStorage;
        public readonly bool AdUserData;
        public readonly bool AdPersonalization;

        private TcfConsent(bool analytics, bool storage, bool userData, bool personalization)
        {
            AnalyticsStorage = analytics;
            AdStorage = storage;
            AdUserData = userData;
            AdPersonalization = personalization;
        }

        public static readonly TcfConsent Granted = new TcfConsent(true, true, true, true);

        /// <param name="gdprApplies">IABTCF_gdprApplies: 1 inside the EEA and UK, 0 outside, -1 unknown.</param>
        /// <param name="purposeConsents">IABTCF_PurposeConsents: one '0' or '1' per purpose, purpose 1 first.</param>
        public static TcfConsent From(int gdprApplies, string purposeConsents)
        {
            if (gdprApplies != 1)
                return Granted;
            bool storage = Purpose(purposeConsents, 1);
            return new TcfConsent(storage, storage, storage && Purpose(purposeConsents, 7),
                Purpose(purposeConsents, 3) && Purpose(purposeConsents, 4));
        }

        private static bool Purpose(string consents, int purpose) =>
            consents != null && consents.Length >= purpose && consents[purpose - 1] == '1';
    }
}
