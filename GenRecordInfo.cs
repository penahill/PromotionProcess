using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMCredit
{
    public class GenRecordInfo
    {
        public int GenId { get; set; }
        public int PromoID { get; set; }
        public int CustomerID { get; set; }
        public int RollingDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CreatedBy { get; set; }
        public string CustomerEmail { get; set; }
        public string PromoCode { get; set; }
        public DateTime PromoEndDate { get; set; }
    }
}
