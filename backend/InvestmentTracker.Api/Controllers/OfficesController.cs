using InvestmentTracker.Api.Core.DTOs;
using InvestmentTracker.Api.Core.Entities;
using InvestmentTracker.Api.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Api.Controllers;

[ApiController]
[Route("api/offices")]
public class OfficesController : ControllerBase
{
    private readonly IOfficeRepository _repository;
    private readonly ILogger<OfficesController> _logger;

    public OfficesController(IOfficeRepository repository, ILogger<OfficesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Register([FromBody] RegisterOfficeRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Registration validation failed from {IP}", ip);
            return BadRequest(new RegisterOfficeResponse { Success = false, Message = "נתונים שגויים. בדוק את הפרטים ונסה שנית." });
        }

        if (await _repository.PhoneExistsAsync(request.Phone!))
        {
            _logger.LogWarning("Registration attempt with duplicate phone from {IP}", ip);
            return BadRequest(new RegisterOfficeResponse { Success = false, Message = "מספר הטלפון כבר קיים במערכת" });
        }

        var office = new Office
        {
            OfficeName = request.OfficeName!,
            ManagerName = request.ManagerName!,
            Email = request.Email!,
            Phone = request.Phone!,
            UserName = request.UserName!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        try
        {
            var saved = await _repository.RegisterAsync(office);
            _logger.LogInformation("Office registered successfully: OfficeId={OfficeId} from {IP}", saved.OfficeId, ip);
            return Ok(new RegisterOfficeResponse
            {
                Success = true,
                OfficeId = saved.OfficeId,
                Message = "Office registered successfully"
            });
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning("Registration failed — unique constraint violation from {IP}", ip);
            return BadRequest(new RegisterOfficeResponse { Success = false, Message = "מספר הטלפון כבר קיים במערכת" });
        }
    }
}
