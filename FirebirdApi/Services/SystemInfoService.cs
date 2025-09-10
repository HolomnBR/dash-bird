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
#if WINDOWS
                // Tentar obter informações mais detalhadas do Windows
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productName = key.GetValue("ProductName")?.ToString();
                        if (!string.IsNullOrEmpty(productName))
                        {
                            // Verificar se é Windows 11 baseado no build number
                            var buildNumber = key.GetValue("CurrentBuild")?.ToString();
                            if (!string.IsNullOrEmpty(buildNumber) && int.TryParse(buildNumber, out int build))
                            {
                                if (build >= 22000)
                                {
                                    return "Windows 11";
                                }
                                else if (build >= 10240)
                                {
                                    return "Windows 10";
                                }
                            }
                            
                            return productName;
                        }
                    }
                }
#endif
                
                // Fallback: usar informações do sistema para detectar Windows 10 vs 11
                var osVersion = Environment.OSVersion;
                var majorVersion = osVersion.Version.Major;
                var minorVersion = osVersion.Version.Minor;
                var osBuildNumber = osVersion.Version.Build;
                
                if (majorVersion == 10)
                {
                    if (osBuildNumber >= 22000)
                    {
                        return "Windows 11";
                    }
                    else
                    {
                        return "Windows 10";
                    }
                }
                
                return $"Windows {majorVersion}.{minorVersion}";
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
            try
            {
                // Tentar ler a versão do arquivo de projeto
                var projectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "FirebirdApi.csproj");
                if (File.Exists(projectPath))
                {
                    var projectContent = File.ReadAllText(projectPath);
                    var versionMatch = System.Text.RegularExpressions.Regex.Match(projectContent, @"<Version>([^<]+)</Version>");
                    if (versionMatch.Success)
                    {
                        return versionMatch.Groups[1].Value;
                    }
                }
                
                // Fallback: tentar ler do assembly
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                if (version != null)
                {
                    return $"{version.Major}.{version.Minor}.{version.Build}";
                }
                
                return "2.1.3"; // Fallback final
            }
            catch
            {
                return "2.1.3"; // Fallback em caso de erro
            }
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
