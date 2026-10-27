using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Customer;

public class ReqUpdateCustomerDto
{

    public string Name { get; set; } = string.Empty;


    public string Nik { get; set; } = string.Empty;


    public string? Phone { get; set; }


    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }
}
