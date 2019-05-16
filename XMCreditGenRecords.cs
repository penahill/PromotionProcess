using Dapper;
using Modere.Services.Data;
using Modere.Services.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMCredit
{
    public class XMCreditGenRecords
    {
        public static string ProcessXMCredit()
        {
            string message = null;
            var connectionString = ConfigurationManager.ConnectionStrings["Exigo"];
            Logger.LogInfo("Start Generating Promo Gen Records");

            var result = GetXMInfo(connectionString);
            var XMCreditInfo = result.Data;

            if (result.Error != null)
            {
                return result.Error.Message;
            }

            if (XMCreditInfo.Count == 0)
            {
                message = "No records found to process for XM Credit promotion.";
                return message;
            }

            // Process Records
            var result2 = ProcessXMInfo(XMCreditInfo, connectionString);

            if (result2.Error != null)
            {
                return result2.Error.Message;
            }

            return "";
        }


        public static DataResponse<List<XMCreditInfo>> GetXMInfo(ConnectionStringSettings connectionString)
        {
            List<XMCreditInfo> xmCreditInfo = new List<XMCreditInfo>();

            // Call store procedure to get the information to process
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString.ToString()))
                {
                    con.Open();
                    const string sql = "promoXMCredit";
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
                Logger.LogInfo("Error in processing Gen Records For XM Credit Promos. Error: " + ex.Message);
                return ErrorResponse.CreateErrorResponse<List<XMCreditInfo>>(ResponseCode.InternalServerError, ex.Message);
            }

            Logger.LogInfo("Finished Getting XM Credit Info");

            return new DataResponse<List<XMCreditInfo>> { Data = xmCreditInfo };
        }

        public static DataResponse<List<XMCreditInfo>> ProcessXMInfo(List<XMCreditInfo> xmCreditInfo, ConnectionStringSettings connectionString)
        {

            GenRecordInfo genRecordInfo = new GenRecordInfo();
            XMCreditInfo holdXMCredit = new XMCreditInfo();
            bool result = true;
            string message = null;
            int holdCustomerId = 0;
            int holdWarehouseID = 0;
            int promoCounter = 0;


            // Get Promo information
            var result2 = getPromoInfo(connectionString);
            var XMPromoInfo = result2.Data;

            if (result2.Error != null)
            {
                return ErrorResponse.CreateErrorResponse<List<XMCreditInfo>>(ResponseCode.InternalServerError, result2.Error.Message);
            }

            if (XMPromoInfo.Count == 0)
            {
                message = "No promo records found for XM Credit promotion.";
                return ErrorResponse.CreateErrorResponse<List<XMCreditInfo>>(ResponseCode.InternalServerError, message);
            }

            // Process potential records
            for (int i = 0; i < xmCreditInfo.Count; i++)
            {
                if (promoCounter > 4)
                {
                    message = "Can only issue 4 promotions. Skipping";
                    Logger.LogInfo(message);
                    promoCounter = 0;
                    continue;
                }

                if (xmCreditInfo[i].CollectionRequirement == false)
                {
                    message = "Customer did not meet collection requirement. Skipping";
                    Logger.LogInfo(message);
                    promoCounter = 0;
                    continue;
                }

                if (holdCustomerId != xmCreditInfo[i].CustomerID)
                {
                    holdCustomerId = xmCreditInfo[i].CustomerID;
                    holdWarehouseID = xmCreditInfo[i].DefaultWarehouseID;
                    promoCounter = 0;
                }

                if (xmCreditInfo[i].LogID > 0)
                {
                    message = "SM " + xmCreditInfo[i].CustomerID + " order " + xmCreditInfo[i].OrderID + " has been processed. Skipping";
                    Logger.LogInfo(message);
                    promoCounter += 1;
                    continue;
                }

                if (xmCreditInfo[i].FirstRecurringOrderRequirement == false)
                {
                    message = "SM " + xmCreditInfo[i].CustomerID + " did not meet first recurring order requirement. Skipping";
                    Logger.LogInfo(message);
                    promoCounter += 1;
                    continue;
                }
                else if (promoCounter > 0 && xmCreditInfo[i].RecurringOrderRequirement == false)
                {
                    message = "SM " + xmCreditInfo[i].CustomerID + " did not meet recurring order requirement. Skipping";
                    Logger.LogInfo(message);
                    promoCounter += 1;
                    continue;
                }

                // Check to make sure the last recurring order was within 31 days
                if (promoCounter > 0)
                {
                    if (xmCreditInfo[i].LastRecurringOrderDate != DateTime.MinValue)
                    {
                        if ((xmCreditInfo[i].OrderDate - xmCreditInfo[i].LastRecurringOrderDate).TotalDays > 31)
                        {
                            message = "SM " + xmCreditInfo[i].CustomerID + " last recurring order is outside the allowed window. Skipping";
                            Logger.LogInfo(message);
                            promoCounter += 1;
                            continue;
                        }
                    }
                }
            
                promoCounter += 1;
                holdXMCredit = xmCreditInfo[i];
                holdXMCredit.WarehouseID = xmCreditInfo[i].DefaultWarehouseID;
                holdXMCredit.IssuePromoCode = Convert.ToString(XMPromoInfo.Where(promo => promo.WarehouseId == xmCreditInfo[i].DefaultWarehouseID && promo.PromoNumber == promoCounter)
                                                                                             .Select(promo => promo.PromoCode).FirstOrDefault());
                holdXMCredit.IssuePromoID = Convert.ToInt32(XMPromoInfo.Where(promo => promo.WarehouseId == xmCreditInfo[i].DefaultWarehouseID && promo.PromoCode == holdXMCredit.IssuePromoCode)
                                                                     .Select(promoid => promoid.PromoId).FirstOrDefault());
                holdXMCredit.RollingDays = Convert.ToInt32(XMPromoInfo.Where(promoday => promoday.PromoId == holdXMCredit.IssuePromoID)
                                                                     .Select(promoday => promoday.RollingDays).FirstOrDefault());
                holdXMCredit.PromoEndDate = Convert.ToDateTime(XMPromoInfo.Where(promodate => promodate.PromoId == holdXMCredit.IssuePromoID)
                                                                     .Select(promodate => promodate.PromoEndDate).FirstOrDefault());
                genRecordInfo = FillGenRecord(holdXMCredit);

                if (genRecordInfo != null)
                {
                    result = CreateGenRecord(genRecordInfo, connectionString);
                    if (result == false)
                    {
                        message = "Error in creating gen record for customer: " + holdXMCredit.CustomerID + " PromoCode: " + holdXMCredit.IssuePromoCode + 
                                  " Order: " + holdXMCredit.OrderID;
                        result = InsertErrorLogInfo(holdXMCredit, "Error", message, connectionString);
                    }
                    else
                    {
                        message = "Created promo code : " + holdXMCredit.IssuePromoCode;
                        result = InsertLogInfo(holdXMCredit, message, connectionString);
                    }

                    if (xmCreditInfo[i].DefaultWarehouseID == 1)
                    {
                        //NFR
                        holdWarehouseID = 18;
                        holdXMCredit.WarehouseID = holdWarehouseID;
                        holdXMCredit.IssuePromoCode = Convert.ToString(XMPromoInfo.Where(promo => promo.WarehouseId == holdWarehouseID && promo.PromoNumber == promoCounter)
                                                                             .Select(promo => promo.PromoCode).FirstOrDefault());
                        holdXMCredit.IssuePromoID = Convert.ToInt32(XMPromoInfo.Where(promo => promo.WarehouseId == holdWarehouseID && promo.PromoCode == holdXMCredit.IssuePromoCode)
                                                                             .Select(promoid => promoid.PromoId).FirstOrDefault());
                        holdXMCredit.RollingDays = Convert.ToInt32(XMPromoInfo.Where(promoday => promoday.PromoId == holdXMCredit.IssuePromoID)
                                                                             .Select(promoday => promoday.RollingDays).FirstOrDefault());
                        holdXMCredit.PromoEndDate = Convert.ToDateTime(XMPromoInfo.Where(promodate => promodate.PromoId == holdXMCredit.IssuePromoID)
                                                                     .Select(promodate => promodate.PromoEndDate).FirstOrDefault());

                        genRecordInfo = FillGenRecord(holdXMCredit);

                        if (genRecordInfo != null)
                        {
                            result = CreateGenRecord(genRecordInfo, connectionString);
                            if (result == false)
                            {
                                message = "Error in creating gen record for customer: " + holdXMCredit.CustomerID + " PromoCode: " + holdXMCredit.IssuePromoCode +
                                          " Order: " + holdXMCredit.OrderID;
                                result = InsertErrorLogInfo(holdXMCredit, "Error", message, connectionString);
                            }
                            // Only log the OTG value. This would mess up logic of loop 
                            //message = "Created promo code : " + holdXMCredit.IssuePromoCode;
                            //result = InsertLogInfo(holdXMCredit, message, connectionString);
                        }
                    }
                }
            }
            
            return new DataResponse<List<XMCreditInfo>> { Data = xmCreditInfo };
        }

        public static bool InsertLogInfo(XMCreditInfo xmCreditInfo, string message, ConnectionStringSettings connectionStringExigo)
{

    try
    {
        bool returnValue;

        const string InsertSql = @"INSERT INTO BizAppExtension.XMCreditLog
                                                (CustomerID,WarehouseID,CollectionRequirement,FirstRecurringOrderRequirement,RecurringOrderRequirement,OrderID,
                                                   OrderDate,GenPromoID,GenPromoCode,ProcessDate,CreatedBy,Message)  
                                                VALUES(@CustomerID,@WarehouseID,@CollectionRequirement,@FirstRecurringOrderRequirement,@RecurringOrderRequirement,
                                                        @OrderID,@OrderDate,@GenPromoID,@GenPromoCode,@ProcessDate,@CreatedBy,@Message) ";

        using (var connection = new SqlConnection(connectionStringExigo.ConnectionString))
        {
            returnValue = (connection.Query<bool>(InsertSql,
                             new
                             {
                                 CustomerID = xmCreditInfo.CustomerID,
                                 WarehouseID = xmCreditInfo.WarehouseID,
                                 CollectionRequirement = xmCreditInfo.CollectionRequirement,
                                 FirstRecurringOrderRequirement = xmCreditInfo.FirstRecurringOrderRequirement,
                                 RecurringOrderRequirement = xmCreditInfo.RecurringOrderRequirement,
                                 OrderID = xmCreditInfo.OrderID,
                                 OrderDate = xmCreditInfo.OrderDate,
                                 GenPromoID = xmCreditInfo.IssuePromoID,
                                 GenPromoCode = xmCreditInfo.IssuePromoCode,
                                 ProcessDate = DateTime.Now,
                                 CreatedBy = "xmcredit",
                                 Message = message
                             },
                             commandTimeout: 480
                             )).FirstOrDefault();
            return true;
        }
    }
    catch (Exception ex)
    {
        return false;
    }
}

