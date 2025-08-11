using Biotrackr.Food.Svc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Repositories.Interfaces
{
    public interface ICosmosRepository
    {
        Task CreateFoodDocument(FoodDocument foodDocument);
    }
}
