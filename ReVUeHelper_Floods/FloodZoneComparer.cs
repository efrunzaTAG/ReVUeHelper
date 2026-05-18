using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json.Linq; // Ensure you have installed Newtonsoft.Json via NuGet
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace ReVUeHelper_Floods
{   
    public class FloodZoneComparer
    {
        private readonly string _connectionString;        

        public FloodZoneComparer()
        {
            _connectionString = "Data Source=sqlProd.saas.amherst.com;Initial Catalog=ThirdPartyData;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUe.Helper";            
        }

        public async Task CompareFloodZoneData(double latitude, double longitude)
        {
            // Step 1: Query the SQL database
            var sqlData = QueryDatabase(latitude, longitude);

            // Step 2: Call the API
            var apiData = await QueryApi(latitude, longitude);

            // Step 3: Compare results
            if (sqlData != null && apiData != null)
            {
                bool sfhaMatch = sqlData.SFHA_TF == apiData.SFHA_TF;
                bool shapeMatch = CompareShapes(sqlData.Geometry, apiData.Geometry);

                Console.WriteLine($"SFHA Match: {sfhaMatch}");
                Console.WriteLine($"Shape Match: {shapeMatch}");
            }
            else
            {
                Console.WriteLine("No matching data found in database or API.");
            }
        }

        private FloodZoneData QueryDatabase(double latitude, double longitude)
        {
            string query = @"
                SELECT TOP 1 sfha_tf, ogr_geometry, cast(ogr_geometry AS VARCHAR(MAX)) AS GeometryJson
                from ThirdPartyData..shp_FEMA_FloodZones_dt fz with (INDEX(""SpatialIndex-Shp_FEMA_FloodZones_dt""))
                where ogr_geometry.STIntersects(geometry::Point(isnull(" + longitude + @",0), isnull("+ latitude + @",0), 4269)) = 1 
            ";

            var geometryJsons = new List<string>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                var point = $"POINT({longitude} {latitude})";
                command.Parameters.AddWithValue("@point", point);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var sfhaTf = reader["sfha_tf"] as string;
                        var geometry = reader["ogr_geometry"] as Microsoft.SqlServer.Types.SqlGeometry;

                        string geometryJson = reader["GeometryJson"].ToString();
                        geometryJsons.Add(geometryJson);

                        return new FloodZoneData
                        {
                            SFHA_TF = sfhaTf,
                            Geometry = geometry,
                            
                        };
                    }
                }
            }

            return null;
        }

        private async Task<FloodZoneData> QueryApi(double latitude, double longitude)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = true;            
            var apiEndpoint = $"https://hazards.fema.gov/arcgis/rest/services/public/NFHL/MapServer/28/query?where=1%3D1&text=&objectIds=&time=&geometry={longitude}%2C{latitude}&geometryType=esriGeometryPoint&inSR=4326&spatialRel=esriSpatialRelWithin&relationParam=&outFields=SFHA_TF,FLD_ZONE,SOURCE_CIT,STUDY_TYP,DFIRM_ID&returnGeometry=true&returnTrueCurves=false&maxAllowableOffset=&geometryPrecision=&outSR=&returnIdsOnly=false&returnCountOnly=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&returnZ=false&returnM=false&gdbVersion=&returnDistinctValues=false&resultOffset=&resultRecordCount=&queryByDistance=&returnExtentsOnly=false&datumTransformation=&parameterValues=&rangeValues=&f=pjson";
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.DefaultRequestHeaders.Referrer = new Uri("https://example.com");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                var response = await httpClient.GetStringAsync(apiEndpoint);
                var json = JsonDocument.Parse(response);

                var sfhaTf = json.RootElement.GetProperty("features")[0].GetProperty("attributes").GetProperty("SFHA_TF").GetString();
                var rings = json.RootElement.GetProperty("features")[0].GetProperty("geometry").GetProperty("rings");

                var geometry = ConvertRingsToSqlGeometry(rings);

                return new FloodZoneData
                {
                    SFHA_TF = sfhaTf,
                    Geometry = geometry
                };
            }
        }

        private SqlGeometry ConvertRingsToSqlGeometry(JsonElement rings)
        {
            var polygonText = "POLYGON((";

            foreach (var ring in rings.EnumerateArray())
            {
                foreach (var point in ring.EnumerateArray())
                {
                    double x = point[0].GetDouble();
                    double y = point[1].GetDouble();
                    polygonText += $"{x} {y},";
                }

                polygonText = polygonText.TrimEnd(',') + "),(";
            }

            polygonText = polygonText.TrimEnd(",(".ToCharArray()) + "))";

            return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars(polygonText), 4326);
        }

        private bool CompareShapes(SqlGeometry dbShape, SqlGeometry apiShape)
        {
            // Normalize and compare
            return dbShape.STEquals(apiShape).IsTrue;
        }

        public bool AreGeometriesEqual(List<Tuple<double, double>> sqlPoints, List<Tuple<double, double>> apiPoints)
        {
            if (sqlPoints.Count != apiPoints.Count) return false;

            for (int i = 0; i < sqlPoints.Count; i++)
            {
                var sqlPoint = Round(sqlPoints[i]);
                var apiPoint = Round(apiPoints[i]);

                if (!ArePointsEqual(sqlPoint, apiPoint))
                    return false;
            }

            return true;
        }

        private static Tuple<double, double> Round(Tuple<double, double> point)
        {
            return new Tuple<double, double>(Math.Round(point.Item1, 5), Math.Round(point.Item2, 5));
        }

        private static bool ArePointsEqual(Tuple<double, double> p1, Tuple<double, double> p2)
        {
            return Math.Abs(p1.Item1 - p2.Item1) < 1e-6 && Math.Abs(p1.Item2 - p2.Item2) < 1e-6;
        }
    }

    public class FloodZoneData
    {
        public string SFHA_TF { get; set; }
        public SqlGeometry Geometry { get; set; }
    }

}

