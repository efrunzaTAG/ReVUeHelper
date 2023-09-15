using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReVUeHelper_MLS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ImportId;

            do
            {
                Console.Write("Enter MLS ImportId: ");
                ImportId = Console.ReadLine();
                if (IsValidGuid(ImportId))
                {
                    Console.WriteLine("Choose a destination:");
                    Console.WriteLine("1. Localhost");
                    Console.WriteLine("2. Dev");
                    Console.WriteLine("3. Test");
                    Console.WriteLine("4. UAT");
                    Console.WriteLine("5. Prod");
                    Console.Write("Enter the number of your choice: ");
                    string selectedDestination = Console.ReadLine();
                    string userChoice = "";
                    switch (selectedDestination)
                    {
                        case "1":
                            userChoice = "http://localhost:5000/api/ThirdParty/Import/mls";
                            Console.WriteLine("Localhost selected");
                            break;
                        case "2":
                            userChoice = "https://revue-api-dev.saas.amherst.com/api/ThirdParty/Import/mls";
                            Console.WriteLine("Dev selected");
                            break;
                        case "3":
                            userChoice = "https://revue-api-test.saas.amherst.com/api/ThirdParty/Import/mls";
                            Console.WriteLine("Test selected");
                            break;
                        case "4":
                            userChoice = "https://revue-api.uat.amherstinsight.com/api/ThirdParty/Import/mls";
                            Console.WriteLine("UAT selected");
                            break;
                        case "5":
                            userChoice = "https://revue-api.amherstinsight.com/api/ThirdParty/Import/mls";
                            Console.WriteLine("Prod selected");
                            break;
                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }

                    if (!string.IsNullOrEmpty(userChoice))
                    {
                        try
                        {
                            MlsImports.RunMlsImport(ImportId, userChoice);
                            Console.WriteLine("Press any key to exit...");
                            Console.ReadKey();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occurred: {0}", ex.Message);
                        }

                        Console.WriteLine("Press any key to exit.");
                        Console.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                    }


                    break; // for while loop
                }
                else
                {
                    Console.WriteLine("Invalid ImportId. Try again.");
                }
            }
            while (true);
            Console.ReadKey();        
        }

        static bool IsValidGuid(string input)
        {
            // Check if the input has exactly 36 characters
            if (input.Length != 36)
            {
                return false;
            }

            // Define a regular expression pattern for GUID validation
            string guidPattern = @"^[A-Fa-f0-9]{8}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{12}$";

            // Use Regex.IsMatch to check if the input matches the pattern
            return Regex.IsMatch(input, guidPattern);
        }
    }
}
