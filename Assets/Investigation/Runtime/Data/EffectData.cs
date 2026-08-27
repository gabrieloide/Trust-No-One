using System;

namespace Investigation
{
    public enum EffectKind
    {
        SetFlag,          // a = nombre de flag literal
        ClearFlag,        // a = nombre de flag literal
        UnlockTopic,      // a = characterId, b = topicId
        CollectClue,      // a = clueId
        IncrementCounter, // a = nombre del contador, b = cantidad (int, vacío = 1)
    }

    [Serializable]
    public class EffectData
    {
        public EffectKind kind;
        public string a = "";
        public string b = "";
    }
}
