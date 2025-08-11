using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Models.FitbitEntities
{
    public class Food
    {
        public bool isFavorite { get; set; }
        public string logDate { get; set; }
        public long logId { get; set; }
        public LoggedFood loggedFood { get; set; }
        public NutritionalValues nutritionalValues { get; set; }
    }
}
