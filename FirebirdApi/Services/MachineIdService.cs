using System.Text.Json;

namespace FirebirdApi.Services
{
    public interface IMachineIdService
    {
        string GetMachineId();
        void SetMachineId(string machineId);
        bool HasMachineId();
    }

    public class MachineIdService : IMachineIdService
    {
        private readonly string _configFilePath;
        private string? _cachedMachineId;

        public MachineIdService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDirectory = Path.Combine(appDataPath, "Dash Bird");
            Directory.CreateDirectory(appDirectory);
            _configFilePath = Path.Combine(appDirectory, "machine-config.json");
        }

        public string GetMachineId()
        {
            if (!string.IsNullOrEmpty(_cachedMachineId))
            {
                return _cachedMachineId;
            }

            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<MachineConfig>(json);
                    if (config != null && !string.IsNullOrEmpty(config.MachineId))
                    {
                        _cachedMachineId = config.MachineId;
                        return _cachedMachineId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler configuração do MachineId: {ex.Message}");
            }

            // Se não conseguir obter, usar Environment.MachineName como fallback
            return Environment.MachineName;
        }

        public void SetMachineId(string machineId)
        {
            try
            {
                var config = new MachineConfig
                {
                    MachineId = machineId,
                    LastUpdated = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
                _cachedMachineId = machineId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar MachineId: {ex.Message}");
            }
        }

        public bool HasMachineId()
        {
            return !string.IsNullOrEmpty(GetMachineId()) && GetMachineId() != Environment.MachineName;
        }
    }

    public class MachineConfig
    {
        public string MachineId { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
