using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SmartCash.ViewModels.Transacoes;
using System;
using System.Linq;

namespace SmartCash.Views.Transacoes
{
    public partial class AdicionarTransacaoView : UserControl
    {
        public AdicionarTransacaoView(AdicionarTransacaoViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Assina o evento de clique do botão para limpar o foco
            BtnAdicionar.Click += BtnAdicionar_Click;
        }

        public AdicionarTransacaoView()
        {
            InitializeComponent();
        }

        private void BtnAdicionar_Click(object? sender, RoutedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Força a limpeza visual do texto caso o binding sofra atraso
                if (AutoCompleteProdutos != null)
                {
                    AutoCompleteProdutos.Text = string.Empty;
                }

                var topLevel = TopLevel.GetTopLevel(this);
                topLevel?.FocusManager?.ClearFocus();

                // Foca na View para garantir que o cursor saia de qualquer TextBox
                this.Focus();

            }, DispatcherPriority.Background);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                // Garante que não haja duplicidade de inscrição
                topLevel.BackRequested -= TopLevel_BackRequested;
                topLevel.BackRequested += TopLevel_BackRequested;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                topLevel.BackRequested -= TopLevel_BackRequested;
            }

            // Limpa o DataContext para evitar o erro de visual parent ao navegar novamente
            DataContext = null;
        }

        private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AdicionarTransacaoViewModel vm)
            {
                // Intercepta a ação e executa o comando de fechar/cancelar a inserção
                if (vm.VoltarCommand.CanExecute(null))
                {
                    e.Handled = true;
                    vm.VoltarCommand.Execute(null);
                }
            }
        }

        private void AutoComplete_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Limpa o foco atual do escopo da janela
                    TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
                });
            }
        }

        // NOVO: Handler usando TextChanged contorna os teclados virtuais bloqueando letras dinamicamente
        public void NumericTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                var originalText = textBox.Text;

                // Filtra o texto mantendo apenas números e separadores de decimais
                var cleanText = new string(originalText.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());

                if (originalText != cleanText)
                {
                    // Restaura a posição do cursor após apagar a letra inválida
                    var caretIndex = textBox.CaretIndex;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = Math.Max(0, caretIndex - (originalText.Length - cleanText.Length));
                }
            }
        }
    }
}