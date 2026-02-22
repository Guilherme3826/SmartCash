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
}
