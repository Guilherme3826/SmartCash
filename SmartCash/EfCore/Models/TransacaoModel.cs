using System;
using System.Collections.Generic;

namespace SmartCash.EfCore.Models
{
    public class TransacaoModel
    {
        public int IdTransacao { get; set; }
        public DateTime Data { get; set; }
        public decimal ValorTotal { get; set; }

        public ICollection<ItemModel> Itens { get; set; } = new List<ItemModel>();
    }
}