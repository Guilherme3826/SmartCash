using SmartCash.EfCore.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Interfaces
{
    public interface IItemRepository : IBaseRepository<ItemModel>
    {
        Task<List<ItemModel>> GetItensPorMesAnoAsync(int mes, int ano);
    }
}