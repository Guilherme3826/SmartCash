using System.Collections.Generic;

namespace SmartCash.EfCore.Models
{
    public class CategoriaModel
    {
        public int IdCategoria { get; set; }
        public string Nome { get; set; } = string.Empty;

        public ICollection<ProdutoModel> Produtos { get; set; } = new List<ProdutoModel>();
    }
}