public static bool InsertErrorLogInfo(XMCreditInfo xmCreditInfo, string level, string message, ConnectionStringSettings connectionStringExigo)
{

    try
    {
        bool returnValue;

        const string InsertSql = @"INSERT INTO BizAppExtension.Log
                                                (Date, Thread, Level, Logger, Message, CustomerID)  
                                                VALUES(@ProcessDate,@Thread,@Level,@Logger,@Message,@CustomerID) ";

        using (var connection = new SqlConnection(connectionStringExigo.ConnectionString))
        {
            returnValue = (connection.Query<bool>(InsertSql,
                             new
                             {
                                 ProcessDate = DateTime.Now,
                                 Thread = 1,
                                 Level = level,
                                 Logger = "xmcredit",
                                 Message = message,
                                 CustomerID = xmCreditInfo.CustomerID
                             },
                             commandTimeout: 480
                             )).FirstOrDefault();
            return true;
        }
    }
    catch (Exception ex)
    {
        return false;
    }
}

public static GenRecordInfo FillGenRecord(XMCreditInfo xmCreditInfo)
{
    GenRecordInfo genRecordInfo = new GenRecordInfo();

    genRecordInfo.CustomerID = xmCreditInfo.CustomerID;
    genRecordInfo.CustomerEmail = xmCreditInfo.Email;
    genRecordInfo.PromoID = xmCreditInfo.IssuePromoID;
    genRecordInfo.PromoCode = xmCreditInfo.IssuePromoCode;
    genRecordInfo.StartDate = DateTime.Now;
    genRecordInfo.RollingDays = xmCreditInfo.RollingDays;
    genRecordInfo.PromoEndDate = xmCreditInfo.PromoEndDate;
    genRecordInfo.CreatedBy = "XMCredit";

    return genRecordInfo;
}

