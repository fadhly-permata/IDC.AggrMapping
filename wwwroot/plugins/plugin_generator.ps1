param(
    [string]$PluginName,
    [string]$Version,
    [string]$SubDirPath
)

if (-not $PluginName -or $PluginName -eq "") {
    $PluginName = Read-Host -Prompt "Enter plugin name (e.g. Hello World)"
}
if (-not $Version -or $Version -eq "") {
    $Version = Read-Host -Prompt "Enter plugin version [default: 1.0.0]"
    if (-not $Version -or $Version -eq "") { $Version = "1.0.0" }
}
if (-not $SubDirPath -or $SubDirPath -eq "") {
    $SubDirPath = Read-Host -Prompt "Enter subdirectory path under 'source' (leave blank for root 'source' folder)"
}

# Remove spaces and add 'Plugin' suffix for filename and class name
$cleanName = ($PluginName -replace '\s+', '')
$className = "${cleanName}Plugin"
$fileName = "${cleanName}.cs"

$sourceFolder = Join-Path $PSScriptRoot "source"
if (-not $SubDirPath -or $SubDirPath -eq "") {
    $targetFolder = $sourceFolder
} else {
    $targetFolder = Join-Path $sourceFolder $SubDirPath
}

if (-not (Test-Path $targetFolder)) {
    New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
}

$filePath = Join-Path $targetFolder $fileName

$template = @"
#pragma warning disable S4487, IDE0005, S107, S2325
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDC.Utilities;
using IDC.Utilities.Comm.Http;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.IO;
using IDC.Utilities.Models.API;
using IDC.Utilities.Plugins;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.Plugins
{
    /// <summary>
    /// Represents a simple $PluginName plugin implementation.
    /// </summary>
    public class $className : IPlugin
    {
        private PluginManager? _pluginManager;
        private SystemLogging? _systemLogging;
        private Language? _language;
        private Caching? _cache;
        private HttpClientUtility? _httpClient;
        private SQLiteHelper? _sqliteHelper;
        private PostgreHelper? _postgreHelper;
        private IMongoDatabase? _mongoDatabase;

        public IPlugin Initialize(
            PluginManager? PluginManager,
            SystemLogging? SystemLogging,
            Language? Language,
            Caching? Cache = null,
            HttpClientUtility? HttpClient = null,
            SQLiteHelper? SQLiteHelper = null,
            PostgreHelper? PostgreHelper = null,
            IMongoDatabase? MongoDatabase = null
        )
        {
            _pluginManager = PluginManager;
            _systemLogging = SystemLogging;
            _language = Language;
            _cache = Cache;
            _httpClient = HttpClient;
            _sqliteHelper = SQLiteHelper;
            _postgreHelper = PostgreHelper;
            _mongoDatabase = MongoDatabase;

            return this;
        }

        public string Name { get; } = "$PluginName";
        public string Version { get; } = "$Version";

        public object? Execute(object? context)
        {
            var sb = new StringBuilder();
            var result = null;

            try
            {
                sb.AppendLine("[$PluginName] : Starting plugin...");

                if (context == null)
                    throw new ArgumentNullException(paramName: nameof(context));

                sb.AppendLine("[$PluginName] : Incoming context:\n" + context?.ToString());

                var strData =
                    context?.ToString()
                    ?? throw new ArgumentNullException(paramName: nameof(context));

                sb.AppendLine(
                    "[$PluginName] : Outgoing response:\n" + $"Hello, {strData}. How are you today?"
                );

                result = $"Hello, {strData}. How are you today?";
            }
            catch (Exception ex)
            {
                _systemLogging?.LogError(exception: ex);
            }
            finally
            {
                _systemLogging?.LogInformation(message: sb.ToString());
            }

            return result;
        }

        public async Task<object?> ExecuteAsync(
            object? context,
            CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore S4487, IDE0005, S107, S2325
"@

Set-Content -Path $filePath -Value $template -Encoding UTF8
Write-Host "Plugin file created: $filePath"
