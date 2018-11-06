using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfStatistics.Model
{
    class StatisticParameter
    {
        public string parameterTypeId{get;set;}
        public string value{get;set;}
    }

    class StatisticIdentifierRequest
    {
        public string statisticIdentifier {get;set;}
        public StatisticParameter[] parameterValueItems {get;set;}
    }

    class StatisticKeysRequest
    {
        public List<StatisticIdentifierRequest> statisticKeys { get; set; }
    }
}
