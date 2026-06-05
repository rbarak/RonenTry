namespace InvestmentTracker.Api.Core.DTOs;

public class RegisterOfficeResponse
{
    public bool Success { get; set; }
    public int? OfficeId { get; set; }
    public string Message { get; set; } = string.Empty;
}
