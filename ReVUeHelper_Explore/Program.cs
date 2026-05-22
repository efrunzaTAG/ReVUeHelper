using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ReVUeHelper_Explore;

namespace ElevationApiExample
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // Google Elevation API: $5 per 1,000 requests
        const double GoogleCostPerCall = 0.005;
        const int    GoogleConcurrency = 5;
        const string GoogleApiKeyEnvVar = "GOOGLE_ELEVATION_API_KEY";

        static readonly string DefaultExcelPath =
            @"C:\Users\efrunza\source\repos\efrunzaTAG\ReVUeHelper\.claude\MSR All Homes.xlsx";

        static async Task Main(string[] args)
        {
            string? apiKey = Environment.GetEnvironmentVariable(GoogleApiKeyEnvVar);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine($"ERROR: env var {GoogleApiKeyEnvVar} not set.");
                Console.WriteLine($"Set it (PowerShell): $env:{GoogleApiKeyEnvVar} = '<your-key>'");
                return;
            }

            string excelPath = args.Length > 0 ? args[0] : DefaultExcelPath;
            string outputPath = Path.Combine(
                Path.GetDirectoryName(excelPath)!,
                Path.GetFileNameWithoutExtension(excelPath)
                    + "_with_elevations_"
                    + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                    + ".xlsx");

            var db = new DbProxy();

            // 1) Read Excel
            Console.WriteLine($"Reading {excelPath}");
            var assetIds = ExcelHelper.ReadAssetIds(excelPath);
            var distinctAssetIds = assetIds.Distinct().ToList();
            Console.WriteLine($"Excel rows w/ Property ID: {assetIds.Count}  (distinct: {distinctAssetIds.Count})");

            // 2) Constellation lookup
            Console.WriteLine("Querying constellation (sqlBI.MerchantBankBI)...");
            var constellationRows = db.GetConstellationData(distinctAssetIds);
            Console.WriteLine($"Constellation matches: {constellationRows.Count}  (missing: {distinctAssetIds.Count - constellationRows.Count})");

            // asset_id -> asg_prop_id   (first wins)
            var assetToAsg = new Dictionary<long, long>();
            foreach (var r in constellationRows)
                if (!assetToAsg.ContainsKey(r.AssetId))
                    assetToAsg[r.AssetId] = r.AsgPropId;

            // first row per asg_prop_id (for lat/lng/cbsa)
            var firstByAsg = new Dictionary<long, ConstellationRow>();
            foreach (var r in constellationRows)
                if (!firstByAsg.ContainsKey(r.AsgPropId))
                    firstByAsg[r.AsgPropId] = r;
            Console.WriteLine($"Distinct asg_prop_ids to resolve: {firstByAsg.Count}");

            // 3) Cache lookup (across all EF_Explore_Elevations* tables on sqlDev)
            Console.WriteLine("Loading cached elevations (DBATestBed)...");
            var cache = db.GetCachedElevations();
            Console.WriteLine($"Cache rows loaded: {cache.Count}");

            var needGoogle = firstByAsg.Values.Where(r => !cache.ContainsKey(r.AsgPropId)).ToList();
            int cacheHits  = firstByAsg.Count - needGoogle.Count;
            Console.WriteLine();
            Console.WriteLine($"  Cache hits     : {cacheHits}");
            Console.WriteLine($"  Need Google    : {needGoogle.Count}");
            Console.WriteLine($"  Est. cost      : ${needGoogle.Count * GoogleCostPerCall:F2}  (@ $5 per 1,000)");
            Console.WriteLine();
            Console.Write("Proceed with Google calls? [y/N]: ");
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.KeyChar != 'y' && key.KeyChar != 'Y')
            {
                Console.WriteLine("Aborted before Google calls. No Excel written.");
                return;
            }

            // 4) Google Elevation calls (results stored in _20260518 only on success)
            var googleResults = new ConcurrentDictionary<long, double>();
            var semaphore = new SemaphoreSlim(GoogleConcurrency);
            var tasks = new List<Task>();
            
            int success = 0, failed = 0;

            foreach (var r in needGoogle)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (r.Latitude == null || r.Longitude == null)
                        {
                            Interlocked.Increment(ref failed);
                            Console.WriteLine($"skip {r.AsgPropId}: missing lat/lng");
                            return;
                        }

                        var url = $"https://maps.googleapis.com/maps/api/elevation/json?locations={r.Latitude},{r.Longitude}&key={apiKey}";
                        var resp = await httpClient.GetAsync(url);
                        resp.EnsureSuccessStatusCode();
                        var json = JObject.Parse(await resp.Content.ReadAsStringAsync());

                        var status = json["status"]?.ToString();
                        if (status != "OK")
                        {
                            Interlocked.Increment(ref failed);
                            Console.WriteLine($"{r.AsgPropId}: Google status={status}");
                            return;
                        }

                        var elevation = Convert.ToDouble(json["results"]?[0]?["elevation"]);
                        googleResults[r.AsgPropId] = elevation;
                        db.InsertElevation(r.AsgPropId, r.Latitude, r.Longitude, elevation, r.CBSAName);
                        Interlocked.Increment(ref success);
                        Console.WriteLine($"{r.AsgPropId}: {elevation:F2} m");
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failed);
                        Console.WriteLine($"{r.AsgPropId}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
            Console.WriteLine($"Google calls done. success={success}  failed={failed}");

            // 5) Final lookup for Excel:  asg_prop_id -> (elevation, source)
            var final = new Dictionary<long, (double Elevation, string Source)>();
            foreach (var asgId in firstByAsg.Keys)
                if (cache.TryGetValue(asgId, out var elev))
                    final[asgId] = (elev, "Cache");
            foreach (var kv in googleResults)
                final[kv.Key] = (kv.Value, "Google");

            // 6) Write Excel
            Console.WriteLine($"Writing {outputPath}");
            ExcelHelper.WriteResults(excelPath, outputPath, assetToAsg, firstByAsg, final);
            Console.WriteLine("Done.");
        }
    }
}
