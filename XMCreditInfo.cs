using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMCredit
{
    public class XMCreditInfo
    {
        
        public int CustomerID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Email { get; set; }
        public int DefaultWarehouseID { get; set; }
        public bool CollectionRequirement { get; set; }
        public bool FirstRecurringOrderRequirement { get; set; }
        public bool RecurringOrderRequirement { get; set; }
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public int LogID { get; set; }
        public int GenPromoID { get; set; }
        public string GenPromoCode { get; set; }
        public DateTime LastRecurringOrderDate { get; set; }
        public int WarehouseID { get; set; }
        public int IssuePromoID { get; set; }
        public string IssuePromoCode { get; set; }
        public int RollingDays { get; set; }
        public DateTime PromoEndDate { get; set; }
    }
}
