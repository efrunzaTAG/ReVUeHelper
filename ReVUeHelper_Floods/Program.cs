using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReVUeHelper_Floods
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var comparer = new FloodZoneComparer();
            //await comparer.CompareFloodZoneData(33.7250283272419, -84.93122703111693);

            //await Comparer2.CompareGeometriesAsync(33.7250283272419, -84.93122703111693);

            TimeSpan delayBetweenCalls = TimeSpan.FromSeconds(8);


            List<int> idsList1 = Enumerable.Range(22, 31 - 22 + 1).ToList();

            // Define the second list of IDs: 32 through 81.
            List<int> idsList2 = Enumerable.Range(32, 81 - 32 + 1).ToList();

            var numberOfCalls = 100;
            var apiUrl = "http://localhost:5000/api/postAcquisition/tasks/load";

            using (HttpClient client = new HttpClient())
            {
                Console.WriteLine($"Starting API POST calls to: {apiUrl}");
                Console.WriteLine($"Number of calls (each iteration will make two API calls): {numberOfCalls}");
                Console.WriteLine($"Delay between each pair of calls: {delayBetweenCalls.TotalMinutes} minute(s)");
                Console.WriteLine($"IDs for List 1: [{string.Join(", ", idsList1)}]");
                Console.WriteLine($"IDs for List 2: [{string.Join(", ", idsList2)}]");

                // Loop to make the specified number of API calls.
                for (int i = 0; i < numberOfCalls; i++)
                {
                    Console.WriteLine($"\n--- Starting Iteration {i + 1}/{numberOfCalls} ---");

                    // --- Call with IDs from List 1 ---
                    try
                    {
                        Console.WriteLine($"  - Making POST request for List 1 with IDs: [{string.Join(", ", idsList1)}]");

                        // 1. Serialize the array of IDs from List 1 to a JSON string.
                        string jsonContent1 = JsonConvert.SerializeObject(idsList1);

                        // 2. Create StringContent for List 1.
                        StringContent content1 = new StringContent(jsonContent1, Encoding.UTF8, "application/json");

                        // 3. Make the POST request for List 1.
                        HttpResponseMessage response1 = await client.PostAsync(apiUrl, content1);

                        // Ensure the call was successful (status code 200-299).
                        response1.EnsureSuccessStatusCode();

                        // Read the response content as a string for List 1.
                        string responseBody1 = await response1.Content.ReadAsStringAsync();

                        Console.WriteLine($"  - List 1 API Call successful! Status: {response1.StatusCode}");
                        Console.WriteLine($"    List 1 Response Body (first 200 chars): {responseBody1.Substring(0, Math.Min(responseBody1.Length, 200))}...");
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine($"  - Error calling API for List 1 (Attempt {i + 1}/{numberOfCalls}): {e.Message}");
                    }
                    catch (JsonException e)
                    {
                        Console.WriteLine($"  - Error during JSON processing for List 1 (Attempt {i + 1}/{numberOfCalls}): {e.Message}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"  - An unexpected error occurred for List 1 (Attempt {i + 1}/{numberOfCalls}): {e.Message}");
                    }


                    // --- Call with IDs from List 2 ---
                    // This call happens sequentially after the List 1 call within the same iteration.
                    try
                    {
                        Console.WriteLine($"  - Making POST request for List 2 with IDs: [{string.Join(", ", idsList2)}]");

                        // 1. Serialize the array of IDs from List 2 to a JSON string.
                        string jsonContent2 = JsonConvert.SerializeObject(idsList2);

                        // 2. Create StringContent for List 2.
                        StringContent content2 = new StringContent(jsonContent2, Encoding.UTF8, "application/json");

                        // 3. Make the POST request for List 2.
                        HttpResponseMessage response2 = await client.PostAsync(apiUrl, content2);

                        // Ensure the call was successful (status code 200-299).
                        response2.EnsureSuccessStatusCode();

                        // Read the response content as a string for List 2.
                        string responseBody2 = await response2.Content.ReadAsStringAsync();

                        Console.WriteLine($"  - List 2 API Call successful! Status: {response2.StatusCode}");
                        Console.WriteLine($"    List 2 Response Body (first 200 chars): {responseBody2.Substring(0, Math.Min(responseBody2.Length, 200))}...");
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine($"  - Error calling API for List 2 (Attempt {i + 1}/{numberOfCalls}): {e.Message}");
                    }
                    catch (JsonException e)
                    {
                        Console.WriteLine($"  - Error during JSON processing for List 2 (Attempt {i + 1}/{numberOfCalls}): {e.Message}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"  - An unexpected error occurred for List 2 (Attempt {i + 1}/{numberOfCalls}): {e.Message}");
                    }

                    // If it's not the last iteration, wait for the specified delay.
                    if (i < numberOfCalls - 1)
                    {
                        Console.WriteLine($"Waiting for {delayBetweenCalls.TotalMinutes} minute(s) before next iteration...");
                        await Task.Delay(delayBetweenCalls); // Asynchronously wait for the specified duration.
                    }
                }

                Console.WriteLine("\nAll API calls completed.");


            }
        }
    }
}
