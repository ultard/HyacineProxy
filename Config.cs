using System.Text.Json;

namespace HyacineProxy;

public class Config
{
    private FileSystemWatcher? _configWatcher;
    private string _configPath = string.Empty;
    private int _initialProxyPort;
    
    public int ProxyPort { get; set; }
    public EndpointConfig Dispatch { get; set; } = new();
    public EndpointConfig SDK { get; set; } = new();
    public List<string> AlwaysIgnoreDomains { get; set; } = [];
    public List<string> RedirectDomains { get; set; } = [];
    public List<string> BlockUrls { get; set; } = [];
    
    public static Config LoadConfig(string path = "config.json")
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Config not found: {path}");
            Console.WriteLine("Creating new config file with default data...");

            var defaultConfig = CreateDefaultConfig();
            defaultConfig.SaveConfig(path);

            Console.WriteLine("File created successfully.");
            
            defaultConfig.StartWatchingConfigChanges(path);
            return defaultConfig;
        }

        var jsonContent = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<Config>(jsonContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (config == null)
        {
            throw new InvalidOperationException("Unable to deserialize config file.");
        }

        config.StartWatchingConfigChanges(path);
        return config;
    }
    
    public void SaveConfig(string path = "config.json")
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        var jsonContent = JsonSerializer.Serialize(this, jsonOptions);
        File.WriteAllText(path, jsonContent);
    }
    
    private void StartWatchingConfigChanges(string configPath)
    {
        if (string.IsNullOrEmpty(configPath))
        {
            Console.WriteLine("Error: Config path is null or empty.");
            return;
        }
        
        var absolutePath = Path.GetFullPath(configPath);
        _configPath = absolutePath;
        _initialProxyPort = ProxyPort;

        var directory = Path.GetDirectoryName(absolutePath);
        var fileName = Path.GetFileName(absolutePath);
        
        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        _configWatcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _configWatcher.Changed += OnConfigFileChanged;
    }
    
    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Task.Run(() =>
        {
            try
            {
                Thread.Sleep(300);

                var jsonContent = File.ReadAllText(_configPath);
                var updatedConfig = JsonSerializer.Deserialize<Config>(jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updatedConfig == null)
                {
                    Console.WriteLine("Error: Unable to deserialize updated config file.");
                    return;
                }
                
                updatedConfig.ProxyPort = _initialProxyPort;
                
                Dispatch = updatedConfig.Dispatch;
                SDK = updatedConfig.SDK;
                AlwaysIgnoreDomains = updatedConfig.AlwaysIgnoreDomains;
                RedirectDomains = updatedConfig.RedirectDomains;
                BlockUrls = updatedConfig.BlockUrls;

                Console.WriteLine($"Config reloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading config: {ex.Message}");
            }
        });
    }
    
    public void StopWatchingConfigChanges()
    {
        if (_configWatcher == null) return;
        _configWatcher.Changed -= OnConfigFileChanged;
        _configWatcher.EnableRaisingEvents = false;
        _configWatcher.Dispose();
        _configWatcher = null;
        Console.WriteLine("Watching of config file stopped.");
    }
    
    private static Config CreateDefaultConfig()
    {
        return new Config
        {
            ProxyPort = 8081,
            Dispatch = new EndpointConfig
            {
                Domain = "localhost",
                Port = 21000,
                RedirectTrigger = [
                    "query_dispatch",
                    "query_gateway",
                    "query_region_list",
                    "query_cur_region"
                ]
            },
            SDK = new EndpointConfig
            {
                Domain = "localhost",
                Port = 20100,
                RedirectTrigger = [
                    "account",
                    "mdk",
                    "combo"
                ]
            },
            RedirectDomains = 
            [
                ".hoyoverse.com",
                ".mihoyo.com",
                ".aliyuncs.com",
                ".bhsr.com",
                ".starrails.com"
            ],
            AlwaysIgnoreDomains = 
            [
                "autopatchcn.bhsr.com",
                "autopatchos.starrails.com"
            ],
            BlockUrls = 
            [
                "/sdk/upload",
                "/sdk/dataUpload",
                "/common/h5log/log/batch",
                "/crash/dataUpload",
                "/crashdump/dataUpload",
                "/client/event/dataUpload",
                "/log",
                "/asm/dataUpload",
                "/sophon/dataUpload",
                "/apm/dataUpload",
                "/2g/dataUpload",
                "/v1/firelog/legacy/log",
                "/h5/upload",
                "/_ts",
                "/perf/config/verify",
                "/ptolemaios_api/api/reportStrategyData",
                "/combo/box/api/config/sdk/combo",
                "/hkrpg_global/combo/granter/api/compareProtocolVersion",
                "/admin/mi18n",
                "/combo/box/api/config/sw/precache",
                "/hkrpg_global/mdk/agreement/api/getAgreementInfos",
                "/device-fp/api/getExtList",
                "/admin/mi18n/plat_os/m09291531181441/m09291531181441-version.json",
                "/admin/mi18n/plat_oversea/m2020030410/m2020030410-version.json"
            ]
        };
    }
}

public class EndpointConfig
{
    public List<string> RedirectTrigger { get; set; } = [];
    public string Domain { get; set; } = string.Empty;
    public int Port { get; set; }
}
