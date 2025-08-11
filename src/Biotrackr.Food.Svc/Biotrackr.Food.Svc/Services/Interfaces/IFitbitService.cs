using Biotrackr.Food.Svc.Models.FitbitEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Services.Interfaces
{
    public interface IFitbitService
    {
        Task<FoodResponse> GetFoodResponse(string date);
    }
}
