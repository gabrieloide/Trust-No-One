using UnityEngine;

namespace Investigation
{
    public class PlayerPrefsSaveStorage : ISaveStorage
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public string GetString(string key, string defaultValue = null)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
