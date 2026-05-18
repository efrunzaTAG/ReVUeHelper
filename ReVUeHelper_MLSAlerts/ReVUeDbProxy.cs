using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using ReVUeHelper_MLSAlerts.DTOs;

namespace ReVUeHelper_MLSAlerts
{
    public class ReVUeDbProxy
    {
        string connectionString = "Data Source=sqlProd.saas.amherst.com;Initial Catalog=ReVue;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUeHelper_MLSAlerts";


        public List<MlsBatchItem> GetRecentMlsImports()
        {
            var sql = @"
                select		ReceivedOn, 
			                datename(weekday, ReceivedOn) as DayOfWeek,
			                ImportId,
			                avgYearBuilt=avg(YearBuilt),
			                avgBeds=avg(Beds),
			                avgBaths=avg(Baths), 
			                avgSqft=avg(Sqft), 
			                count(*) as CountReceived
                from		dbo.MlsAutoImportItems with (nolock)
                where		ReceivedOn >= dateadd(day, -1, getdate())
                group by	ReceivedOn, ImportId
                order by	ReceivedOn desc
            ";

            using (var connection = new SqlConnection(connectionString))
            {
                return connection.Query<MlsBatchItem>(sql, null, null, true, 20).ToList();
            }
        }
    }
}
