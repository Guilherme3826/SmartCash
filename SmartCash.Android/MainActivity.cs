using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SmartCash.Android.Services;
using SmartCash.Mensageiros;



namespace SmartCash.Android;

[Activity(
    Label = "SmartCash.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private const string CorBarraEscuro = "#1F2C34";
    private const string CorBarraClaro = "#008069";

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Injeta os serviços específicos do Android antes do App rodar
        App.RegisterPlatformServices = services =>
        {
            services.AddSingleton<Interfaces.ICompartilhamentoService, AndroidCompartilhamentoService>();
            services.AddSingleton<Interfaces.IPermissaoService, AndroidPermissaoService>();
        };

        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .With(new AndroidPlatformOptions
            {
                // No Avalonia 11, usamos RenderingMode para definir a prioridade de renderização
                // Isso substitui o antigo AccelerationMode.
                RenderingMode = new[] { AndroidRenderingMode.Egl }
            });
    }

    protected override void OnCreate(Bundle savedInstanceState)
    {
        // Define a Activity atual para usarmos no serviço de permissão nativo
        Platform.CurrentActivity = this;

        WeakReferenceMessenger.Default.Register<TemaAlteradoMessage>(this, (r, m) =>
        {
            RunOnUiThread(() => AplicarCorDaBarraDeStatus(m.Value));
        });

        base.OnCreate(savedInstanceState);
    }

    protected override void OnResume()
    {
        base.OnResume();

        if (Avalonia.Application.Current != null)
        {
            bool isEscuro = Avalonia.Application.Current.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
            AplicarCorDaBarraDeStatus(isEscuro);
        }
    }

    // O PULO DO GATO: Dispara no exato momento em que o app termina de carregar e está 100% visível
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        if (hasFocus && Avalonia.Application.Current != null)
        {
            bool isEscuro = Avalonia.Application.Current.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
            AplicarCorDaBarraDeStatus(isEscuro);
        }
    }

    private void AplicarCorDaBarraDeStatus(bool isTemaEscuro)
    {
        try
        {
            if (Window == null) return;

            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            string corHexadecimal = isTemaEscuro ? CorBarraEscuro : CorBarraClaro;
            Window.SetStatusBarColor(global::Android.Graphics.Color.ParseColor(corHexadecimal));

            if (isTemaEscuro)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    Window.InsetsController?.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightStatusBars);
                }
                else
                {
#pragma warning disable CS0618
                    Window.DecorView.SystemUiVisibility &= ~(StatusBarVisibility)SystemUiFlags.LightStatusBar;
#pragma warning restore CS0618
                }
            }
            else
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    Window.InsetsController?.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightStatusBars);
                }
                else
                {
#pragma warning disable CS0618
                    Window.DecorView.SystemUiVisibility &= ~(StatusBarVisibility)SystemUiFlags.LightStatusBar;
#pragma warning restore CS0618
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao pintar a barra: {ex.Message}");
        }
    }
}