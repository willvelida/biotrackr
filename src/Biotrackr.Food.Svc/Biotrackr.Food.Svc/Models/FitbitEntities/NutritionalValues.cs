using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Models.FitbitEntities
{
    public class NutritionalValues
    {
        public int calories { get; set; }
        public double carbs { get; set; }
        public double fat { get; set; }
        public int fiber { get; set; }
        public int protein { get; set; }
        public int sodium { get; set; }
    }
}
