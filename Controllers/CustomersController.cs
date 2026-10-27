using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]                              // Enables automatic validation, binding
[Route("api/[controller]")]                  // Route = api/customers (class name - "Controller")
public class CustomersController : ControllerBase  // ControllerBase = no view support (API only)
{
    private readonly ICustomerService _service;    // Constructor injection

    public CustomersController(ICustomerService service)
    {
        _service = service;
    }

    [HttpGet]                                // GET /api/customers
    public async Task<IActionResult> GetAll()
    {
        var customers = await _service.GetAllAsync();
        return Ok(customers);                 // 200 OK with JSON body
    }

    [HttpGet("{id:int}")]               // GET /api/customers/5  (int constraint!)
    public async Task<IActionResult> GetById(int id)
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

    // [HttpPut("{id}")]                        // PUT /api/customers/5
    // public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    // {
    //     var customer = await _service.UpdateAsync(id, dto);
    //     if (customer is null) return NotFound();
    //     return Ok(customer);
    // }

    // [HttpDelete("{id}")]                     // DELETE /api/customers/5
    // public async Task<IActionResult> Delete(int id)
    // {
    //     var deleted = await _service.DeleteAsync(id);
    //     if (!deleted) return NotFound();
    //     return NoContent();                   // 204
    // }
}
