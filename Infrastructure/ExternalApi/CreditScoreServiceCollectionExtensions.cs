using EasyCreditApi.Application.Interfaces;
using Refit;

namespace EasyCreditApi.Infrastructure.ExternalApi;

public static class CreditScoreServiceCollectionExtensions
{
    // BaseUrl kosong      -> FakeCreditScoreService
    // UseRefit = true     -> Refit client (ICreditScoreApi) + RefitCreditScoreService
    // selain itu          -> typed HttpClient CreditScoreService
    public static IServiceCollection AddCreditScoreService(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(CreditScoreSettings.SectionName).Get<CreditScoreSettings>()
                       ?? new CreditScoreSettings();

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            services.AddSingleton<ICreditScoreService, FakeCreditScoreService>();
            return services;
        }

        void Configure(HttpClient client)
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            if (!string.IsNullOrEmpty(settings.ApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
        }

        if (settings.UseRefit)
        {
            // Refit 16: client dibuat source generator saat compile (tanpa reflection).
            services.AddRefitGeneratedClient<ICreditScoreApi>().ConfigureHttpClient(Configure);
            services.AddScoped<ICreditScoreService, RefitCreditScoreService>();
        }
        else
        {
            services.AddHttpClient<ICreditScoreService, CreditScoreService>(Configure);
        }

        return services;
    }
}