public static bool CreateGenRecord(GenRecordInfo genRecordInfo, ConnectionStringSettings connectionString)
{

    bool returnValue = true;

    try
    {

        if (genRecordInfo.RollingDays != 0)
        {
            genRecordInfo.EndDate = genRecordInfo.StartDate.AddDays(Convert.ToInt32(genRecordInfo.RollingDays));
            
        }
        else
        {
            genRecordInfo.EndDate = genRecordInfo.PromoEndDate;
        }

        const string InsertSql = @"
                              INSERT INTO PromotionsContext.PromoGenRecords
                                   (PromoID,CustomerID,StartDate,EndDate,CreatedBy,BatchID,CustomerEmail,PromoCode,CreatedOrderID,GiverID)
                                VALUES(@promoID, @customerID, @promoStartDate, @endDate, @createdBy, 0, @email, @promoCode, @createdOrderID, @giverID)";



        using (var connection = new SqlConnection(connectionString.ConnectionString))
        {
            returnValue = (connection.Query<bool>(InsertSql,
                             new
                             {
                                 promoID = genRecordInfo.PromoID,
                                 customerID = genRecordInfo.CustomerID,
                                 promoStartDate = genRecordInfo.StartDate,
                                 endDate = genRecordInfo.EndDate,
                                 createdBy = genRecordInfo.CreatedBy,
                                 email = genRecordInfo.CustomerEmail,
                                 promoCode = genRecordInfo.PromoCode,
                                 createdOrderID = DBNull.Value.ToString(),
                                 giverID = DBNull.Value.ToString()
                             },
                             commandTimeout: 480
                             )).FirstOrDefault();
            return true;
        }
    }
    catch (Exception ex)
    {
        Logger.LogInfo("Error in creating XM Gen record for customer: " + genRecordInfo.CustomerID + " promo code: " + genRecordInfo.PromoCode + " error: " + ex.Message);
        return false;
    }
}

