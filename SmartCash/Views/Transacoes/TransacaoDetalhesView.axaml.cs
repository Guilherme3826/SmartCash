using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartCash.ViewModels.Transacoes;

namespace SmartCash.Views.Transacoes;

public partial class TransacaoDetalhesView : UserControl
{
    public TransacaoDetalhesView()
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
        if (topLevel != null) topLevel.BackRequested -= TopLevel_BackRequested;
    }

    private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
    {
        // Como o DataContext é definido pelo DataTemplate, pegamos ele aqui
        if (DataContext is TransacaoDetalhesViewModel vm)
        {
            e.Handled = true;
            vm.VoltarCommand.Execute(null);
        }
    }
}
