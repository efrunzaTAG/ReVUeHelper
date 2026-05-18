using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ReVUeHelper_OSS
{
    public class ReVUeDbProxy
    {
        string connectionString = "Data Source=sqlProd.saas.amherst.com;Initial Catalog=ReVue;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUeHelper_OSS";


        public List<OppStatusItem> GetOutOfSyncItems()
        {
            var sql = @"
                select		t.OpportunityStatus as OppStatus_Opp,
			                wto.OpportunityStatus as OppStatus_Wto,
			                wto.HadSuccessfulCrmPush,
			                wto.Id,
			                IsProcessing = iif(po.Id > 0, 1, 0)
                from		dbo.WorkflowTransactionOpportunities wto with (nolock)
                left join	dbo.TransactionOpportunities t with (nolock)
                on			t.Id = wto.Id
                left join	dbo.ProcessingOpportunities po with (nolock)
                on			t.Id = po.Id
                where		1=1
                and			t.OpportunityStatus != wto.OpportunityStatus
            ";

            using (var connection = new SqlConnection(connectionString))
            {
                return connection.Query<OppStatusItem>(sql, null, null, true, 10).ToList();
            }
        }
    }
}
