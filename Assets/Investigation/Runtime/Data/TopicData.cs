using System;
using System.Collections.Generic;

namespace Investigation
{
    // "Topic": aparece en el menú de temas cuando se cumplen unlockConditions.
    // "Presionar": igual que Topic, pero normalmente exige TopicSeen del tema base como condición.
    // "Confrontar": no aparece en el menú de temas — se dispara desde la opción "Confront..."
    // del menú principal (ver ConversationController.PresentEvidenceRoutine); unlockConditions
    // se ignora para este kind.
    public enum TopicKind
    {
        Topic,
        Presionar,
        Confrontar,
    }

    [Serializable]
    public class TopicData
    {
        public string id = "";
        public string displayName = "";
        public TopicKind kind = TopicKind.Topic;
        public List<ConditionData> unlockConditions = new List<ConditionData>();
        public List<DialogueVariantData> variants = new List<DialogueVariantData>();
    }
}
