namespace SmartCash.EfCore.Models
{
    public class ItemModel
    {
        public int IdItem { get; set; }
        public decimal Quantidade { get; set; }
        public decimal ValorUnit { get; set; }
        public decimal ValorTotal { get; set; }
        public int IdTransacao { get; set; }
        public int IdProduto { get; set; }

        public TransacaoModel Transacao { get; set; } = null!;
        public ProdutoModel Produto { get; set; } = null!;
    }
}