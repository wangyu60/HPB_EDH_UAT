using System.Collections.Generic;

namespace HPB_EDH_UAT
{
    public class TestCase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TestMethod TestMethod { get; set; }

        public string UEN { get; set; }

        public Dictionary<string, string> QueryParams { get; } = new Dictionary<string, string>();

        public void AddQueryParam(string ParamName, string ParamVal) //can also overload Dictionary.Add
        {
            if (!QueryParams.ContainsKey(ParamName))
            {
                QueryParams.Add(ParamName, ParamVal);
            }
            else
            {
                QueryParams[ParamName] += $",{ParamVal}";
            }
        }
    }    
}
