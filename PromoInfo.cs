using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMCredit
{
    public class PromoInfo
    {
        public int PromoId { get; set; }
        public string PromoCode { get; set; }
        public int RollingDays { get; set; }
        public int PerOrderLimit { get; set; }
        public int CustomerLimit { get; set; }
        public int WarehouseId { get; set; }
        public DateTime PromoEndDate { get; set; }
        public int PromoNumber { get; set; }
    }
}
