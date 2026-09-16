using System;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    /// <summary>
    /// Implements ISpeakerVisualConfigProvider for Trust No One, providing noir detective colors
    /// and protagonist checking to StoryDialogueUI without coupling the generic engine.
    /// </summary>
    public class InvestigationSpeakerVisualConfig : MonoBehaviour, ISpeakerVisualConfigProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            StoryDialogueUI.SpeakerConfigProvider = new DefaultInvestigationSpeakerConfig();
        }

        public bool IsProtagonist(string speakerName)
        {
            if (string.IsNullOrEmpty(speakerName)) return false;
            return speakerName.Equals("Gabe", StringComparison.OrdinalIgnoreCase) ||
                   speakerName.Equals("Protagonista", StringComparison.OrdinalIgnoreCase) ||
                   speakerName.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                   speakerName.Equals("Gabriel", StringComparison.OrdinalIgnoreCase);
        }

        public Color GetSpeakerColor(string speakerName)
        {
            if (IsProtagonist(speakerName)) return new Color(0.35f, 0.50f, 0.70f, 1f);

            switch (speakerName.ToLowerInvariant())
            {
                case "elena": return new Color(0.85f, 0.45f, 0.45f, 1f);
                case "ernesto": return new Color(0.75f, 0.55f, 0.35f, 1f);
                case "robert": return new Color(0.45f, 0.65f, 0.75f, 1f);
                case "mark": return new Color(0.65f, 0.45f, 0.75f, 1f);
                case "frank": return new Color(0.75f, 0.75f, 0.45f, 1f);
                case "gus": return new Color(0.45f, 0.75f, 0.55f, 1f);
                case "marta": return new Color(0.85f, 0.65f, 0.55f, 1f);
                default: return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        public Sprite GetSpeakerPortrait(string speakerName)
        {
            return null;
        }

        private class DefaultInvestigationSpeakerConfig : ISpeakerVisualConfigProvider
        {
            public bool IsProtagonist(string speakerName)
            {
                if (string.IsNullOrEmpty(speakerName)) return false;
                return speakerName.Equals("Gabe", StringComparison.OrdinalIgnoreCase) ||
                       speakerName.Equals("Protagonista", StringComparison.OrdinalIgnoreCase) ||
                       speakerName.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                       speakerName.Equals("Gabriel", StringComparison.OrdinalIgnoreCase);
            }

            public Color GetSpeakerColor(string speakerName)
            {
                if (IsProtagonist(speakerName)) return new Color(0.35f, 0.50f, 0.70f, 1f);

                switch (speakerName.ToLowerInvariant())
                {
                    case "elena": return new Color(0.85f, 0.45f, 0.45f, 1f);
                    case "ernesto": return new Color(0.75f, 0.55f, 0.35f, 1f);
                    case "robert": return new Color(0.45f, 0.65f, 0.75f, 1f);
                    case "mark": return new Color(0.65f, 0.45f, 0.75f, 1f);
                    case "frank": return new Color(0.75f, 0.75f, 0.45f, 1f);
                    case "gus": return new Color(0.45f, 0.75f, 0.55f, 1f);
                    case "marta": return new Color(0.85f, 0.65f, 0.55f, 1f);
                    default: return new Color(0.6f, 0.6f, 0.6f, 1f);
                }
            }

            public Sprite GetSpeakerPortrait(string speakerName) => null;
        }
    }
}
