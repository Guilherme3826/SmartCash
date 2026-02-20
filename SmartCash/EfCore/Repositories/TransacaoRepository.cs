using SmartCash.EfCore.Models;

namespace SmartCash.EfCore.Repositories
{
    public class TransacaoRepository : BaseRepository<TransacaoModel>
    {
        public TransacaoRepository(MeuDbContext context) : base(context) { }
    }
}