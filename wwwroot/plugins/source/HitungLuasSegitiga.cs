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
    /// Provides a plugin implementation to calculate the area of a triangle (Luas Segitiga).
    /// </summary>
    /// <remarks>
    /// This plugin expects a JSON object with <c>alas</c> (base) and <c>tinggi</c> (height) as input.
    /// </remarks>
    /// <sample>
    /// <code>
    /// var plugin = new HitungLuasSegitigaPlugin();
    /// plugin.Initialize(PluginManager: pm, SystemLogging: log, Language: lang);
    /// var result = plugin.Execute("{ \"alas\": 10, \"tinggi\": 5 }");
    /// // result = { Rumus = "0.5 * alas * tinggi", Input = { Alas = 10, Tinggi = 5 }, Hasil = 25 }
    /// </code>
    /// </sample>
    /// <param name="context">A JSON object or string containing <c>alas</c> and <c>tinggi</c> values.</param>
    /// <returns>
    /// An object containing the formula, input values, and the calculated area of the triangle.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>alas</c> or <c>tinggi</c> is missing from the input context.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="context"/> is null.
    /// </exception>
    /// <remarks>
    /// > [!NOTE]
    /// > This plugin logs all operations and errors using the provided <see cref="SystemLogging"/> instance.
    /// </remarks>
    public class HitungLuasSegitigaPlugin : IPlugin
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

        public string Name { get; } = "Hitung Luas Segitiga";
        public string Version { get; } = "1.0.0";

        public object? Execute(object? context)
        {
            var sb = new StringBuilder();
            object result = null;

            try
            {
                sb.AppendLine($"[{Name}] : Starting plugin...");
                context.ThrowIfNull(paramName: nameof(context));

                sb.AppendLine($"[{Name}] : Incoming context:\n" + context?.ToString());

                var json = JObject.Parse(
                    context is string strContext ? strContext : context?.ToString()
                );

                sb.AppendLine($"[{Name}] : Parsed JSON:");
                sb.AppendLine(json.ToString());

                var alasData =
                    json["alas"]?.ToObject<double>()
                    ?? throw new InvalidOperationException(message: $"[{Name}] : Alas is required");

                sb.AppendLine($"[{Name}] : Extracted alas:");
                sb.AppendLine(alasData.ToString());

                var tinggiData =
                    json["tinggi"]?.ToObject<double>()
                    ?? throw new InvalidOperationException(
                        message: $"[{Name}] : Tinggi is required"
                    );

                sb.AppendLine($"[{Name}] : Extracted tinggi:");
                sb.AppendLine(tinggiData.ToString());

                result = new
                {
                    Rumus = "0.5 * alas * tinggi",
                    Input = new { Alas = alasData, Tinggi = tinggiData },
                    Hasil = 0.5 * alasData * tinggiData,
                };

                sb.AppendLine(
                    $"[{Name}] : Calculation result:"
                        + JsonConvert.SerializeObject(
                            value: result,
                            formatting: Formatting.Indented
                        )
                );
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
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore S4487, IDE0005, S107, S2325
