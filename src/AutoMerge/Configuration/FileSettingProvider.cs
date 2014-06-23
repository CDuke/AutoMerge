using System;
using System.IO;

namespace AutoMerge
{
    internal class FileSettingProvider : ISettingProvider
    {
        public T ReadValue<T>(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            T value;
            if (!TryReadValue(key, out value))
                throw new InvalidOperationException(string.Format("Setting {0} not found", key));

            return value;
        }

        public bool TryReadValue<T>(string key, out T value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            value = default(T);
            var path = GetSettingFilePath();
            var settingJson = File.ReadAllText(path);
            var settings = JsonParser.ParseJson(settingJson);

            string stringValue;
            if (!settings.TryGetValue(key, out stringValue))
                return false;

            value = (T) Convert.ChangeType(stringValue, typeof(T));
            return true;
        }

        public void WriteValue<T>(string key, T value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var path = GetSettingFilePath();
            var settingJson = File.ReadAllText(path);
            var settings = JsonParser.ParseJson(settingJson);

            settings[key] = value.ToString();

            settingJson = JsonParser.ToJson(settings);
            File.WriteAllText(path, settingJson);
        }

        private static string GetSettingFilePath()
        {
            var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var autoMergeFolder = Path.Combine(roamingPath, "Visual Studio Auto Merge");
            if (!Directory.Exists(autoMergeFolder))
            {
                Directory.CreateDirectory(autoMergeFolder);
            }
            var settingFilePath = Path.Combine(autoMergeFolder, "automerge.conf");
            if (!File.Exists(settingFilePath))
            {
                using (File.Create(settingFilePath))
                {
                }
            }
            return settingFilePath;
        }
    }
}
