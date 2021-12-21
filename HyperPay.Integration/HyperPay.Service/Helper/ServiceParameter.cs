using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace HyperPay.Service.Helper
{
    public class ServiceParameter
    {
        public string Name { get; set; }
        public OracleDbType DataType { get; set; }
        public ParameterDirection Direction { get; set; }
        public object Value { get; set; }
        public int Size { get; set; }
    }
}
