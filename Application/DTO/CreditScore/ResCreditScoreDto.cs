namespace EasyCreditApi.Application.DTO.CreditScore;

// Bentuk response bureau: GET /api/v1/score/{customerId}
public class ResCreditScoreDto
{
    public int CustomerId { get; set; }
    public int Score { get; set; }          // 300-850
    public string Grade { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
