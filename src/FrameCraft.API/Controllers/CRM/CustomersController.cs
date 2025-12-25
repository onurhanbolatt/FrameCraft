using FrameCraft.Application.Common.Models;
using FrameCraft.Application.Customers.Commands.CreateCustomer;
using FrameCraft.Application.Customers.Commands.DeleteCustomer;
using FrameCraft.Application.Customers.Commands.UpdateCustomer;
using FrameCraft.Application.Customers.Queries.GetCustomerById;
using FrameCraft.Application.Customers.Queries.GetCustomers;
using FrameCraft.Application.Customers.Queries.GetCustomersPaged;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrameCraft.API.Controllers.CRM;

[Authorize]  // ← TÜM ENDPOINT'LER PROTECTED!
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Sayfalanmış müşteri listesi - Filtreleme, sıralama ve sayfalama ile
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerListDto>>>> GetCustomersPaged(
        [FromQuery] GetCustomersPagedQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PagedResult<CustomerListDto>>.SuccessResult(result));
    }

    /// <summary>
    /// Tüm müşterileri getir (sayfalama yok)
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<List<CustomerListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CustomerListDto>>>> GetAllCustomers(
        [FromQuery] GetCustomersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<List<CustomerListDto>>.SuccessResult(result));
    }

    /// <summary>
    /// ID'ye göre müşteri getir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetCustomerById(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        return Ok(ApiResponse<CustomerDto>.SuccessResult(result!));  
    }

    /// <summary>
    /// Yeni müşteri oluştur
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        var customerId = await _mediator.Send(command);
        return CreatedAtAction(
            nameof(GetCustomerById),
            new { id = customerId },
            ApiResponse<Guid>.SuccessResult(customerId, "Müşteri başarıyla oluşturuldu"));
    }

    /// <summary>
    /// Müşteri güncelle
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> UpdateCustomer(Guid id, [FromBody] UpdateCustomerCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = "URL'deki ID ile gövdedeki ID eşleşmiyor"
            });
        }

        await _mediator.Send(command);
        return Ok(ApiResponse.SuccessResult("Müşteri başarıyla güncellendi"));
    }

    /// <summary>
    /// Müşteri sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteCustomer(Guid id)
    {
        await _mediator.Send(new DeleteCustomerCommand(id));
        return Ok(ApiResponse.SuccessResult("Müşteri başarıyla silindi"));
    }
}