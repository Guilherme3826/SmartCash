using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Models
{
    public class ResumoCategoriaModel
    {
        public CategoriaModel Categoria { get; set; } = null!;
        public string CorHex { get; set; }
        public decimal Total { get; set; }
    }
}
