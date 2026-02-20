using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Repositories
{
    public class ItemRepository : BaseRepository<ItemModel>
    {
        public ItemRepository(MeuDbContext context) : base(context) { }
    }
}