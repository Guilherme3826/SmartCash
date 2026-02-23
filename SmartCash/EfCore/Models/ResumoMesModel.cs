using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Models
{
    public class ResumoMesModel
    {
        public string MesAnoApresentacao { get; set; } = string.Empty;
        public int Mes { get; set; }
        public int Ano { get; set; }
        public decimal Total { get; set; }

        // Propriedade utilizada pela interface XAML para desenhar a barra proporcional
        public double AlturaBarra { get; set; }
    }
}
