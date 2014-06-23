namespace AutoMerge
{
    internal interface ISettingProvider
    {
        T ReadValue<T>(string key);
        bool TryReadValue<T>(string key, out T value);
        void WriteValue<T>(string key, T value);
    }
}
