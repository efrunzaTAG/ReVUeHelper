using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ComponentModel.DataAnnotations;

namespace ReVUeHelper_MLS
{
    public class MlsAutoImportItemDto
    {
        #region Required

        public long? ReferenceId { get; set; }  // required and need to be unique
        public string MlsId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public int? YearBuilt { get; set; }
        public int? Sqft { get; set; }
        public int? Beds { get; set; }
        public decimal? Baths { get; set; }
        public string PropertyType { get; set; }
        public int? TotalUnits { get; set; }
        public decimal? AskingPrice { get; set; }
        public DateTime? MlsListingDate { get; set; }

        #endregion


        #region Optional

        public string County { get; set; }
        public bool? HasPool { get; set; }
        public string Notes { get; set; }
        public string Subdivision { get; set; }
        public string Neighborhood { get; set; }
        public int? DaysOnMarket { get; set; }
        public string MlsBoardAbbreviation { get; set; }
        public string OccupancyStatus { get; set; }

        public string ListingBrokerName { get; set; }
        public string ListingAgentName { get; set; }
        public string ListingAgentPhone { get; set; }
        public string ListingAgentEmail { get; set; }

        public string LotNumber { get; set; }
        public string Block { get; set; }
        public string LegalDescription { get; set; }
        public string ParcelId { get; set; }
        public string DeedBook { get; set; }
        public string PageNumber { get; set; }

        public string SubLotNumber { get; set; }
        public string PlatBook { get; set; }
        public string PlatPage { get; set; }
        public decimal? ModelRent { get; set; }
        public decimal? ModelArv { get; set; }
        public decimal? ModelNoi { get; set; }
        public decimal? ModelExpectedSalesPrice { get; set; }
        public decimal? SellerHoa { get; set; }

        #endregion
    }

    public class AutoImportResponseDto
    {
        public AutoImportResponseDto()
        {
            ImportItemValidationResults = new List<ImportItemValidationResult>();
        }

        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<ImportItemValidationResult> ImportItemValidationResults { get; set; }
    }

    public class ImportItemValidationResult
    {
        public ImportItemValidationResult(int referenceId)
        {
            ReferenceId = referenceId;
            ValidationErrors = new List<ImportItemValidationError>();
        }

        public int ReferenceId { get; set; }  // Frontend/third party will use this as their reference from error field to property field
        public bool PassedValidation => !ValidationErrors.Any();
        public List<ImportItemValidationError> ValidationErrors { get; set; }

        public void AddValidationError(string fieldName, string errorMessage)
        {
            ValidationErrors.Add(new ImportItemValidationError(fieldName, errorMessage));
        }

        public void AddValidationErrors(IEnumerable<ImportItemValidationError> errors)
        {
            ValidationErrors.AddRange(errors);
        }
    }

    public class ImportItemValidationError
    {
        public ImportItemValidationError(string fieldName, string errorMessage)
        {
            FieldName = fieldName;
            ErrorMessage = errorMessage;
        }

        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CrmAutoImportDto
    {
        public string Name { get; set; }
        public Channel? Channel { get; set; }
        public OpportunityStatus? RequestedOpportunityStatus { get; set; }
        public IEnumerable<CrmAutoImportItemDto> CrmAutoImportItems { get; set; }
    }

    public enum Channel
    {
        [Display(Name = "Auction")]
        Auction = 0,
        [Display(Name = "MLS")]
        MLS = 1,
        [Display(Name = "Bulk Sale")]
        BulkSale = 2,
        [Display(Name = "Build To Rent")]
        BuildToRent = 3,
        [Display(Name = "Off Market")]
        OffMarket = 4,
        [Display(Name = "Other")]
        Other = 5
    }

    public enum OpportunityStatus
    {
        Imported,
        PreScreen,
        Initial,
        Counter,
        DueDiligence,
        Terminated,
        Completed
    }

    public class CrmAutoImportItemDto
    {
        #region Required
        public long? ReferenceId { get; set; } // required and need to be unique
        public string SourceId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public int? YearBuilt { get; set; }
        public int? Sqft { get; set; }
        public int? Beds { get; set; }
        public decimal? Baths { get; set; }
        public string PropertyType { get; set; }
        public int? TotalUnits { get; set; }
        public DateTime? ContractCloseDate { get; set; }

        #endregion

        #region Optional

        public string County { get; set; }
        public bool? HasPool { get; set; }
        public decimal? AskingPrice { get; set; }
        public string Notes { get; set; }
        public string Subdivision { get; set; }
        public string Neighborhood { get; set; }
        public decimal? TipRent { get; set; }
        public DateTime? LeaseEndDate { get; set; }
        public string OccupancyStatus { get; set; }

        public decimal? SellerArv { get; set; }
        public decimal? SellerRent { get; set; }
        public decimal? SellerTaxes { get; set; }
        public decimal? SellerHoa { get; set; }

        public string ListingBrokerName { get; set; }
        public string ListingAgentName { get; set; }
        public string ListingAgentPhone { get; set; }
        public string ListingAgentEmail { get; set; }

        public string LotNumber { get; set; }
        public string Block { get; set; }
        public string LegalDescription { get; set; }
        public string ParcelId { get; set; }

        #endregion

    }

    public class MlsImports
    {
        public static async void RunMlsImport(string ImportId, string Url)
        {
            var ImportIDs = new List<string>();
            ImportIDs.Add(ImportId);

            foreach (var ImportID in ImportIDs)
            {
                var db = new ReVUeDbProxy();
                var MlsAutoImportItems = db.GetMlsAutoImportItems(ImportID);

                if (MlsAutoImportItems.Count == 0)
                {
                    Console.WriteLine("no data for ImportId=" + ImportId);
                    continue;
                }

                for (int i = MlsAutoImportItems.Count; i-- > 0;)
                {
                    var MlsAutoImportItem = MlsAutoImportItems[i];
                    if (
                        1 == 0
                            //use this to skip some addresses (used for 2nd round of failed validation from PropMaster)

                            //|| (MlsAutoImportItem.Street == "159 DA VINCI" && MlsAutoImportItem.ZipCode == "78258")
                            //|| (MlsAutoImportItem.Street == "12203 COMMANDER DR" && MlsAutoImportItem.ZipCode == "78252")
                            //||  (MlsAutoImportItem.Street == "" && MlsAutoImportItem.ZipCode == "")
                            )
                    {
                        MlsAutoImportItems.RemoveAt(i);
                    }
                }


                var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.Timeout = 600000;
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("ReVUeApiKey", "9c2c0586-31bc-48e4-8ea8-4ae27c8720e9");

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(MlsAutoImportItems);
                    streamWriter.Write(json);
                }

                Console.WriteLine("Sending " + MlsAutoImportItems.Count + " items ...");

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var responseDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<AutoImportResponseDto>(result);

                    if (responseDTO.Success)
                    {
                        Console.WriteLine("succeeded (" + ImportID + ") | count=" + MlsAutoImportItems.Count);

                        if (ImportIDs.Count > 1)
                        {
                            Thread.Sleep(60000 * 2);
                        }                        
                    }
                    else
                    {
                        Console.WriteLine(responseDTO);
                    }
                }
            }
        }
    }
}
