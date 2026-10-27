namespace EasyCreditApi.Application.DTO.Customer
{
    public class ReqCreateCustomerDto
    {
        // [Required(ErrorMessage = "Nama wajib diisi")]
        // [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // [Required(ErrorMessage = "NIK wajib diisi")]
        // [MaxLength(20)]
        public string Nik { get; set; } = string.Empty;

        // [MaxLength(20)]
        public string? Phone { get; set; }

        // [EmailAddress(ErrorMessage = "Format email tidak valid")]
        // [MaxLength(100)]
        public string? Email { get; set; }

        // [MaxLength(255)]
        public string? Address { get; set; }
    }

}
