using System.Collections.Generic;
using System.Linq;

namespace HyperPay.OracleConnector
{
    public class ResultItem
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class ResultSet : List<ResultItem>
    {
        public ResultItem this[string name]
        {
            get
            {
                return this.FirstOrDefault(tTemp => tTemp.Name == name);
            }
        }
    }
}
