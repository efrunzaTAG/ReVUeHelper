using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReVUeHelper_OSS
{
    public class OppStatusItem
    {
        public long Id { get; set; }
        
        public string OppStatus_Opp { get; set; }

        public string OppStatus_Wto { get; set; }

        public bool HadSuccessfulCrmPush { get; set; }

        public bool IsProcessing { get; set; }
    }
}
