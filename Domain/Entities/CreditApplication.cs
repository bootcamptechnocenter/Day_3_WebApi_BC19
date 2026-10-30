namespace EasyCreditApi.Domain.Entities;

public class CreditApplication
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public decimal InterestRate { get; set; }   // persen per tahun, mis. 12.50
    public string? Purpose { get; set; }
    public string Status { get; set; } = CreditApplicationStatus.Draft;
    public int? CreditScore { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Nilai STATUS di EASYCREDIT.CREDIT_APPLICATIONS (lihat CK_CREDIT_APP_STATUS).
public static class CreditApplicationStatus
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";

    public static readonly IReadOnlyList<string> All = [Draft, Submitted, Approved, Rejected];
}
