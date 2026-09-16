using System;
using UnityEngine;

namespace Investigation
{
    public class SaveGameService
    {
        public const string DefaultSaveKey = "TrustNoOne_SaveSlot_0";

        private static SaveGameService instance;
        public static SaveGameService Instance => instance ?? (instance = new SaveGameService());

        public ISaveStorage Storage { get; set; } = new PlayerPrefsSaveStorage();

        public static event Action<SaveData> OnGameSaved;
        public static event Action<SaveData> OnGameLoaded;
        public static event Action OnSaveDeleted;

        public bool HasSave(string key = DefaultSaveKey)
        {
            return Storage.HasKey(key);
        }

        public bool Save(string key = DefaultSaveKey)
        {
            if (CaseState.Instance == null) return false;

            try
            {
                SaveData data = CaseState.Instance.CaptureSnapshot();
                data.timestampIso = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(data);
                Storage.SetString(key, json);
                Storage.Save();
                OnGameSaved?.Invoke(data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveGameService] Failed to save game: {ex.Message}");
                return false;
            }
        }

        public bool TryLoad(out SaveData data, string key = DefaultSaveKey)
        {
            data = null;
            if (!HasSave(key)) return false;

            try
            {
                string json = Storage.GetString(key);
                if (string.IsNullOrEmpty(json)) return false;

                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return false;

                if (CaseState.Instance != null)
                {
                    CaseState.Instance.RestoreSnapshot(data);
                }

                OnGameLoaded?.Invoke(data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveGameService] Failed to load save data: {ex.Message}");
                data = null;
                return false;
            }
        }

        public void DeleteSave(string key = DefaultSaveKey)
        {
            if (Storage.HasKey(key))
            {
                Storage.DeleteKey(key);
                Storage.Save();
                OnSaveDeleted?.Invoke();
            }
        }
    }
}
