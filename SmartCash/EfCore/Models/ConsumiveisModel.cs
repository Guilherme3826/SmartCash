using System.Collections.Generic;

namespace SmartCash.EfCore.Models
{
    public class ConsumiveisModel
    {
        public int IdProduto { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int IdCategoria { get; set; }
        public decimal Valor { get; set; }

        public CategoriaModel Categoria { get; set; } = null!;
        public ICollection<ItemModel> Itens { get; set; } = new List<ItemModel>();
    }
}