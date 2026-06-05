using InvestmentTracker.Api.Core.Entities;

namespace InvestmentTracker.Api.Core.Interfaces;

public interface IOfficeRepository
{
    Task<bool> PhoneExistsAsync(string phone);
    Task<Office> RegisterAsync(Office office);
}
