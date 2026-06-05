using System.ComponentModel.DataAnnotations;
using InvestmentTracker.Api.Core.DTOs;

namespace InvestmentTracker.Api.Tests;

public class RegisterOfficeRequestValidationTests
{
    private static IList<ValidationResult> Validate(RegisterOfficeRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        Assert.Empty(Validate(ValidRequest()));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void InvalidEmail_FailsValidation(string email)
    {
        var req = ValidRequest();
        req.Email = email;
        Assert.NotEmpty(Validate(req));
    }

    [Fact]
    public void ValidEmail_PassesValidation()
    {
        var req = ValidRequest();
        req.Email = "user.name+tag@sub.domain.co.il";
        Assert.Empty(Validate(req));
    }

    [Theory]
    [InlineData("123456789")]        // 9 digits — too short
    [InlineData("1234567890123456")] // 16 digits — too long
    [InlineData("054-123-4567")]     // contains dashes
    [InlineData("054abc45678")]      // contains letters
    public void InvalidPhone_FailsValidation(string phone)
    {
        var req = ValidRequest();
        req.Phone = phone;
        Assert.NotEmpty(Validate(req));
    }

    [Theory]
    [InlineData("0541234567")]       // 10 digits
    [InlineData("054123456789012")]  // 15 digits
    public void ValidPhone_PassesValidation(string phone)
    {
        var req = ValidRequest();
        req.Phone = phone;
        Assert.Empty(Validate(req));
    }

    [Theory]
    [InlineData("short1@")]          // too short (7 chars)
    [InlineData("nouppercase1@abc")] // no uppercase letter
    [InlineData("NODIGIT@Passabc")]  // no digit
    [InlineData("NoSpecial1pass")]   // no special character
    [InlineData("Aa1!567")]          // 7 chars — one short
    public void WeakPassword_FailsValidation(string password)
    {
        var req = ValidRequest();
        req.Password = password;
        Assert.NotEmpty(Validate(req));
    }

    [Theory]
    [InlineData("Password@123")]
    [InlineData("Str0ng!Pass")]
    [InlineData("Abcdef1!")]         // exactly 8 chars — minimum valid
    public void StrongPassword_PassesValidation(string password)
    {
        var req = ValidRequest();
        req.Password = password;
        Assert.Empty(Validate(req));
    }

    [Theory]
    [InlineData(nameof(RegisterOfficeRequest.OfficeName))]
    [InlineData(nameof(RegisterOfficeRequest.ManagerName))]
    [InlineData(nameof(RegisterOfficeRequest.Email))]
    [InlineData(nameof(RegisterOfficeRequest.Phone))]
    [InlineData(nameof(RegisterOfficeRequest.UserName))]
    [InlineData(nameof(RegisterOfficeRequest.Password))]
    public void NullRequiredField_FailsValidation(string fieldName)
    {
        var req = ValidRequest();
        typeof(RegisterOfficeRequest).GetProperty(fieldName)!.SetValue(req, null);
        Assert.NotEmpty(Validate(req));
    }

    private static RegisterOfficeRequest ValidRequest() => new()
    {
        OfficeName = "ABC CPA",
        ManagerName = "David Cohen",
        Email = "david@abc.com",
        Phone = "0541234567",
        UserName = "abcuser",
        Password = "Password@123"
    };
}
