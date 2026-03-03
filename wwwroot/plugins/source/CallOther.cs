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
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.Plugins
{
    /// <summary>
    /// Provides a plugin implementation that invokes another registered plugin by name.
    /// </summary>
    /// <remarks>
    /// This plugin expects a JSON object with <c>pluginName</c> (the name of the plugin to call) and
    /// <c>pluginData</c> (the input for the target plugin).
    /// </remarks>
    /// <sample>
    /// <code>
    /// var plugin = new CallOtherPlugin();
    /// plugin.Initialize(PluginManager: pm, SystemLogging: log, Language: lang);
    /// var result = plugin.Execute("{ \"pluginName\": \"Hitung Luas Segitiga\", \"pluginData\": { \"alas\": 10, \"tinggi\": 5 } }");
    /// // result = output of the called plugin
    /// </code>
    /// </sample>
    /// <param name="context">A JSON object or string containing <c>pluginName</c> and <c>pluginData</c>.</param>
    /// <returns>
    /// The result returned by the invoked plugin.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>pluginName</c> or <c>pluginData</c> is missing, or the target plugin is not registered, or plugin execution returns null.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="context"/> is null.
    /// </exception>
    /// <remarks>
    /// > [!IMPORTANT]
    /// > This plugin enables dynamic invocation of other plugins and logs all operations and errors using the provided <see cref="SystemLogging"/> instance.
    /// </remarks>
    public class CallOtherPlugin : IPlugin
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

        public string Name { get; } = "Call Other";
        public string Version { get; } = "1.0.4";

        public object? Execute(object? context)
        {
            var sb = new StringBuilder();
            object result = null;

            try
            {
                sb.AppendLine($"[{Name}] :  Starting plugin...");
                context.ThrowIfNull(paramName: nameof(context));

                sb.AppendLine($"[{Name}] : Incoming context:\n" + context.ToString());

                var strData =
                    (context is string str ? JObject.Parse(str) : JObject.FromObject(context))
                    ?? throw new InvalidOperationException("Failed to parse context.");

                sb.AppendLine($"[{Name}] : Parsed JSON:\n" + strData.ToString());

                var pluginName =
                    strData["pluginName"]?.ToString()
                    ?? throw new InvalidOperationException($"[{Name}] : pluginName is required");

                sb.AppendLine($"[{Name}] : Extracted pluginName:\n" + pluginName);

                var pluginData =
                    strData["pluginData"]?.ToString()
                    ?? throw new InvalidOperationException($"[{Name}] : pluginData is required");

                sb.AppendLine($"[{Name}] : Extracted pluginData:\n" + pluginData);

                result =
                    (
                        _pluginManager?.GetActive(pluginName: pluginName) as IPlugin
                        ?? throw new InvalidOperationException(
                            $"[{Name}] : {pluginName} is not registered."
                        )
                    )
                        .Initialize(
                            PluginManager: _pluginManager,
                            SystemLogging: _systemLogging,
                            Language: _language,
                            Cache: _cache,
                            HttpClient: _httpClient,
                            SQLiteHelper: _sqliteHelper,
                            PostgreHelper: _postgreHelper,
                            MongoDatabase: _mongoDatabase
                        )
                        .Execute(context: pluginData)
                    ?? throw new InvalidOperationException(
                        $"[{Name}] : Plugin execution returned null."
                    );

                sb.AppendLine(
                    $"[{Name}] : Plugin execution completed successfully with result. See the result from executed plugin."
                );
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
            var sb = new StringBuilder();
            object result = null;

            try
            {
                sb.AppendLine($"[{Name} Async] : Starting plugin...");
                context.ThrowIfNull(paramName: nameof(context));

                sb.AppendLine($"[{Name} Async] : Incoming context:\n" + context.ToString());

                var strData =
                    (context is string str ? JObject.Parse(str) : JObject.FromObject(context))
                    ?? throw new InvalidOperationException("Failed to parse context.");

                sb.AppendLine($"[{Name} Async] : Parsed JSON:\n" + strData.ToString());

                var pluginName =
                    strData["pluginName"]?.ToString()
                    ?? throw new InvalidOperationException(
                        $"[{Name} Async] : pluginName is required"
                    );

                sb.AppendLine($"[{Name} Async] : Extracted pluginName:\n" + pluginName);

                var pluginData =
                    strData["pluginData"]?.ToString()
                    ?? throw new InvalidOperationException(
                        $"[{Name} Async] : pluginData is required"
                    );

                sb.AppendLine($"[{Name} Async] : Extracted pluginData:\n" + pluginData);

                result =
                    await (
                        _pluginManager?.GetActive(pluginName: pluginName) as IPlugin
                        ?? throw new InvalidOperationException(
                            $"[{Name} Async] : {pluginName} is not registered."
                        )
                    )
                        .Initialize(
                            PluginManager: _pluginManager,
                            SystemLogging: _systemLogging,
                            Language: _language,
                            Cache: _cache,
                            HttpClient: _httpClient,
                            SQLiteHelper: _sqliteHelper,
                            PostgreHelper: _postgreHelper,
                            MongoDatabase: _mongoDatabase
                        )
                        .ExecuteAsync(context: pluginData, cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"[{Name} Async] : Plugin execution returned null."
                    );

                sb.AppendLine(
                    $"[{Name}] : Plugin execution completed successfully with result. See the result from executed plugin."
                );
            }
            catch (Exception ex)
            {
                _systemLogging?.LogError(exception: ex);
                throw;
            }
            finally
            {
                _systemLogging.LogInformation(message: sb.ToString());
            }

            return result;
        }
    }
}
#pragma warning restore S4487, IDE0005, S107, S2325
