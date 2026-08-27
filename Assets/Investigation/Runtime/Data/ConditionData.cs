using System;

namespace Investigation
{
    public enum ConditionKind
    {
        Flag,           // a = nombre de flag literal
        TopicUnlocked,  // a = characterId, b = topicId
        TopicSeen,      // a = characterId, b = topicId (ya se reprodujo esa variante al menos una vez)
        ClueCollected,  // a = clueId
        Confronted,     // a = characterId, b = clueId
        CounterAtLeast, // a = nombre del contador, b = umbral (int)
    }

    [Serializable]
    public class ConditionData
    {
        public ConditionKind kind;
        public string a = "";
        public string b = "";
        public bool negate = false;
    }
}
