using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace XMCredit
{
    public class Logger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Logger));
        public static void LogError(string status, params object[] args)
        {
            Log.ErrorFormat(status, args);
        }

        public static void LogError(Exception ex, params object[] args)
        {
            string error = ex.Message;
            if (ex.InnerException != null)
                error += "\r\n" + ex.InnerException.Message;
            error += "\r\n" + ex.StackTrace;

            Log.ErrorFormat(error, args);
        }

        public static void LogInfo(string status, params object[] args)
        {
            Log.InfoFormat(status, args);
        }
    }
}
