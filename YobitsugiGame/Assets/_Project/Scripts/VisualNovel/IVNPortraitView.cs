namespace Yobitsugi.VisualNovel
{
    /// <summary>Stage layer for character portraits. Pure presentation; the presenter decides what appears when.</summary>
    public interface IVNPortraitView
    {
        /// <summary><paramref name="instant"/> skips animation, used when replaying stage state after a load.</summary>
        void Apply(VNPortraitCommand command, bool instant);

        /// <summary>Highlights the speaking character and dims the rest.</summary>
        void SetSpeaking(CharacterDefinition character, bool instant);

        void ClearAll(bool instant);
    }
}
