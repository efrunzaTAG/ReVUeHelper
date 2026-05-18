using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReVUeHelper_MLSAlerts.DTOs
{
    public class MlsBatchItem
    {
        public DateTime ReceivedOn { get; set; }
        public Guid ImportId { get; set; }
        public int CountReceived { get; set; }
    }
}
