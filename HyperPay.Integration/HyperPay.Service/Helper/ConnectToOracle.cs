using System;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace HyperPay.Service.Helper
{
    public class ConnectToOracle
    {
        private readonly string _connectionString;

        public ConnectToOracle(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) _connectionString = string.Empty;
            _connectionString = connectionString;
        }

        public bool IsConnected { get; private set; }
        public virtual string ConnectionString
        {
            get { return _connectionString; }
        }
        public OracleConnection Connection { get; private set; }

        public void Connect()
        {
            if (string.IsNullOrEmpty(_connectionString)) throw new NullReferenceException("Connection String is empty please check it");

            try
            {
                Connection = new OracleConnection(_connectionString);
                Connection.Open();
                if (Connection.State != ConnectionState.Open) throw new Exception("Couldn't connect to Oracle ");
                IsConnected = true;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            if (Connection == null)
            {
                IsConnected = false;
                return;
            }
            try
            {
                Connection.Close();
                Connection.Dispose();
                IsConnected = false;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
    }
}
