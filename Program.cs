using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMCredit
{
    class Program
    {
        static void Main(string[] args)
        {
            string result = null;
            string emails = null;

            try
            {
                if (args.Length == 0)
                {
                    Logger.LogInfo("Need to pass in what process to run. (no argument)");
                }
                else if (args[0] == "creategenrecords")
                {
                    result = XMCreditGenRecords.ProcessXMCredit();

                    if (string.IsNullOrWhiteSpace(result) == false)
                    {
                        emailspace.email em = new emailspace.email();
                        List<string> mess = new List<string>();
                        mess.Add("The XM Credit GenRecord store procedure had an error");
                        mess.Add(result);
                        emails = ConfigurationManager.AppSettings["Emailaddresses"];
                        em.SendMail("XM Credit has failed - " + Environment.MachineName, mess, "BatchError@modere.com", emails);
                    }
                }
                else if (args[0] == "createhistory")
                {
                    var result2 = XMCreditHistory.ProcessXMCreditHistory();

                    if (result2.Error != null)
                    {
                        emailspace.email em = new emailspace.email();
                        List<string> mess = new List<string>();
                        mess.Add("The XM Credit History store procedure had an error");
                        mess.Add(result2.Error.Message);
                        emails = ConfigurationManager.AppSettings["Emailaddresses"];
                        em.SendMail("XM Credit has failed - " + Environment.MachineName, mess, "BatchError@modere.com", emails);
                    }
                }
            }
            catch (Exception ex)
            {
                emailspace.email em = new emailspace.email();
                List<string> mess = new List<string>();
                mess.Add("XM Credit has thrown an exception");
                mess.Add(ex.Message);
                if (ex.InnerException != null)
                {
                    mess.Add(ex.InnerException.ToString());
                }
                emails = ConfigurationManager.AppSettings["Emailaddresses"];
                em.SendMail("XM Credit has failed - " + Environment.MachineName, mess, "BatchError@modere.com", emails);
                Logger.LogError(ex);
            }
        }
    }
}
