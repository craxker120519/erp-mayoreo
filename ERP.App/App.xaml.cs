using System.Globalization;

namespace ERP.App;

public partial class App : Application
{
    public App()
    {
        // Moneda/fechas en formato México sin importar la configuración regional de la
        // PC donde se instale -- el negocio siempre opera en pesos mexicanos.
        var culture = new CultureInfo("es-MX");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage())
        {
            Title = "ERP",
            Width = 1280,
            Height = 800,
            MinimumWidth = 900,
            MinimumHeight = 600
        };
    }
}
