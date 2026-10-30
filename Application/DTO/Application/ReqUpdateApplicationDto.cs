using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Application;

// CustomerId tidak bisa diganti; buat pengajuan baru kalau customer berbeda.
public class ReqUpdateApplicationDto
{
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
