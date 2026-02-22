using System.Collections.Generic;

namespace SmartCash.EfCore.Models
{
    public class CategoriaModel
    {
        public int IdCategoria { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string IconeApresentacao { get; set; } = "fa-solid fa-tags"; // Ícone padrão caso não haja um definido

        public ICollection<ConsumiveisModel> Produtos { get; set; } = new List<ConsumiveisModel>();
    }
}