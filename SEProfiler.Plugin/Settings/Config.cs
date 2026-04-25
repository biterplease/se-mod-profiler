namespace SEProfiler.Settings
{
    public class Config
    {
        // Empty string = Default Framework (observe all mods).
        // A non-empty string = the Steam Workshop ID of the scoped mod.
        public string SelectedModId { get; set; } = "";

        public static readonly Config Default = new Config();

        // Loaded once on first access (after Plugin.Init runs CommandListener.Start,
        // which creates the watch directory).
        public static Config Current = ConfigStorage.Load();
    }
}
