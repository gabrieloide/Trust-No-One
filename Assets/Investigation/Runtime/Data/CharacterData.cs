using System;
using System.Collections.Generic;

namespace Investigation
{
    [Serializable]
    public class ConfrontationReactionData
    {
        public string clueId = "";
        public List<string> alternativeClueIds = new List<string>();
        public List<DialogueLineData> lines = new List<DialogueLineData>();
        public List<EffectData> effects = new List<EffectData>();
    }

    [Serializable]
    public class AmbientGreetingData
    {
        public int day = 2;
        public int phase = 0; // 0 = cualquiera
        public string text = "";
    }

    [Serializable]
    public class CharacterData
    {
        public string id = "";
        public string displayName = "";

        // Nombre de sprite a resolver más adelante cuando haya arte (placeholder por ahora).
        public string portraitId = "";
        public string defaultRejection = "";

        public List<TopicData> topics = new List<TopicData>();
        public List<ConfrontationReactionData> confrontations = new List<ConfrontationReactionData>();
        public List<AmbientGreetingData> ambientGreetings = new List<AmbientGreetingData>();
    }
}
