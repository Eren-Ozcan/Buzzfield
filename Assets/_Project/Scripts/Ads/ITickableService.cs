namespace Buzzfield.Ads
{
    /// <summary>
    /// A service that needs the frame clock, like the mocks and their fake delays. Real SDKs
    /// call back on their own and do not implement it.
    /// </summary>
    public interface ITickableService
    {
        void Tick(float unscaledDeltaTime);
    }
}
