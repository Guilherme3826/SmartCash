using Avalonia.Controls;
using Avalonia.Controls.Templates;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;

        // Se o dado já for um controle (View injetada), apenas use-o
        if (data is Control control) return control;

        // Se você cair aqui, é porque o Avalonia tentou renderizar uma ViewModel 
        // que não foi injetada manualmente. Em AOT, evite o Type.GetType.
        return new TextBlock { Text = "Nenhuma View vinculada estaticamente para: " + data.GetType().Name };
    }

    public bool Match(object? data)
    {
        return data is CommunityToolkit.Mvvm.ComponentModel.ObservableObject || data is Control;
    }
}