// See https://aka.ms/new-console-template for more information
using Azure.Core;
using ReVUeHelper_OSS;
using System.Linq;
using System.Net;
using System.Text;

Console.WriteLine("Press Ctrl+C to exit.");

List<long> itemsWithSentEmail = new List<long>(); // Global variable to track items with sent emails

var db = new ReVUeDbProxy();

while (true)
{
    DateTime currentTime = DateTime.Now;
    if (currentTime.Hour >= 8 && currentTime.Hour < 21)
    {
        var outOfSyncItems = db.GetOutOfSyncItems();
        var resultStringBuilder = new StringBuilder();
        var foundCount = 0;
        var pingFoundIDs = new List<long>();
        foreach (var item in outOfSyncItems)
        {
            if (!itemsWithSentEmail.Contains(item.Id))
            {
                if (item.OppStatus_Wto != item.OppStatus_Opp)
                {
                    resultStringBuilder.AppendLine($"Id: {item.Id}, OppStatus_Wto: {item.OppStatus_Wto}, OppStatus_Opp: {item.OppStatus_Opp}, IsProcessing: {item.IsProcessing}");
                    resultStringBuilder.Append("\r");
                    foundCount++;
                    itemsWithSentEmail.Add(item.Id);
                    pingFoundIDs.Add(item.Id);
                }
            }
          
        }

        if (foundCount > 0)
        {
            if (pingFoundIDs.Count > 0)
            {
                /*
                await Task.Delay(15000);

                var Url = "https://revue-api.amherstinsight.com/api/admin/misc/syncWorkflowTransactionOpportunityByIds";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.Timeout = 10000;
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "PUT";
                httpWebRequest.Headers.Add("AdminApiKey", "p29r5a4defa9");

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(pingFoundIDs);
                    streamWriter.Write(json);
                }

                Console.WriteLine("Sending " + pingFoundIDs.Count + " items to Sync Endpoint... (" + string.Join(", ", pingFoundIDs) + ")");

                using (var response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Console.WriteLine("PUT request successful");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusDescription}");
                    }
                }
                //*/
            }

            Console.WriteLine($"Ping ({foundCount} found) - {DateTime.Now} *****************************");
            var es = new EmailService();
            es.SendEmail(resultStringBuilder.ToString());
        }
        else
        {
            Console.WriteLine($"Ping - {DateTime.Now}");
        }
    }
    else
    {
        Console.WriteLine($"Ping (Skip) - {DateTime.Now}");
    }

    await Task.Delay(120000); // delay in ms
}