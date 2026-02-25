using Android.Content;
using AndroidX.Core.Content;
using Java.IO;
using SmartCash.Interfaces;
using Application = Android.App.Application;

namespace SmartCash.Android.Services
{
    public class AndroidCompartilhamentoService : ICompartilhamentoService
    {
        public void CompartilharArquivo(string caminhoArquivo, string titulo)
        {
            var context = Application.Context;
            var file = new File(caminhoArquivo);

            // Requer a configuração do FileProvider no AndroidManifest e res/xml/file_paths.xml
            var uri = FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", file);

            var intent = new Intent(Intent.ActionSend);
            intent.SetType("application/zip");
            intent.PutExtra(Intent.ExtraStream, uri);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.NewTask);

            var chooserIntent = Intent.CreateChooser(intent, titulo);
            chooserIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(chooserIntent);
        }

        public void AbrirPastaDoArquivo(string caminhoArquivo)
        {
            // Sem comportamento de "explorador de arquivos" no Android, acionamos o compartilhar direto
            CompartilharArquivo(caminhoArquivo, "Abrir Backup");
        }
    }
}