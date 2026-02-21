using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartCash.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _saldoTotal = "R$ 1.801,73";
    }
}