
namespace WpfStatistics.Model
{
    class Parameter
    {
        public string value {get;set;}
        public string displayString{get;set;}
        public string description{get;set;}

        public override string ToString()
        {
            return displayString;
        }
    }

    class ParameterResponse
    {
        public Parameter[] parameterValues{get;set;}
    }
}
