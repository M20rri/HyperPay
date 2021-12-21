using Oracle.ManagedDataAccess.Client;

namespace HyperPay.OracleConnector
{
    public class ServiceParameter
    {
        public string Name { get; set; }
        public OracleDbType DataType { get; set; }
        public System.Data.ParameterDirection Direction { get; set; }
        public object Value { get; set; }
        public int Size { get; set; }
    }
}
