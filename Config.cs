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

        public string? GetValue(string key)
        {
            if (!File.Exists(ConfigFile)) return null;
            using var sr = new StreamReader(ConfigFile);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                var sp = line.Split('=', StringSplitOptions.TrimEntries);
                if (sp.Length != 2) continue;
                var k = sp[0];
                if (k == key)
                {
                    var v = sp[1];
                    return v;
                }
            }
            return null;
        }

        public void SetValue(string key, string value)
        {
            bool exists = false;
            var kvp = new List<(string, string)>();
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
                    if (k == key)
                    {
                        v = value;
                        exists = true;
                    }
                    kvp.Add((k, v));
                }
                
            }
            if (!exists)
            {
                kvp.Add((key, value));
            }
            {
                using var sw = new StreamWriter(ConfigFile);
                foreach (var (k, v) in kvp)
                {
                    sw.WriteLine($"{k}={v}");
                }
            }
        }
    }
}
