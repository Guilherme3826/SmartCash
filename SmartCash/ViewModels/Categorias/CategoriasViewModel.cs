using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models; // Necessário para acessar o CategoriaModel
using SmartCash.Views.Categorias;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.ViewModels.Categorias
{
    public partial class CategoriasViewModel : ObservableObject
    {

        [ObservableProperty]
        private object? _viewSubAtual;

        [ObservableProperty]
        private bool _exibindoLista = true;












        private readonly IBaseRepository<CategoriaModel> _categoriaRepository;

        [ObservableProperty]
        private ObservableCollection<CategoriaModel> _categorias = new();


        [RelayCommand]
        private void AdicionarNovaCategoria()
        {
            var view = App.ServiceProvider.GetRequiredService<AdicionarCategoriaView>();
            ViewSubAtual = view;
            ExibindoLista = false;
        }



        

        // O construtor agora recebe a interface vinculada ao Modelo correto
        public CategoriasViewModel(IBaseRepository<CategoriaModel> categoriaRepository)
        {
            ExibindoLista = true;
            _categoriaRepository = categoriaRepository;            
            _ = CarregarCategorias();
        }

        public CategoriasViewModel()
        {
            
        }
        public async Task CarregarCategorias()
        {
            Categorias.Clear();
            var lista = await _categoriaRepository.GetAllAsync();

            foreach(var item in lista)
            {
                Categorias.Add(item);
            }
        }
    }
}