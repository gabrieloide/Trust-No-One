using UnityEngine;

namespace VisualNovelSystem
{
    /// <summary>
    /// Decoupled interface to provide speaker styling, placeholder colors, and protagonist checks
    /// to StoryDialogueUI without coupling the generic engine to specific story casts.
    /// </summary>
    public interface ISpeakerVisualConfigProvider
    {
        bool IsProtagonist(string speakerName);
        Color GetSpeakerColor(string speakerName);
        Sprite GetSpeakerPortrait(string speakerName);
    }
}
