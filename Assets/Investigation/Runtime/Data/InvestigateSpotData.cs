using System;
using System.Collections.Generic;

namespace Investigation
{
    // Una acción de tipo "Investigar" de la matriz (zona/objeto, no un personaje).
    // Reutiliza DialogueVariantData: cada variante es la descripción que narra Gabe +
    // los efectos que dispara (típicamente CollectClue), resuelta igual que un Topic.
    [Serializable]
    public class InvestigateSpotData
    {
        public string id = "";
        public string displayName = "";
        public List<ConditionData> unlockConditions = new List<ConditionData>();
        public List<DialogueVariantData> variants = new List<DialogueVariantData>();
    }

    [Serializable]
    public class InvestigateSpotDatabaseFile
    {
        public List<InvestigateSpotData> spots = new List<InvestigateSpotData>();
    }
}