public static DataResponse<List<PromoInfo>> getPromoInfo(ConnectionStringSettings connectionStringExigo)
{
    List<PromoInfo> promoInfo = new List<PromoInfo>();

    try
    {
        const string SelectSql = @"SELECT p.PromoID, p.PromoCode as PromoCode, p.RollingDays, p.OrderLimit, p.CustomerLimit, pw.WarehouseID, p.EndDate as PromoEndDate,
                                            SUBSTRING(p.PromoCode,9,1) as PromoNumber  
                                	       FROM PromotionsContext.Promotions p
                                                JOIN PromotionsContext.PromoWarehouses pw ON(pw.PromoID = p.PromoID)
                                                JOIN PromotionsContext.PromoCountrys pc ON(pc.PromoID = p.PromoID)
                                           WHERE p.PromoCode LIKE 'XMCREDIT%'
                                              AND p.Active = 1
                                           GROUP BY p.PromoCode, p.PromoID, p.RollingDays, p.OrderLimit, p.CustomerLimit, p.EndDate, pw.WarehouseID";

        using (var connection = new SqlConnection(connectionStringExigo.ConnectionString))
        {
            promoInfo = (connection.Query<PromoInfo>(SelectSql,
                             commandTimeout: 480
                             )).ToList();
        }
    }
    catch (Exception ex)
    {
        Logger.LogInfo("Error in getting XM Promo information. Error: " + ex.Message);
        return ErrorResponse.CreateErrorResponse<List<PromoInfo>>(ResponseCode.InternalServerError, ex.Message);
    }
    Logger.LogInfo("Successfully getting XM Credit Promo Info");
    return new DataResponse<List<PromoInfo>> { Data = promoInfo };
}
    }
}
