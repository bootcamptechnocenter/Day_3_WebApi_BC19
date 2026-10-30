namespace EasyCreditApi.Application.DTO.Application;

public class ResInstallmentDto
{
    public int InstallmentId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
