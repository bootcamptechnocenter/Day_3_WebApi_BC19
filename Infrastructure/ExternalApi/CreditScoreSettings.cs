namespace EasyCreditApi.Infrastructure.ExternalApi;

// Section "CreditScoreApi" di appsettings.json.
public class CreditScoreSettings
{
    public const string SectionName = "CreditScoreApi";

    public string BaseUrl { get; set; } = string.Empty;   // kosong -> FakeCreditScoreService
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
    public bool UseRefit { get; set; }
}
