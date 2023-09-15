using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using Dapper;
using System.Linq;

namespace ReVUeHelper_MLS
{
    public class ReVUeDbProxy
    {
        string connectionString = "Data Source=sqlProd.saas.amherst.com;Initial Catalog=ReVue;Integrated Security=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Application Name=ReVUe.Helper";


        public List<MlsAutoImportItemDto> GetMlsAutoImportItems(string ImportId)
        {
            var sql = $"select * from dbo.MlsAutoImportItems where ImportId = '{ImportId}' order by Id";
            //var sql = $"select * from dbo.MlsAutoImportItems where ImportId in ('EE1E82AD-A441-484A-A3DA-4E0D472EE4DA','35EDAB00-7D26-4D3A-B285-B4304DF7F4CE','449B60B0-CD29-48A6-ADAA-FE92C9DAF7F0','8F7C6668-93D1-4F71-8EA1-F050FEF34793','4F182A82-E234-428B-B0ED-DB36AC1DEF87','7849E71D-E069-435D-A2CA-109C901FD8FA','43ECE9A4-7B78-41DB-9EC0-C1058838C19B','A18F05E7-855B-40C0-8035-454E25ADC98C') order by Id";

            using (var connection = new SqlConnection(connectionString))
            {
                return connection.Query<MlsAutoImportItemDto>(sql, null, null, true, 600).ToList();
            }
        }
    }
}
