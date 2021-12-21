using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;

namespace HyperPay.Service.Helper
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

        /// <summary>
        /// This Method is used to execute an oracle stored procedure that return cursor from the database and return datatable to the dll consumer
        /// </summary>
        /// <param name="name">Name of the stored procedure</param>
        /// <param name="parameters">List of stored procedure parameters; containing the input and output parameters</param>
        /// <returns>Datatable contains the data or null otherwise</returns>
        public System.Data.DataTable ExecuteCursorStoredProcedure(string name, List<ServiceParameter> parameters, bool useDefaultReturnParamters)
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
                        Direction = System.Data.ParameterDirection.Output,
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
                        Direction = System.Data.ParameterDirection.Output,
                        Size = 200
                    };
                    parameters.Add(ParamStatusDescription);
                }



                string CursorName = "";

                try
                {
                    CursorName = parameters.Where(T => T.DataType == OracleDbType.RefCursor).First().Name;
                }
                catch (Exception EE)
                {
                    throw new NullReferenceException("The paramters doesn't contain any Cursor data.");
                }


                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };
                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                if (useDefaultReturnParamters && oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                    throw new NullReferenceException("Error | Cursor returned with Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                var reader = ((OracleRefCursor)oracleCommand.Parameters[CursorName].Value).GetDataReader();
                //OracleDataReader reader = oracleCommand.Parameters[CursorName].Value;

                var dataTbale = new System.Data.DataTable();
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
        #region modified method by Tamer 
        public List<System.Data.DataTable> ExecuteCursorStoredProcedure(string name, List<ServiceParameter> parameters, out NameValueCollection RetValue, bool useDefaultReturnParamters)
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
                        Direction = System.Data.ParameterDirection.Output,
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
                        Direction = System.Data.ParameterDirection.Output,
                        Size = 200
                    };
                    parameters.Add(ParamStatusDescription);
                }

                List<string> CursorsNames = new List<string>();

                try
                {
                    foreach (var parameter in parameters)
                    {
                        if (parameter.DataType == OracleDbType.RefCursor)
                        {
                            CursorsNames.Add(parameter.Name);
                        }
                        //CursorName = parameters.Where(T => T.DataType == OracleDbType.RefCursor).First().Name;
                    }

                }
                catch (Exception EE)
                {
                    throw new NullReferenceException("The paramters doesn't contain any Cursor data.");
                }


                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };

                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                if (useDefaultReturnParamters && oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                    throw new NullReferenceException("Error | Cursor returned with Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                List<DataTable> dataTables = new List<DataTable>();

                foreach (var cursorName in CursorsNames)
                {
                    var reader = ((OracleRefCursor)oracleCommand.Parameters[cursorName].Value).GetDataReader();

                    var dataTable = new System.Data.DataTable();
                    dataTable.Load(reader);

                    dataTables.Add(dataTable);

                    reader.Close();
                    reader.Dispose();
                }


                var OutParams = parameters.Where(P => P.Direction == System.Data.ParameterDirection.Output && P.DataType != OracleDbType.RefCursor).ToList();
                RetValue = new NameValueCollection(OutParams.Count);


                foreach (var param in OutParams)
                {
                    var RetParam = oracleCommand.Parameters[param.Name];
                    if (RetParam.Value != null && RetParam.Value.ToString() != "null")
                        RetValue.Add(RetParam.ParameterName, RetParam.Value.ToString());
                    else
                        RetValue.Add(RetParam.ParameterName, "");
                }

                if (_databaseConnection.IsConnected)
                {
                    _databaseConnection.Disconnect();
                }

                return dataTables.Count <= 0 ? null : dataTables;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public List<System.Data.DataTable> ExecuteCursorStoredProcedure2(string name, List<ServiceParameter> parameters, out NameValueCollection RetValue, bool useDefaultReturnParamters)
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
                        Direction = System.Data.ParameterDirection.Output,
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
                        Direction = System.Data.ParameterDirection.Output,
                        Size = 200
                    };
                    parameters.Add(ParamStatusDescription);
                }

                List<string> CursorsNames = new List<string>();

                try
                {
                    foreach (var parameter in parameters)
                    {
                        if (parameter.DataType == OracleDbType.RefCursor)
                        {
                            CursorsNames.Add(parameter.Name);
                        }
                        //CursorName = parameters.Where(T => T.DataType == OracleDbType.RefCursor).First().Name;
                    }

                }
                catch (Exception EE)
                {
                    throw new NullReferenceException("The paramters doesn't contain any Cursor data.");
                }


                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };

                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                if (useDefaultReturnParamters && oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                    throw new NullReferenceException("Error | Cursor returned with Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                List<DataTable> dataTables = new List<DataTable>();

                foreach (var cursorName in CursorsNames)
                {
                    var reader = ((OracleRefCursor)oracleCommand.Parameters[cursorName].Value).GetDataReader();

                    var dataTable = new System.Data.DataTable();
                    dataTable.Load(reader);

                    dataTables.Add(dataTable);

                    reader.Close();
                    reader.Dispose();
                }


                var OutParams = parameters.Where(P => P.DataType == OracleDbType.RefCursor).ToList();
                RetValue = new NameValueCollection(OutParams.Count);


                foreach (var param in OutParams)
                {
                    var RetParam = oracleCommand.Parameters[param.Name];

                    RetValue.Add(RetParam.ParameterName, RetParam.Value.ToString());

                }

                if (_databaseConnection.IsConnected)
                {
                    _databaseConnection.Disconnect();
                }

                return dataTables.Count <= 0 ? null : dataTables;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }


        #endregion 

        public System.Data.DataTable ExecuteCursorStoredProcedureAndOutValues(string name, List<ServiceParameter> parameters, out NameValueCollection RetValues, bool useDefaultReturnParamters)
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
                        Direction = System.Data.ParameterDirection.Output,
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
                        Direction = System.Data.ParameterDirection.Output,
                        Size = 200
                    };
                    parameters.Add(ParamStatusDescription);
                }

                string CursorName = "";

                try
                {
                    CursorName = parameters.Where(T => T.DataType == OracleDbType.RefCursor).First().Name;
                }
                catch (Exception EE)
                {
                    throw new NullReferenceException("The paramters doesn't contain any Cursor data.");
                }


                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };

                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                if (useDefaultReturnParamters && oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                    throw new NullReferenceException("Error | Cursor returned with Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                var reader = ((OracleRefCursor)oracleCommand.Parameters[CursorName].Value).GetDataReader();
                //OracleDataReader reader = oracleCommand.Parameters[CursorName].Value;

                var dataTbale = new System.Data.DataTable();
                dataTbale.Load(reader);

                var OutParams = parameters.Where(P => P.Direction == System.Data.ParameterDirection.Output && P.DataType != OracleDbType.RefCursor).ToList();
                RetValues = new NameValueCollection(OutParams.Count);


                foreach (var param in OutParams)
                {
                    var RetParam = oracleCommand.Parameters[param.Name];
                    if (RetParam.Value != null && RetParam.Value.ToString() != "null")
                        RetValues.Add(RetParam.ParameterName, RetParam.Value.ToString());
                    else
                        RetValues.Add(RetParam.ParameterName, "");
                }

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


        public System.Data.DataTable ExecuteCursorStoredProcedureAndOutValues2(string name, List<ServiceParameter> parameters, out NameValueCollection RetValues, bool useDefaultReturnParamters)
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
                        Direction = System.Data.ParameterDirection.Output,
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
                        Direction = System.Data.ParameterDirection.Output,
                        Size = 200
                    };
                    parameters.Add(ParamStatusDescription);
                }

                string CursorName = "";

                try
                {
                    CursorName = parameters.Where(T => T.DataType == OracleDbType.RefCursor).First().Name;
                }
                catch (Exception EE)
                {
                    throw new NullReferenceException("The paramters doesn't contain any Cursor data.");
                }


                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };

                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                if (useDefaultReturnParamters && oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                    throw new NullReferenceException("Error | Cursor returned with Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                var reader = ((OracleRefCursor)oracleCommand.Parameters[CursorName].Value).GetDataReader();
                //OracleDataReader reader = oracleCommand.Parameters[CursorName].Value;

                var dataTbale = new System.Data.DataTable();
                dataTbale.Load(reader);

                var OutParams = parameters.Where(P => P.Direction == System.Data.ParameterDirection.Output).ToList();
                RetValues = new NameValueCollection(OutParams.Count);


                foreach (var param in OutParams)
                {
                    var RetParam = oracleCommand.Parameters[param.Name];
                    if (RetParam.Value != null && RetParam.Value.ToString() != "null")
                        RetValues.Add(RetParam.ParameterName, RetParam.Value.ToString());
                    else
                        RetValues.Add(RetParam.ParameterName, "");
                }

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

        public System.Data.DataTable ExcuteQuery(string Query)
        {
            try
            {

                if (!_databaseConnection.IsConnected)
                    _databaseConnection.Connect();

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = Query,

                    CommandType = System.Data.CommandType.Text,

                };



                var reader = oracleCommand.ExecuteReader();
                //OracleDataReader reader = oracleCommand.Parameters[CursorName].Value;

                var dataTbale = new System.Data.DataTable();
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

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };
                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteNonQuery();

                var parametersNumber = parameters.Count;


                //if (oracleCommand.Parameters[ProcedureStatusCode].Value.ToString() != "000000")
                //    throw new NullReferenceException("Error | Cursor returned with Description: " + oracleCommand.Parameters[ProcedureStatusDescription].Value.ToString());

                //var reader = ((OracleRefCursor)oracleCommand.Parameters[CursorName].Value).GetDataReader();
                //OracleDataReader reader = oracleCommand.Parameters[CursorName].Value;

                //var dataTbale = new System.Data.DataTable();
                //dataTbale.Load(reader);

                //reader.Close();
                //reader.Dispose();

                //return dataTbale.Rows.Count <= 0 ? null : dataTbale;

                if (_databaseConnection.IsConnected)
                    _databaseConnection.Disconnect();
                return true;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public bool ExecuteStoredProcedureAsync(string name, List<ServiceParameter> parameters)
        {
            try
            {
                if (name.Trim().Length == 0) throw new NullReferenceException("The stored procedure name is null or empty");
                if (parameters == null || parameters.Count == 0) throw new NullReferenceException("Parameters are null or empty you must provide at least the output parameters");

                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };
                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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

                //Task task = Task.Factory.StartNew(() =>
                //{
                //    return oracleCommand.ExecuteNonQueryAsync();
                //});

                oracleCommand.ExecuteNonQueryAsync();

                if (_databaseConnection.IsConnected)
                    _databaseConnection.Disconnect();
                return true;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }


        public NameValueCollection ExecuteStoredProcedureAndReturnValues(string name, List<ServiceParameter> parameters)
        {
            try
            {

                if (name.Trim().Length == 0) throw new NullReferenceException("The stored procedure name is null or empty");
                if (parameters == null || parameters.Count == 0) throw new NullReferenceException("Parameters are null or empty you must provide at least the output parameters");

                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,

                    CommandType = System.Data.CommandType.StoredProcedure,

                };
                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteReader();

                var parametersNumber = parameters.Count;

                var OutParams = parameters.Where(P => P.Direction == System.Data.ParameterDirection.Output && P.DataType != OracleDbType.RefCursor).ToList();
                NameValueCollection RetValue = new NameValueCollection(OutParams.Count);


                foreach (var param in OutParams)
                {
                    var RetParam = oracleCommand.Parameters[param.Name];
                    if (RetParam.Value != null && RetParam.Value.ToString() != "null")
                        RetValue.Add(RetParam.ParameterName, RetParam.Value.ToString());
                    else
                        RetValue.Add(RetParam.ParameterName, "");
                }


                if (_databaseConnection.IsConnected)
                    _databaseConnection.Disconnect();
                return RetValue;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public NameValueCollection ExecuteStoredProcedureAndReturnValues(string name, List<ServiceParameter> parameters, bool useDefaultReturnParameters)
        {
            try
            {

                if (name.Trim().Length == 0) throw new NullReferenceException("The stored procedure name is null or empty");
                if (parameters == null || parameters.Count == 0) throw new NullReferenceException("Parameters are null or empty you must provide at least the output parameters");

                #region DefaultOutParameters
                if (parameters.Where(T => T.Name == ProcedureStatusCode).Count() == 0 && useDefaultReturnParameters)
                {
                    ServiceParameter ParamStatusCode = new ServiceParameter()
                    {
                        Name = ProcedureStatusCode,
                        DataType = OracleDbType.NVarchar2,
                        Direction = System.Data.ParameterDirection.Output,
                        Size = 200

                    };
                    parameters.Add(ParamStatusCode);
                }

                if (parameters.Where(T => T.Name == ProcedureStatusDescription).Count() == 0 && useDefaultReturnParameters)
                {
                    ServiceParameter ParamStatusDescription = new ServiceParameter()
                    {
                        Name = ProcedureStatusDescription,
                        DataType = OracleDbType.NVarchar2,
                        Direction = System.Data.ParameterDirection.Output,
                        Size = 2000
                    };
                    parameters.Add(ParamStatusDescription);
                }
                #endregion

                if (!_databaseConnection.IsConnected)
                {
                    _databaseConnection.Connect();
                }

                //database operations
                var oracleCommand = new OracleCommand
                {
                    Connection = _databaseConnection.Connection,
                    CommandText = name,
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                foreach (var param in parameters)
                {
                    if (param.Direction == System.Data.ParameterDirection.Input)
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
                //oracleCommand.Parameters.AddRange(parameters.ToArray());


                //Execute the Search Query
                oracleCommand.ExecuteReader();

                var OutParams = parameters.Where(P => P.Direction == System.Data.ParameterDirection.Output && P.DataType != OracleDbType.RefCursor).ToList();
                NameValueCollection RetValue = new NameValueCollection(OutParams.Count);


                foreach (var param in OutParams)
                {
                    var RetParam = oracleCommand.Parameters[param.Name];
                    if (RetParam.Value != null && RetParam.Value.ToString() != "null")
                        RetValue.Add(RetParam.ParameterName, RetParam.Value.ToString());
                    else
                        RetValue.Add(RetParam.ParameterName, "");
                }


                if (_databaseConnection.IsConnected)
                    _databaseConnection.Disconnect();
                return RetValue;
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
