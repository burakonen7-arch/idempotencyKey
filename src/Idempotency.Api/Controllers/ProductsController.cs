using System.ComponentModel.DataAnnotations;
using Idempotency.Application.DTOs;
using Idempotency.Application.Exceptions;
using Idempotency.Application.Interfaces;
using Idempotency.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Idempotency.Api.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var keys) || keys.Count != 1)
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Tek bir Idempotency-Key başlığı zorunlu.");

        try
        {
            //controller idempotencyKey değerini alır ve createAsync'e gider ve aşağıda validasyon işlemleri gerçekleşir.
            var result = await productService.CreateAsync(idempotencyKey, request, ct);
            
            
            Response.Headers.Location = result.Location;
            Response.Headers["Idempotency-Replayed"] = result.Replayed ? "true" : "false";

            return new ContentResult
            {
                Content = result.Body,
                ContentType = "application/json",
                StatusCode = result.StatusCode
            };
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
        catch (IdempotencyConflictException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                detail: "Bu anahtar farklı bir istek gövdesiyle kullanılmış. Yeni işlem için yeni anahtar kullanın.");
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await productService.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }
}
