using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.ViewModels;
using System;

namespace SmartCash;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;

        // Se o objeto já for uma View injetada, retorne-a diretamente
        if (data is Control control) return control;

        // Lógica padrão para ViewModels
        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        // Aceita tanto ViewModels quanto as Views injetadas
        return data is CommunityToolkit.Mvvm.ComponentModel.ObservableObject || data is Control;
    }
}
