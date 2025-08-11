using Biotrackr.Food.Svc.Models.FitbitEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Services.Interfaces
{
    public interface IFoodService
    {
        Task MapAndSaveDocument(string date, FoodResponse foodResponse);
    }
}
