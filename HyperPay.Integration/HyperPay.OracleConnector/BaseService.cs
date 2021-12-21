using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Collections.Generic;

namespace HyperPay.OracleConnector
{
    public class BaseService
    {
        public List<ServiceParameter> SetupParamters(string p_channel, long p_usr_id, string p_lang_id, long p_sdp_id, string p_saptco_pnr, long p_payment_method_id, long p_total_paid_amount, string p_transaction_id, string p_payment_process_date, int p_is_paying, string p_expiry_date)
        {
            List<ServiceParameter> parameters = new List<ServiceParameter>() {

                // InPut
                new ServiceParameter {  Name = "p_channel", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Input, Value = p_channel,Size = 200 },
                new ServiceParameter {  Name = "p_usr_id", DataType = OracleDbType.Int64, Direction = ParameterDirection.Input, Value = p_usr_id,Size = 15 },
                new ServiceParameter {  Name = "p_lang_id", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Input, Value = p_lang_id,Size = 200 },
                new ServiceParameter {  Name = "p_sdp_id", DataType = OracleDbType.Int64, Direction = ParameterDirection.Input, Value = p_sdp_id,Size = 15 },
                new ServiceParameter {  Name = "p_saptco_pnr", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Input, Value = p_saptco_pnr,Size = 200 },
                new ServiceParameter {  Name = "p_payment_method_id", DataType = OracleDbType.Int64, Direction = ParameterDirection.Input, Value = p_payment_method_id,Size = 15 },
                new ServiceParameter {  Name = "p_total_paid_amount", DataType = OracleDbType.Int64, Direction = ParameterDirection.Input, Value = p_total_paid_amount,Size = 15 },
                new ServiceParameter {  Name = "p_transaction_id", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Input, Value = p_transaction_id,Size = 200 },
                new ServiceParameter {  Name = "p_payment_process_date", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Input, Value = p_payment_process_date,Size = 200 },
                new ServiceParameter {  Name = "p_is_paying", DataType = OracleDbType.Int32, Direction = ParameterDirection.Input, Value = p_is_paying,Size = 15 },
                new ServiceParameter {  Name = "p_expiry_date", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Input, Value = p_expiry_date,Size = 200 },

                 // OutPut
               new ServiceParameter {  Name = "p_is_paid", DataType = OracleDbType.Int64, Direction = ParameterDirection.Output },
               new ServiceParameter {  Name = "p_excp_ticket_numbers", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Output },
               new ServiceParameter {  Name = "p_rtrn_cd", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Output },
               new ServiceParameter {  Name = "p_rtrn_desc", DataType = OracleDbType.NVarchar2, Direction = ParameterDirection.Output },

            };

            return parameters;
        }
    }
}
