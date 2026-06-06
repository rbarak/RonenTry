using InvestmentTracker.Api.Controllers;
using InvestmentTracker.Api.Core.DTOs;
using InvestmentTracker.Api.Core.Entities;
using InvestmentTracker.Api.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvestmentTracker.Api.Tests;

public class OfficesControllerTests
{
    private readonly Mock<IOfficeRepository> _repoMock = new();
    private readonly Mock<ILogger<OfficesController>> _loggerMock = new();
    private readonly OfficesController _controller;

    public OfficesControllerTests()
    {
        _controller = new OfficesController(_repoMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithOfficeId()
    {
        var request = ValidRequest();
        _repoMock.Setup(r => r.PhoneExistsAsync(request.Phone!)).ReturnsAsync(false);
        _repoMock.Setup(r => r.RegisterAsync(It.IsAny<Office>()))
            .ReturnsAsync((Office o) => { o.OfficeId = 111; return o; });

        var result = await _controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RegisterOfficeResponse>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(111, response.OfficeId);
    }

    [Fact]
    public async Task Register_DuplicatePhone_ReturnsBadRequest()
    {
        var request = ValidRequest();
        _repoMock.Setup(r => r.PhoneExistsAsync(request.Phone!)).ReturnsAsync(true);

        var result = await _controller.Register(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<RegisterOfficeResponse>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("טלפון", response.Message);
    }

    [Fact]
    public async Task Register_DbUpdateException_ReturnsBadRequestNot500()
    {
        var request = ValidRequest();
        _repoMock.Setup(r => r.PhoneExistsAsync(request.Phone!)).ReturnsAsync(false);
        _repoMock.Setup(r => r.RegisterAsync(It.IsAny<Office>()))
            .ThrowsAsync(new DbUpdateException("unique constraint", new Exception()));

        var result = await _controller.Register(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<RegisterOfficeResponse>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Email", "Invalid email");

        var result = await _controller.Register(ValidRequest());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<RegisterOfficeResponse>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Register_PasswordStoredAsBcryptHash_NotPlainText()
    {
        var request = ValidRequest();
        Office? captured = null;
        _repoMock.Setup(r => r.PhoneExistsAsync(request.Phone!)).ReturnsAsync(false);
        _repoMock.Setup(r => r.RegisterAsync(It.IsAny<Office>()))
            .Callback<Office>(o => captured = o)
            .ReturnsAsync((Office o) => { o.OfficeId = 111; return o; });

        await _controller.Register(request);

        Assert.NotNull(captured);
        Assert.NotEqual(request.Password, captured.PasswordHash);
        Assert.StartsWith("$2", captured.PasswordHash);
    }

    [Fact]
    public async Task Register_PhoneCheckCalledOnce_BeforeInsert()
    {
        var request = ValidRequest();
        _repoMock.Setup(r => r.PhoneExistsAsync(request.Phone!)).ReturnsAsync(false);
        _repoMock.Setup(r => r.RegisterAsync(It.IsAny<Office>()))
            .ReturnsAsync((Office o) => { o.OfficeId = 111; return o; });

        await _controller.Register(request);

        _repoMock.Verify(r => r.PhoneExistsAsync(request.Phone!), Times.Once);
        _repoMock.Verify(r => r.RegisterAsync(It.IsAny<Office>()), Times.Once);
    }

    private static RegisterOfficeRequest ValidRequest() => new()
    {
        OfficeName = "Test Office",
        ManagerName = "Test Manager",
        Email = "test@example.com",
        Phone = "0541234567",
        UserName = "testuser",
        Password = "Password@123"
    };
}
