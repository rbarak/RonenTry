using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Api.Core.DTOs;

public class RegisterOfficeRequest
{
    [Required]
    [MaxLength(200)]
    public string? OfficeName { get; set; }

    [Required]
    [MaxLength(200)]
    public string? ManagerName { get; set; }

    [Required]
    [MaxLength(255)]
    [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    public string? Email { get; set; }

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^\d{10,15}$")]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(100)]
    public string? UserName { get; set; }

    [Required]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$")]
    public string? Password { get; set; }
}
