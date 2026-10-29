using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase  // ControllerBase = no view support (API only)
{
    private readonly ICustomerService _service;    // Constructor injection

    public CustomersController(ICustomerService service)
    {
        _service = service;
    }

    [HttpGet]                                // GET /api/customers
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 2)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var customers = await _service.GetAllAsync();
        var items = customers.Skip((page - 1) * size).Take(size).ToList();

        return Ok(new { page, size, total = customers.Count, items  });                 // 200 OK with JSON body
    }

    [HttpGet("{id}")]               // GET /api/customers/5  (int constraint!)
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var customer = await _service.GetByIdAsync(id);
        if (customer is null)
            return NotFound();                // 404
        return Ok(customer);
    }

    [HttpPost]                               // POST /api/customers
    public async Task<IActionResult> Create([FromBody] ReqCreateCustomerDto dto)
    {
        var customer = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),                  // Location header: /api/customers/{newId}
            new
            {
                id = customer.CustomerId,
                name = customer.Name,
                nik = customer.Nik,
                phone = customer.Phone,
                email = customer.Email,
                address = customer.Address
            },
            customer);
    }

    [HttpPut("{id}")]                        // PUT /api/customers/5
    public async Task<IActionResult> Update(int id, [FromBody] ReqUpdateCustomerDto dto)
    {
        var customer = await _service.UpdateAsync(id, dto);
        if (customer is null) return NotFound();
        return Ok(customer);
    }
// }

    [HttpDelete("{id}")]                     // DELETE /api/customers/5
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        return await GetAll();                   // 204
    }
}
