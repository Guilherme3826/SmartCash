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
            ViewSubAtual = null;

            var vm = App.ServiceProvider.GetRequiredService<AdicionarCategoriaViewModel>();
            ViewSubAtual = vm;
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
        [RelayCommand]
        private async Task ExcluirCategoriaAsync(CategoriaModel categoria)
        {
            if (categoria == null) return;

            try
            {
                // 1. Remove da base de dados através do seu repositório
                // Substitua "DeleteAsync" pelo nome exato do método no seu IBaseRepository
                await _categoriaRepository.DeleteAsync(categoria.IdCategoria);

                // 2. Remove da lista visual para atualizar o ecrã instantaneamente
                // Se estiver a utilizar uma ObservableCollection, pode fazer:
                Categorias.Remove(categoria);

                // Em alternativa, pode recarregar a lista inteira se preferir:
                // await CarregarCategorias();
            }
            catch (Exception ex)
            {
                // NOTA: Se esta categoria já estiver vinculada a "Consumíveis", o Entity Framework 
                // pode gerar uma exceção de restrição de chave estrangeira (Foreign Key).
                // É aconselhável ter aqui um aviso para o utilizador, caso não use "Cascade Delete".
                System.Diagnostics.Debug.WriteLine($"[ERRO AO EXCLUIR] {ex.Message}");
            }
        }
    }
}