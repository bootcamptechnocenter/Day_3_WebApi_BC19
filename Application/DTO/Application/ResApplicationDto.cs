namespace EasyCreditApi.Application.DTO.Application;

public class ResApplicationDto
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public decimal InterestRate { get; set; }
    public string? Purpose { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CreditScore { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
