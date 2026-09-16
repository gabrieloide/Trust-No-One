using System.Collections.Generic;

namespace Investigation
{
    public class InMemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> store = new Dictionary<string, string>();

        public bool HasKey(string key) => store.ContainsKey(key);

        public void SetString(string key, string value)
        {
            store[key] = value;
        }

        public string GetString(string key, string defaultValue = null)
        {
            return store.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public void DeleteKey(string key)
        {
            store.Remove(key);
        }

        public void Save()
        {
            // In-memory does not require disk flush.
        }

        public void Clear()
        {
            store.Clear();
        }
    }
}
