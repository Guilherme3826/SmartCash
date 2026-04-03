using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartCash.ViewModels.Consumiveis;
using System;
using System.Linq;

namespace SmartCash.Views.Consumiveis
{
    public partial class AdicionarConsumivelView : UserControl
    {
        public AdicionarConsumivelView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
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

            DataContext = null;
        }

        private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AdicionarConsumivelViewModel vm)
            {
                if (vm.VoltarCommand.CanExecute(null))
                {
                    e.Handled = true;
                    vm.VoltarCommand.Execute(null);
                }
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