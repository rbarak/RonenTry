using InvestmentTracker.Api.Core.Entities;
using InvestmentTracker.Api.Core.Interfaces;
using InvestmentTracker.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Api.Infrastructure.Repositories;

public class OfficeRepository : IOfficeRepository
{
    private readonly AppDbContext _context;

    public OfficeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> PhoneExistsAsync(string phone) =>
        _context.Offices.AnyAsync(o => o.Phone == phone);

    public async Task<Office> RegisterAsync(Office office)
    {
        _context.Offices.Add(office);
        await _context.SaveChangesAsync();
        return office;
    }
}
