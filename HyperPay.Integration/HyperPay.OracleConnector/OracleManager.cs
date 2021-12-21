using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace HyperPay.OracleConnector
{
    public class OracleManager : IDisposable
    {
        private string ProcedureStatusCode = "P_RTRN_CD";
        private string ProcedureStatusDescription = "P_RTRN_DESC";
        private ConnectToOracle _databaseConnection;

        public OracleManager(string connectionString)
        {
            _databaseConnection = new ConnectToOracle(connectionString);
        }

        public DataTable ExecuteCursorStoredProcedure(string name, List<ServiceParameter> parameters, bool useDefaultReturnParamters)
        {
            try
            {


                if (name.Trim().Length == 0) throw new NullReferenceException("The stored procedure name is null or empty");
                if (parameters == null || parameters.Count == 0) throw new NullReferenceException("Parameters are null or empty you must provide at least the output parameters");

                if (parameters.Where(T => T.Name == ProcedureStatusCode).Count() == 0 && useDefaultReturnParamters)
                {
                    ServiceParameter ParamStatusCode = new ServiceParameter()
                    {
                        Name = ProcedureStatusCode,
                        DataType = OracleDbType.NVarchar2,
                        Direction = ParameterDirection.Output,
                        Size = 200

                    };
                    parameters.Add(ParamStatusCode);
                }

                if (parameters.Where(T => T.Name == ProcedureStatusDescription).Count() == 0 && useDefaultReturnParamters)
                {
                    ServiceParameter ParamStatusDescription = new ServiceParameter()
                    {
                        Name = ProcedureStatusDescription,
                        DataType = OracleDbType.NVarchar2,
                        Direction = ParameterDirection.Output,
                        Size = 200
                    };
                    parameters.Add(ParamStatusDescription);
                }



                string CursorName = "";

                try
                {
                    CursorName = parameters.Where(T => T.DataType == OracleDbType.RefCursor).First().Name;
                }
                catch (Exception ex)
                {
                    throw new NullReferenceException("The paramters doesn't contain any Cursor data.");
                }


                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = CommandType.StoredProcedure,

                };
                foreach (var param in parameters)
                {
                    if (param.Direction == ParameterDirection.Input)
                    {
                        var AddedParam = oracleCommand.Parameters.Add(param.Name, param.DataType, param.Value, param.Direction);
                        if (param.Size > 0)
                            AddedParam.Size = param.Size;
                    }
                    else
                    {
                        var AddedParam = oracleCommand.Parameters.Add(param.Name, param.DataType, param.Direction);
                        if (param.Size > 0)
                            AddedParam.Size = param.Size;

                    }
                }

                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                if (useDefaultReturnParamters && oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                    throw new NullReferenceException("Error | Cursor returned with Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                var reader = ((OracleRefCursor)oracleCommand.Parameters[CursorName].Value).GetDataReader();
                var dataTbale = new DataTable();
                dataTbale.Load(reader);

                reader.Close();
                reader.Dispose();
                if (_databaseConnection.IsConnected)
                {
                    _databaseConnection.Disconnect();
                }

                return dataTbale.Rows.Count <= 0 ? null : dataTbale;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public ResultSet ExecuteStoredProcedure(string name, List<ServiceParameter> parameters, bool useDefaultReturnParamters)
        {
            ResultSet ReturnValue = new ResultSet();
            try
            {


                if (name.Trim().Length == 0) throw new NullReferenceException("The stored procedure name is null or empty");
                if (parameters == null || parameters.Count == 0) throw new NullReferenceException("Parameters are null or empty you must provide at least the output parameters");

                if (parameters.Where(T => T.Name == ProcedureStatusCode).Count() == 0 && useDefaultReturnParamters)
                {
                    ServiceParameter ParamStatusCode = new ServiceParameter()
                    {
                        Name = ProcedureStatusCode,
                        DataType = OracleDbType.NVarchar2,
                        Direction = ParameterDirection.Output,
                        Size = 200

                    };
                    parameters.Add(ParamStatusCode);
                }

                if (parameters.Where(T => T.Name == ProcedureStatusDescription).Count() == 0 && useDefaultReturnParamters)
                {
                    ServiceParameter ParamStatusDescription = new ServiceParameter()
                    {
                        Name = ProcedureStatusDescription,
                        DataType = OracleDbType.NVarchar2,
                        Direction = ParameterDirection.Output,
                        Size = 200
                    };
                    parameters.Add(ParamStatusDescription);
                }


                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = CommandType.StoredProcedure,

                };
                ResultSet ParamsList = new ResultSet();
                foreach (var param in parameters)
                {
                    if (param.Direction == ParameterDirection.Input)
                    {
                        var AddedParam = oracleCommand.Parameters.Add(param.Name, param.DataType, param.Value, param.Direction);
                        if (param.Size > 0)
                            AddedParam.Size = param.Size;

                    }
                    else
                    {
                        var AddedParam = oracleCommand.Parameters.Add(param.Name, param.DataType, param.Direction);
                        if (param.Size > 0)
                            AddedParam.Size = param.Size;

                        ParamsList.Add(new ResultItem()
                        {
                            Name = AddedParam.ParameterName
                        });


                    }
                }

                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                if (useDefaultReturnParamters && oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                    throw new NullReferenceException("Error | Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                foreach (var item in ParamsList)
                {
                    var param = parameters.Where(P => P.Name == item.Name).SingleOrDefault();
                    if (param != null)
                    {
                        if (param.DataType == OracleDbType.RefCursor)
                        {
                            var reader = ((OracleRefCursor)oracleCommand.Parameters[param.Name].Value).GetDataReader();

                            var dataTable = new DataTable();
                            dataTable.Load(reader);

                            ReturnValue.Add(new ResultItem()
                            {
                                Name = item.Name,
                                Value = dataTable,
                            });
                        }
                        else
                        {
                            var value = oracleCommand.Parameters[item.Name].Value;

                            ReturnValue.Add(new ResultItem()
                            {
                                Name = item.Name,
                                Value = value,
                            });
                        }
                    }

                }

                if (_databaseConnection.IsConnected)
                {
                    _databaseConnection.Disconnect();
                }

                return ReturnValue;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public DataTable ExcuteQuery(string Query)
        {
            try
            {

                if (!_databaseConnection.IsConnected)
                    _databaseConnection.Connect();

                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = Query,

                    CommandType = CommandType.Text,

                };



                var reader = oracleCommand.ExecuteReader();

                var dataTbale = new DataTable();
                dataTbale.Load(reader);

                reader.Close();
                reader.Dispose();
                if (_databaseConnection.IsConnected)
                    _databaseConnection.Disconnect();

                return dataTbale.Rows.Count <= 0 ? null : dataTbale;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public bool ExecuteStoredProcedure(string name, List<ServiceParameter> parameters)
        {
            try
            {
                if (name.Trim().Length == 0) throw new NullReferenceException("The stored procedure name is null or empty");
                if (parameters == null || parameters.Count == 0) throw new NullReferenceException("Parameters are null or empty you must provide at least the output parameters");

                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = CommandType.StoredProcedure,

                };
                foreach (var param in parameters)
                {
                    if (param.Direction == ParameterDirection.Input)
                    {
                        var AddedParam = oracleCommand.Parameters.Add(param.Name, param.DataType, param.Value, param.Direction);
                        if (param.Size > 0)
                            AddedParam.Size = param.Size;
                    }
                    else
                    {
                        var AddedParam = oracleCommand.Parameters.Add(param.Name, param.DataType, param.Direction);
                        if (param.Size > 0)
                            AddedParam.Size = param.Size;

                    }
                }

                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;
                if (_databaseConnection.IsConnected)
                    _databaseConnection.Disconnect();
                return true;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }


        public void Dispose()
        {
            if (_databaseConnection.IsConnected)
                _databaseConnection.Disconnect();

        }

    }
}
