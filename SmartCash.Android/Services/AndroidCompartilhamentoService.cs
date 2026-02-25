using Android.Content;
using AndroidX.Core.Content;
using Avalonia.Threading;
using SmartCash.Interfaces;
using Java.IO;
using System;

namespace SmartCash.Android.Services
{
    public class AndroidCompartilhamentoService : ICompartilhamentoService
    {
        public void AbrirPastaDoArquivo(string caminhoArquivo)
        {
            throw new NotImplementedException();
        }

        public void CompartilharArquivo(string caminho, string titulo)
        {
            try
            {
                // Garante a execução na thread da UI
                Dispatcher.UIThread.Post(() =>
                {
                    // Pega a instância garantida da MainActivity
                    var activity = MainActivity.Instance;

                    if (activity == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[AndroidCompartilhamento] ERRO: MainActivity.Instance está null.");
                        return;
                    }

                    var file = new File(caminho);

                    var uri = FileProvider.GetUriForFile(
                        activity,
                        activity.PackageName + ".fileprovider",
                        file);

                    var intent = new Intent(Intent.ActionSend);
                    intent.SetType("application/zip");
                    intent.PutExtra(Intent.ExtraStream, uri);

                    // Permissões obrigatórias para Android 14
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                    var chooser = Intent.CreateChooser(intent, titulo);
                    chooser.AddFlags(ActivityFlags.NewTask);

                    activity.StartActivity(chooser);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidCompartilhamento] Erro crítico no Compartilhamento Android: {ex.Message}");
            }
        }
    }
}