using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FirebirdApi.Services
{
    public interface ISystemInfoService
    {
        string GetOperatingSystem();
        string GetSystemVersion();
        string GetMachineName();
        string GetArchitecture();
    }

    public class SystemInfoService : ISystemInfoService
    {
        public string GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsVersion();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxDistribution();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSVersion();
            }
            else
            {
                return "Unknown";
            }
        }

        private string GetWindowsVersion()
        {
            try
            {
                // Tentar obter informações mais detalhadas do Windows
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productName = key.GetValue("ProductName")?.ToString();
                        if (!string.IsNullOrEmpty(productName))
                        {
                            return productName;
                        }
                    }
                }
                
                // Fallback para versão básica
                var osVersion = Environment.OSVersion;
                return $"Windows {osVersion.Version.Major}.{osVersion.Version.Minor}";
            }
            catch
            {
                return "Windows";
            }
        }

        private string GetLinuxDistribution()
        {
            try
            {
                if (File.Exists("/etc/os-release"))
                {
                    var lines = File.ReadAllLines("/etc/os-release");
                    var nameLine = lines.FirstOrDefault(l => l.StartsWith("PRETTY_NAME="));
                    if (nameLine != null)
                    {
                        return nameLine.Split('=')[1].Trim('"');
                    }
                    
                    var idLine = lines.FirstOrDefault(l => l.StartsWith("NAME="));
                    if (idLine != null)
                    {
                        return idLine.Split('=')[1].Trim('"');
                    }
                }
                
                return "Linux";
            }
            catch
            {
                return "Linux";
            }
        }

        private string GetMacOSVersion()
        {
            try
            {
                // Para macOS, usar sw_vers se disponível
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sw_vers",
                    Arguments = "-productName",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        if (!string.IsNullOrEmpty(output))
                        {
                            return output;
                        }
                    }
                }
                
                return "macOS";
            }
            catch
            {
                return "macOS";
            }
        }

        public string GetSystemVersion()
        {
            // Retornar a versão do aplicativo Dash Bird
            return "2.1.3";
        }

        public string GetMachineName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "Unknown";
            }
        }

        public string GetArchitecture()
        {
            try
            {
                return RuntimeInformation.OSArchitecture.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
