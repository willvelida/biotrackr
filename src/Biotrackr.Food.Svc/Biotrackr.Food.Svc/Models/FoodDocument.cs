using Biotrackr.Food.Svc.Models.FitbitEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Models
{
    public class FoodDocument
    {
        public string? Id { get; set; }
        public FoodResponse? Food { get; set; }
        public string? Date { get; set; }
        public string? DocumentType { get; set; }
    }
}
