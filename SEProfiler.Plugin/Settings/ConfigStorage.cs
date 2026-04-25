using System;
using System.IO;
using System.Xml.Serialization;
using VRage.FileSystem;
using VRage.Utils;

namespace SEProfiler.Settings
{
    internal static class ConfigStorage
    {
        private const string FileName = "SEModProfiler.cfg";

        // Stored alongside cmd.json and session.jsonl in the profiler watch directory.
        private static string FilePath
        {
            get { return Path.Combine(MyFileSystem.UserDataPath, "SEModProfiler", FileName); }
        }

        public static void Save(Config config)
        {
            var path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var writer = File.CreateText(path))
                new XmlSerializer(typeof(Config)).Serialize(writer, config);
        }

        public static Config Load()
        {
            var path = FilePath;
            if (!File.Exists(path))
                return new Config();

            try
            {
                using (var reader = File.OpenText(path))
                    return (Config)new XmlSerializer(typeof(Config)).Deserialize(reader) ?? new Config();
            }
            catch (Exception)
            {
                MyLog.Default.Warning($"{FileName}: failed to load config from {path}; using defaults");
                return new Config();
            }
        }
    }
}
