using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using PublicUtilitiesFunction.Services;
using Microsoft.Extensions.DependencyInjection;

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
            //builder.Services.AddHttpClient<IApiService, ApiService>();
            //builder.Services.AddTransient<IApiService, ApiService>();
            builder.Services.AddTransient<IScrapingService, ScrapingService>();
        }
    }
}
