using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Application;

public class ReqCreateApplicationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "CustomerId wajib diisi.")]
    public int CustomerId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Amount harus lebih dari 0.")]
    public decimal Amount { get; set; }

    [Range(1, 120, ErrorMessage = "TenorMonths harus 1-120 bulan.")]
    public int TenorMonths { get; set; }

    [Range(typeof(decimal), "0", "100",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "InterestRate harus 0-100 (% per tahun).")]
    public decimal InterestRate { get; set; }

    [StringLength(200)]
    public string? Purpose { get; set; }
}
