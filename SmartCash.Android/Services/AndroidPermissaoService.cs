using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using SmartCash.Interfaces;
using System.Threading.Tasks;
using Application = Android.App.Application;

namespace SmartCash.Android.Services
{
    public class AndroidPermissaoService : IPermissaoService
    {
        public Task<bool> SolicitarPermissaoArmazenamentoAsync()
        {
            var context = Application.Context;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                if (Environment.IsExternalStorageManager)
                {
                    return Task.FromResult(true);
                }
                else
                {
                    var activity = Platform.CurrentActivity;
                    if (activity != null)
                    {
                        try
                        {
                            var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
                            intent.AddCategory("android.intent.category.DEFAULT");
                            intent.SetData(global::Android.Net.Uri.Parse(string.Format("package:{0}", context.PackageName)));
                            intent.AddFlags(ActivityFlags.NewTask);
                            context.StartActivity(intent);
                        }
                        catch
                        {
                            var intent = new Intent(Settings.ActionManageAllFilesAccessPermission);
                            intent.AddFlags(ActivityFlags.NewTask);
                            context.StartActivity(intent);
                        }
                    }
                    return Task.FromResult(false);
                }
            }
            else
            {
                string permissao = Manifest.Permission.WriteExternalStorage;

                if (ContextCompat.CheckSelfPermission(context, permissao) == Permission.Granted)
                {
                    return Task.FromResult(true);
                }

                var activity = Platform.CurrentActivity;

                if (activity != null)
                {
                    ActivityCompat.RequestPermissions(activity, new string[] { permissao, Manifest.Permission.ReadExternalStorage }, 1001);
                    return Task.FromResult(false);
                }

                return Task.FromResult(false);
            }
        }
    }

    public static class Platform
    {
        public static global::Android.App.Activity? CurrentActivity { get; set; }
    }
}