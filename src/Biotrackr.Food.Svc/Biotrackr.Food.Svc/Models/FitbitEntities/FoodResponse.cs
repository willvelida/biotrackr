using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Models.FitbitEntities
{
    public class FoodResponse
    {
        public List<Food> foods { get; set; }
        public Goals goals { get; set; }
        public Summary summary { get; set; }
    }
}
