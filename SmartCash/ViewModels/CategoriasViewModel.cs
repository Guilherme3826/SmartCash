using CommunityToolkit.Mvvm.ComponentModel;
using SmartCash.EfCore.Interfaces;
using SmartCash.EfCore.Models; // Necessário para acessar o CategoriaModel
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.ViewModels
{
    public partial class CategoriasViewModel : ObservableObject
    {
        private readonly IBaseRepository<CategoriaModel> _categoriaRepository;

        [ObservableProperty]
        private ObservableCollection<CategoriaModel> _categorias = new();

        

        // O construtor agora recebe a interface vinculada ao Modelo correto
        public CategoriasViewModel(IBaseRepository<CategoriaModel> categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
            _ = CarregarCategorias();
        }

        public CategoriasViewModel()
        {
            
        }
        private async Task CarregarCategorias()
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