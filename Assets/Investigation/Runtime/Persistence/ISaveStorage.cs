namespace Investigation
{
    public interface ISaveStorage
    {
        bool HasKey(string key);
        void SetString(string key, string value);
        string GetString(string key, string defaultValue = null);
        void DeleteKey(string key);
        void Save();
    }
}
