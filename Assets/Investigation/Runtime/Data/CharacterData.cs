using System;
using System.Collections.Generic;

namespace Investigation
{
    [Serializable]
    public class CharacterData
    {
        public string id = "";
        public string displayName = "";

        // Nombre de sprite a resolver más adelante cuando haya arte (placeholder por ahora).
        public string portraitId = "";

        public List<TopicData> topics = new List<TopicData>();
    }
}
