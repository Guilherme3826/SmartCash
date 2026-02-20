using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Repositories
{
    public class ProdutoRepository : BaseRepository<ProdutoModel>
    {
        public ProdutoRepository(MeuDbContext context) : base(context) { }
    }
}