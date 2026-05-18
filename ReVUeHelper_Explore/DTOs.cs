using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReVUeHelper_Explore
{
    public class ExploreProperty
    {
        public long asgPropID { get; set; }

        public double? Elevation { get; set; }

        public long? GudPropertyID { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string CBSA { get; set; }
        public string CBSAName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string CensusTractGeoId { get; set; }
        public string CensusTractName { get; set; }
        public string PropertyType { get; set; }
        public string PropertySubtype { get; set; }
        public int? YearBuilt { get; set; }
        public int LivingAreaSqFt { get; set; }
        public int Beds { get; set; }
        public int FullBaths { get; set; }
        public int? MFUnits { get; set; }

        public DateTime? UpdateTimeStamp { get; set; }
        public string Status { get; set; }
        public double? AVMValue { get; set; }
        public double? LastTaxAmount { get; set; }
        public short? SchoolScore { get; set; }
        public double? ForSalePrice { get; set; }
        public double? ForRentPrice { get; set; }
        public double? ForSalePricePerSqFt { get; set; }
        public double? ForRentPricePerSqFt { get; set; }
        public double? ForSalePricePctOfAVM { get; set; }
        public double? UnemploymentRate { get; set; }
        public double? MedianIncome { get; set; }
        public double? CapRate { get; set; }
        public double? HPA6mo { get; set; }
        public double? HPA1Yr { get; set; }
        public double? HPA2Yr { get; set; }
        public double? CrimeIndex { get; set; }
        public short? AmherstDemographicCategory { get; set; }
        public string AmherstDemographicCluster_v2 { get; set; }
        public string OwnerName { get; set; }
        public string CustomPortfolio { get; set; }
        public string AmherstRegion { get; set; }
        public DateTime? StatusDate { get; set; }
        public double? LastSalePrice { get; set; }
        public DateTime? LastSaleDate { get; set; }

        public long? AmherstPropertyID { get; set; }
        public bool? IsAmherstManaged { get; set; }
        public double? ModelRent { get; set; }

        public bool IsProspect { get; set; }


        public string InsertedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime InsertedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public bool? DescriptiveBuyBoxFit { get; set; }
        public double? LastRent { get; set; }
        public DateTime? LastRentDate { get; set; }

        public bool? IsInOpportunityZone { get; set; }
        public bool? IsRecentFlip { get; set; }

        public double? LotSizeAcres { get; set; }
        public double? Stories { get; set; }
        public string OwnerType { get; set; }

        public string CommunityName { get; set; }
        public string FIPS { get; set; }
        public string Gemstone { get; set; }

        public long? TransactionOpportunityId { get; set; }        

        public string FullAddress
        {
            get
            {
                return Address + ", " + City + ", " + State + " " + ZipCode;
            }
        }

    }
}
