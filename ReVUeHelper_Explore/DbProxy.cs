using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ReVUeHelper_Explore
{
    public class ConstellationRow
    {
        public long AssetId { get; set; }
        public long AsgPropId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? CBSAName { get; set; }
    }

    public class DbProxy
    {
        const string PROD_connectionString = "Data Source=sqlProd.saas.amherst.com;Initial Catalog=ReVue;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUeHelper_Explore";
        const string DEV_connectionString  = "Data Source=sqlDev;Initial Catalog=DBATestBed;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUeHelper_Explore";
        const string BI_connectionString   = "Data Source=sqlBI.saas.amherst.com;Initial Catalog=MerchantBankBI;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUeHelper_Explore";

        public List<ConstellationRow> GetConstellationData(IEnumerable<long> assetIds)
        {
            const string sql = @"
                SELECT  asset_id     AS AssetId,
                        asg_prop_id  AS AsgPropId,
                        latitude     AS Latitude,
                        longitude    AS Longitude,
                        cbsa_name    AS CBSAName
                FROM    dbo.constellation WITH (NOLOCK)
                WHERE   asset_id IN @ids";

            var results = new List<ConstellationRow>();
            using var conn = new SqlConnection(BI_connectionString);
            conn.Open();

            foreach (var batch in assetIds.Distinct().Chunk(1000))
            {
                var rows = conn.Query<ConstellationRow>(sql, new { ids = batch });
                results.AddRange(rows);
            }
            return results;
        }

        public Dictionary<long, double> GetCachedElevations()
        {
            const string sql = @"
                SELECT asgPropID, Elevation FROM DBATestBed.dbo.EF_Explore_Elevations          WITH (NOLOCK) WHERE Elevation IS NOT NULL
                UNION
                SELECT asgPropID, Elevation FROM DBATestBed.dbo.EF_Explore_Elevations_20250516 WITH (NOLOCK) WHERE Elevation IS NOT NULL
                UNION
                SELECT asgPropID, Elevation FROM DBATestBed.dbo.EF_Explore_Elevations_20260518 WITH (NOLOCK) WHERE Elevation IS NOT NULL";

            using var conn = new SqlConnection(DEV_connectionString);
            var rows = conn.Query<(long asgPropID, double Elevation)>(sql);

            var dict = new Dictionary<long, double>();
            foreach (var r in rows)
                if (!dict.ContainsKey(r.asgPropID))
                    dict[r.asgPropID] = r.Elevation;
            return dict;
        }

        public void InsertElevation(long asgPropID, double? latitude, double? longitude, double elevation, string? cbsaName)
        {
            const string sql = @"
                INSERT INTO EF_Explore_Elevations_20260518 (asgPropID, Latitude, Longitude, Elevation, CBSAName)
                VALUES (@asgPropID, @Latitude, @Longitude, @Elevation, @CBSAName)";

            using IDbConnection conn = new SqlConnection(DEV_connectionString);
            conn.Execute(sql, new
            {
                asgPropID,
                Latitude  = latitude,
                Longitude = longitude,
                Elevation = elevation,
                CBSAName  = cbsaName
            });
        }
    }
}
