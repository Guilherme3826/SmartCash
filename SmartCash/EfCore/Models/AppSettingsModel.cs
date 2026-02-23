using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.EfCore.Models
{
    public class AppSettingsModel
    {
        public string Ambiente { get; set; } = "Produção";
        public bool ModoEscuro { get; set; } = false;
    }
}
