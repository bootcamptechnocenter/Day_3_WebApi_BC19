using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var (items, total) = await _service.GetPagedAsync(keyword, page, size);
        return Ok(new { page, size, total, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var customer = await _service.GetByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReqCreateCustomerDto dto)
    {
        var customer = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.CustomerId },
            customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReqUpdateCustomerDto dto)
    {
        var customer = await _service.UpdateAsync(id, dto);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}