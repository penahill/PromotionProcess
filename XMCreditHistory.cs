using Dapper;
using Modere.Services.Data;
using Modere.Services.Utilities;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace XMCredit
{
    public class XMCreditHistory
    {
        public static DataResponse<bool> ProcessXMCreditHistory()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Exigo"];
            Logger.LogInfo("Start History Promo check");
            List<XMCreditInfo> xmCreditInfo = new List<XMCreditInfo>();

            // Call store procedure to get the information to process
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString.ToString()))
                {
                    con.Open();
                    const string sql = "promoXMCreditHistory";
                    DynamicParameters dbParams = new DynamicParameters();

                    xmCreditInfo = con.Query<XMCreditInfo>(
                        sql,
                        dbParams,
                        commandType: CommandType.StoredProcedure
                        ).ToList();

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo("Error in processing History Records For XM Credit Promos. Error: " + ex.Message);
                return ErrorResponse.CreateErrorResponse<bool>(ResponseCode.InternalServerError, ex.Message);
            }

            Logger.LogInfo("Finished XM Credit History process");

            return new DataResponse<bool> { Data = true };
        }
    }
}
