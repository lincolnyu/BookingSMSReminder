using Android.Icu.Text;
using System.Xml.Linq;
using static Android.Renderscripts.Sampler;

namespace BookingSMSReminder
{
    public class Config
    {
        private static Config? instance_ = null;

        public static Config Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new Config();
                }
                return instance_;
            }
        }

        private Config() { }

        public string ConfigFile
        {
            get
            {
                if (configFile_  == null) {
                    var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    configFile_ = System.IO.Path.Combine(appDataPath, "main.cfg");
                }
                return configFile_;
            }
        }

        private string? configFile_;
        bool loaded_ = false;
        public Dictionary<string, string> KeyValuePair { get; } = new Dictionary<string, string>();

        private void LoadKeyValuePairIfHasNotUnsafe()
        {
            if (loaded_) return;
            if (File.Exists(ConfigFile))
            {
                using var sr = new StreamReader(ConfigFile);
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    var sp = line.Split('=', StringSplitOptions.TrimEntries);
                    if (sp.Length != 2) continue;
                    var k = sp[0];
                    var v = sp[1];
                    KeyValuePair[k] = v;
                }
            }
            loaded_ = true;
        }

        public string? GetValue(string key)
        {
            lock(this)
            {
                LoadKeyValuePairIfHasNotUnsafe();

                if (KeyValuePair.TryGetValue(key, out var val))
                {
                    return val;
                }
                return null;
            }
        }

        public void SetValue(string key, string value)
        {
            lock(this)
            {
                LoadKeyValuePairIfHasNotUnsafe();

                KeyValuePair[key] = value;
            }
        }

        public void ClearValue(string key)
        {
            lock (this)
            {
                KeyValuePair.Remove(key);
            }
        }

        public void Save()
        {
            lock(this)
            {
                LoadKeyValuePairIfHasNotUnsafe();
                using var sw = new StreamWriter(ConfigFile);
                foreach (var (k, v) in KeyValuePair)
                {
                    sw.WriteLine($"{k}={v}");
                }
            }
        }
    }
}
