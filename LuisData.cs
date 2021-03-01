using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceBot
{
    public class LuisData
    {
        public string intent { get; set; }

        public string FacilityType { get; set; }

        public string EmployeeID { get; set; }

        public string FacilityID { get; set; }

        public string FacilityPreference { get; set; }

        public string PersonAmount { get; set; }

        public string Floor { get; set; }

        public string Date { get; set; }
    }
}
