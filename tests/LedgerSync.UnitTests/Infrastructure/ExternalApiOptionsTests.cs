using System.ComponentModel.DataAnnotations;
using LedgerSync.Infrastructure.ExternalSource;

namespace LedgerSync.UnitTests.Infrastructure;

/// <summary>
/// A broken external API address must stop the application at startup, not fail every sync.
/// </summary>
public sealed class ExternalApiOptionsTests
{
    [Theory]
    [InlineData("http://localhost:5080", "/mock/external/documents")]
    [InlineData("https://erp.example.com", "/api/v1/documents?type=all")]
    public void Validate_CorrectAddress_IsValid(string baseUrl, string path) =>
        Assert.Empty(Validate(new ExternalApiOptions { BaseUrl = baseUrl, DocumentsPath = path }));

    [Theory]
    [InlineData("", "/documents", "BaseUrl")]
    [InlineData("ftp://files.example.com", "/documents", "BaseUrl")]
    [InlineData("localhost:5080", "/documents", "BaseUrl")]
    [InlineData("http://localhost:5080", "C:/Program Files/Git/documents", "DocumentsPath")]
    [InlineData("http://localhost:5080", "documents", "DocumentsPath")]
    public void Validate_BrokenAddress_IsRejected(string baseUrl, string path, string field) =>
        Assert.Contains(Validate(new ExternalApiOptions { BaseUrl = baseUrl, DocumentsPath = path }),
            r => r.MemberNames.Contains(field));

    private static List<ValidationResult> Validate(ExternalApiOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
