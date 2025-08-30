namespace FirebirdApi.Models
{
    public class ProjectConfig
    {
        public List<DatabaseConfig> Databases { get; set; } = new List<DatabaseConfig>();
        public ProjectSettings Settings { get; set; } = new ProjectSettings();
    }

    public class ProjectSettings
    {
        public string DefaultDatabaseId { get; set; } = "default";
        public bool AutoLoadFromConfig { get; set; } = true;
        public string ConfigFilePath { get; set; } = "database-configs.json";
    }
}
