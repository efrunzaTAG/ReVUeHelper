using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReVUeHelper_Explore;

namespace ElevationApiExample
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public class BingElevationResponse
        {
            public string AuthenticationResultCode { get; set; }
            public string BrandLogoUri { get; set; }
            public string Copyright { get; set; }
            public List<ResourceSet> ResourceSets { get; set; }
            public int StatusCode { get; set; }
            public string StatusDescription { get; set; }
            public string TraceId { get; set; }
        }

        public class ResourceSet
        {
            public int EstimatedTotal { get; set; }
            public List<Resource> Resources { get; set; }
        }

        public class Resource
        {
            public string __type { get; set; }
            public List<double> Elevations { get; set; }
            public int ZoomLevel { get; set; }
        }

        

        static async Task Main(string[] args)
        {
            var db = new DbProxy();

            var msrHomes = db.GetMsrHomes();
            // Replace with your Google Maps Elevation API key
            string apiKey = "AIzaSyBjEvxNiPIeQNS0OkG9VBA2RrB5r1XfdHk";

            var semaphore = new SemaphoreSlim(5);
            var tasks = new List<Task>();
            foreach (var property in msrHomes)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {

                    string url = $"https://maps.googleapis.com/maps/api/elevation/json?locations={property.Latitude},{property.Longitude}&key={apiKey}";

                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var json = JObject.Parse(jsonResponse);
                        if (json["status"].ToString() == "OK")
                        {
                            var elevation = json["results"][0]["elevation"];

                            var prop = new ExploreProperty();
                            prop.asgPropID = property.asgPropID;
                            prop.Latitude = property.Latitude;
                            prop.Longitude = property.Longitude;
                            prop.Elevation = Convert.ToDouble(elevation);

                            db.InsertProperty(prop);

                            Console.WriteLine($"Elevation for {property.asgPropID} is {elevation} meters.");
                        }
                        else
                        {
                            Console.WriteLine("Error retrieving elevation for "+ property.asgPropID +": " + json["status"]);
                        }

                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine($"Request for {property.asgPropID} had exception: {e.Message}");
                    }
                    finally
                    {
                        semaphore.Release();                // ← Added: free up the slot
                    }


                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine($"✅ All {tasks.Count} API calls completed at {DateTime.Now:O}");

        }


        static async Task Main_BB(string[] args)
        {
            // Replace with your Bing Maps API key
            string apiKey = "AhEpT8aU53XxyuiEHlk5ALpwY5gEzv06TvRkrn9aVKu0Zc4hH3geNHse2tbKHold"; // Insert your Bing Maps API key here
            string latitude = "26.556229634512775"; // Example latitude
            string longitude = "-81.96006792181716"; // Example longitude

            string url = $"http://dev.virtualearth.net/REST/v1/Elevation/List?points={latitude},{longitude}&key={apiKey}";
                            //http://dev.virtualearth.net/REST/v1/Elevation/List?points={lat1,long1,lat2,long2,latN,longnN}&heights={heights}&key={BingMapsKey}

            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var elevationResponse = JsonConvert.DeserializeObject<BingElevationResponse>(jsonResponse);

                // Accessing elevation data from the response
                //var elevation = json["resourceSets"][0]["resources"][0]["elevations"][0].Value<double>();
                //Console.WriteLine($"Elevation at {latitude}, {longitude}: {elevation} meters");
                Console.WriteLine(elevationResponse);

                //elevationResponse.ResourceSets[0].Resources[0].Elevations[0]
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
    }
}
