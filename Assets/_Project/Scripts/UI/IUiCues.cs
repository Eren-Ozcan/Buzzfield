namespace Buzzfield.UI
{
    /// <summary>
    /// Sound and touch feedback the interface asks for; the game answers with the sound
    /// manager and the phone's vibration, so the views need neither.
    /// </summary>
    public interface IUiCues
    {
        /// <summary>A button that can be used was pressed.</summary>
        void Tap();

        /// <summary>A press that cannot happen (too expensive, locked): goes with the wiggle.</summary>
        void Refused();
    }
}
