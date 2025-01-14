using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using PublicUtilitiesFunction.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(PublicUtilitiesFunction.Startup))]

namespace PublicUtilitiesFunction
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            base.ConfigureAppConfiguration(builder);
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Registro de servicios necesarios
            builder.Services.AddScoped<IScrapingService, ScrapingService>(); // Cambiar a Scoped para manejar dependencias en cada solicitud
            builder.Services.AddHttpClient(); // Registrar HttpClient para manejar solicitudes HTTP de forma óptima

            //builder.Services.AddHttpClient<IApiService, ApiService>();
            //builder.Services.AddTransient<IApiService, ApiService>();
            //builder.Services.AddTransient<IScrapingService, ScrapingService>();
        }
    }
}
