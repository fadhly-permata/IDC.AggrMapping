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
    /// Represents a simple Hello World plugin implementation.
    /// </summary>
    public class HelloWorldPlugin : IPlugin
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

        public string Name { get; } = "Hello World";
        public string Version { get; } = "1.0.0";

        public object? Execute(object? context)
        {
            var sb = new StringBuilder();
            object result = null;

            try
            {
                sb.AppendLine($"[{Name}] : Starting plugin...");

                if (context == null)
                    throw new ArgumentNullException(paramName: nameof(context));

                sb.AppendLine($"[{Name}] : Incoming context:\n" + context?.ToString());

                var strData =
                    context?.ToString()
                    ?? throw new ArgumentNullException(paramName: nameof(context));

                result = $"Hello, {strData}. Apa kabarnya?";
                sb.AppendLine($"[{Name}] : Outgoing response:\n{result}");
            }
            catch (Exception ex)
            {
                _systemLogging?.LogError(exception: ex);
                throw;
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
            throw new NotImplementedException(
                message: $"[{Name}] : Asynchronous execution is not implemented. Please use Execute method."
            );
        }
    }
}
#pragma warning restore S4487, IDE0005, S107, S2325
