using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Repositories
{
    public class CategoriaRepository : BaseRepository<CategoriaModel>
    {
        public CategoriaRepository(MeuDbContext context) : base(context) { }
    }
}