using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json;

namespace ReVUeHelper_Floods
{
    public class Feature
    {
        public Geometry Geometry { get; set; }
    }

    public class Geometry
    {
        public List<Ring> Rings { get; set; }
    }

    public class Ring
    {
        public List<Point> Points { get; set; }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Comparer2
    {
        public static async Task CompareGeometriesAsync(double latitude, double longitude)
        {
            // Step 1: Get SQL Geometry as JSON string
            var sqlGeometryJson = await GetSqlGeometryJsonAsync(latitude, longitude);

            // Step 2: Call API and get Geometry JSON response
            var apiResponse = await CallApiAndGetGeometryJsonAsync(latitude, longitude);

            // Step 3: Parse both geometries into DTOs
            var sqlFeature = JsonConvert.DeserializeObject<Feature>(sqlGeometryJson);
            var apiFeature = JsonConvert.DeserializeObject<Feature>(apiResponse);

            // Step 4: Compare the geometries
            bool areEqual = AreGeometriesEqual(sqlFeature.Geometry, apiFeature.Geometry);

            Console.WriteLine($"\nAre the geometries equal? {areEqual}");
        }

        private static async Task<string> GetSqlGeometryJsonAsync(double latitude, double longitude)
        {
            string connectionString = "Data Source=sqlProd.saas.amherst.com;Initial Catalog=ThirdPartyData;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUe.Helper";
            string query = @"
                SELECT TOP 1 sfha_tf, ogr_geometry, cast(ogr_geometry AS VARCHAR(MAX)) AS GeometryJson
                from ThirdPartyData..shp_FEMA_FloodZones_dt fz with (INDEX(""SpatialIndex-Shp_FEMA_FloodZones_dt""))
                where ogr_geometry.STIntersects(geometry::Point(isnull(" + longitude + @",0), isnull(" + latitude + @",0), 4269)) = 1 
            ";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(query, connection);
                var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    return reader["GeometryJson"].ToString();
                }
            }

            return string.Empty;
        }

        private static async Task<string> CallApiAndGetGeometryJsonAsync(double latitude, double longitude)
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

                //var geometry = ConvertRingsToSqlGeometry(rings);

                return response;
            }
        }

        private static bool AreGeometriesEqual(Geometry sqlGeometry, Geometry apiGeometry)
        {
            // Check if both geometries have the same number of rings and points
            if (sqlGeometry.Rings.Count != apiGeometry.Rings.Count)
                return false;

            for (int i = 0; i < sqlGeometry.Rings.Count; i++)
            {
                var sqlRing = sqlGeometry.Rings[i];
                var apiRing = apiGeometry.Rings[i];

                if (sqlRing.Points.Count != apiRing.Points.Count)
                    return false;

                // Round coordinates to handle minor precision differences
                for (int j = 0; j < sqlRing.Points.Count; j++)
                {
                    var sqlPoint = sqlRing.Points[j];
                    var apiPoint = apiRing.Points[j];

                    if (!ArePointsApproximatelyEqual(sqlPoint, apiPoint))
                        return false;
                }
            }

            return true;
        }

        private static bool ArePointsApproximatelyEqual(Point sqlPoint, Point apiPoint)
        {
            // Round to 4 decimal places for comparison
            double epsilon = 0.0001;

            return Math.Abs(sqlPoint.X - apiPoint.X) < epsilon &&
                   Math.Abs(sqlPoint.Y - apiPoint.Y) < epsilon;
        }
    }
}
