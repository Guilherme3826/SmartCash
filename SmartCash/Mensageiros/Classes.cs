using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.Mensageiros
{
    public class TransacaoSelecionadaMessage : ValueChangedMessage<int>
    {
        public TransacaoSelecionadaMessage(int value) : base(value)
        {
        }
    }
    public class NavegarParaCategoriaDetalhesGlobalMessage
    {
        public int IdCategoria { get; }
        public string PeriodoFiltro { get; }

        public NavegarParaCategoriaDetalhesGlobalMessage(int idCategoria, string periodoFiltro)
        {
            IdCategoria = idCategoria;
            PeriodoFiltro = periodoFiltro;
        }
    }
    public class CategoriaDetalhesMessage
    {
        public int IdCategoria { get; }
        public string PeriodoFiltro { get; }

        public CategoriaDetalhesMessage(int idCategoria, string periodoFiltro)
        {
            IdCategoria = idCategoria;
            PeriodoFiltro = periodoFiltro;
        }
    }
    public class TemaAlteradoMessage : ValueChangedMessage<bool>
    {
        public TemaAlteradoMessage(bool isDark) : base(isDark)
        {
        }
    }
    public class NovaTransacaoAdicionada { }
    public class NovoConsumivelAdicionado { }
    
}
