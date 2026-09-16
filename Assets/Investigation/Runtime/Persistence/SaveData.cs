using System;
using System.Collections.Generic;

namespace Investigation
{
    [Serializable]
    public class CounterEntry
    {
        public string key;
        public int value;

        public CounterEntry() { }

        public CounterEntry(string key, int value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;
        public string timestampIso;
        public int currentDay = 1;
        public int currentPhase = 1;
        public int actionsRemainingInPhase = 4;

        public List<string> flags = new List<string>();
        public List<string> unlockedTopics = new List<string>();
        public List<string> seenTopics = new List<string>();
        public List<string> collectedClues = new List<string>();
        public List<string> confrontations = new List<string>();
        public List<CounterEntry> counters = new List<CounterEntry>();
    }
}
