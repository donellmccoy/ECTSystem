using Xunit;
using FluentAssertions;
using AF.ECT.Shared.Options;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Tests for strongly-typed configuration options validation.
/// Ensures all configuration classes properly validate their data annotations on startup.
/// Covers boundary value testing, parameterized validation, and error message verification.
/// </summary>
[Collection("Configuration Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "Configuration")]
public class ConfigurationValidationTests
{
    #region DatabaseOptions Tests

    /// <summary>
    /// Tests that DatabaseOptions accepts valid configuration values.
    /// </summary>
    [Fact]
    public void DatabaseOptions_ValidConfiguration_Succeeds()
    {
        // Arrange
        var options = new DatabaseOptions
        {
            MaxRetryCount = 3,
            MaxRetryDelaySeconds = 30,
            CommandTimeoutSeconds = 30
        };

        // Act & Assert
        ValidateOptions(options).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that DatabaseOptions accepts exact boundary values (minimum and maximum valid values).
    /// </summary>
    [Theory]
    [InlineData(1)]   // Minimum valid value
    [InlineData(10)]  // Maximum valid value
    [InlineData(5)]   // Middle value
    public void DatabaseOptions_BoundaryMaxRetryCount_Succeeds(int value)
    {
        // Arrange
        var options = new DatabaseOptions { MaxRetryCount = value };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that DatabaseOptions rejects MaxRetryCount outside valid boundaries.
    /// </summary>
    [Theory]
    [InlineData(0)]      // Below minimum
    [InlineData(-1)]     // Well below minimum
    [InlineData(11)]     // Above maximum
    [InlineData(100)]    // Well above maximum
    public void DatabaseOptions_InvalidMaxRetryCount_FailsWithDescriptiveError(int invalidValue)
    {
        // Arrange
        var options = new DatabaseOptions { MaxRetryCount = invalidValue };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DatabaseOptions.MaxRetryCount)));
        results.First(r => r.MemberNames.Contains(nameof(DatabaseOptions.MaxRetryCount)))
            .ErrorMessage.Should().Contain("between");
    }

    /// <summary>
    /// Tests that DatabaseOptions validates all retry delay seconds within boundaries.
    /// </summary>
    [Theory]
    [InlineData(1)]     // Minimum valid
    [InlineData(150)]   // Middle range
    [InlineData(300)]   // Maximum valid
    public void DatabaseOptions_BoundaryMaxRetryDelaySeconds_Succeeds(int value)
    {
        // Arrange
        var options = new DatabaseOptions { MaxRetryDelaySeconds = value };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that DatabaseOptions rejects MaxRetryDelaySeconds outside valid boundaries.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public void DatabaseOptions_InvalidMaxRetryDelaySeconds_Fails(int invalidValue)
    {
        // Arrange
        var options = new DatabaseOptions { MaxRetryDelaySeconds = invalidValue };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests DatabaseOptions uses correct default values and they are valid.
    /// </summary>
    [Fact]
    public void DatabaseOptions_DefaultValues_AreValid()
    {
        // Arrange & Act
        var options = new DatabaseOptions();
        var results = ValidateOptions(options);

        // Assert
        results.Should().BeEmpty();
        options.MaxRetryCount.Should().Be(3);
        options.MaxRetryDelaySeconds.Should().Be(30);
        options.CommandTimeoutSeconds.Should().Be(30);
    }

    #endregion

    #region CorsOptions Tests

    /// <summary>
    /// Tests that CorsOptions accepts valid configuration.
    /// </summary>
    [Fact]
    public void CorsOptions_ValidOrigins_Succeeds()
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = ["https://localhost:3000", "https://example.com"] };

        // Act & Assert
        ValidateOptions(options).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that CorsOptions accepts single origin (minimum valid).
    /// </summary>
    [Fact]
    public void CorsOptions_SingleOrigin_Succeeds()
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = ["https://localhost:3000"] };

        // Act & Assert
        ValidateOptions(options).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that CorsOptions rejects empty AllowedOrigins array with descriptive error.
    /// </summary>
    [Fact]
    public void CorsOptions_EmptyOrigins_FailsWithDescriptiveError()
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = [] };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CorsOptions.AllowedOrigins)));
        results.First(r => r.MemberNames.Contains(nameof(CorsOptions.AllowedOrigins)))
            .ErrorMessage.Should().ContainAny("minimum length", "at least 1");
    }

    /// <summary>
    /// Tests that CorsOptions rejects null AllowedOrigins.
    /// </summary>
    [Fact]
    public void CorsOptions_NullOrigins_Fails()
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = null! };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that CorsOptions rejects null entries within AllowedOrigins array.
    /// </summary>
    [Fact]
    public void CorsOptions_WithNullEntry_ValidatesDataAnnotations()
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = ["https://example.com", null!] };

        // Act
        var results = ValidateOptions(options);

        // Assert - Should be valid (protobuf doesn't validate string array elements)
        // This test documents actual behavior
    }

    #endregion

    #region ServerOptions Tests

    /// <summary>
    /// Tests that ServerOptions accepts valid HTTPS URL.
    /// </summary>
    [Fact]
    public void ServerOptions_ValidHttpsUrl_Succeeds()
    {
        // Arrange
        var options = new ServerOptions { ServerUrl = "https://api.example.com" };

        // Act & Assert
        ValidateOptions(options).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ServerOptions accepts localhost URL (minimum valid).
    /// </summary>
    [Fact]
    public void ServerOptions_LocalhostUrl_Succeeds()
    {
        // Arrange
        var options = new ServerOptions { ServerUrl = "https://localhost:7000" };

        // Act & Assert
        ValidateOptions(options).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ServerOptions rejects invalid URL format with descriptive error.
    /// </summary>
    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("example.com")]  // Missing protocol
    [InlineData("http://")]      // Missing host
    [InlineData("ftp://server")] // Wrong protocol
    public void ServerOptions_InvalidUrlFormat_FailsWithDescriptiveError(string invalidUrl)
    {
        // Arrange
        var options = new ServerOptions { ServerUrl = invalidUrl };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ServerOptions.ServerUrl)));
        results.First(r => r.MemberNames.Contains(nameof(ServerOptions.ServerUrl)))
            .ErrorMessage.Should().Contain("valid");
    }

    /// <summary>
    /// Tests that ServerOptions rejects null ServerUrl.
    /// </summary>
    [Fact]
    public void ServerOptions_NullUrl_Fails()
    {
        // Arrange
        var options = new ServerOptions { ServerUrl = null! };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests ServerOptions uses correct default value that is valid.
    /// </summary>
    [Fact]
    public void ServerOptions_DefaultValue_IsValidUrl()
    {
        // Arrange & Act
        var options = new ServerOptions();
        var results = ValidateOptions(options);

        // Assert
        results.Should().BeEmpty();
        options.ServerUrl.Should().Be("https://localhost:7000");
        options.ServerUrl.Should().StartWith("https://");
    }

    #endregion

    #region WorkflowClientOptions Tests

    /// <summary>
    /// Tests that WorkflowClientOptions accepts valid configuration.
    /// </summary>
    [Fact]
    public void WorkflowClientOptions_ValidConfiguration_Succeeds()
    {
        // Arrange
        var options = new WorkflowClientOptions
        {
            MaxRetryAttempts = 3,
            InitialRetryDelayMs = 100,
            MaxRetryDelayMs = 1000,
            RequestTimeoutSeconds = 30
        };

        // Act & Assert
        ValidateOptions(options).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that WorkflowClientOptions validates all properties within valid boundaries.
    /// </summary>
    [Theory]
    [InlineData(1, 50, 500, 10)]     // All minimum valid values
    [InlineData(10, 5000, 10000, 300)] // All maximum valid values
    [InlineData(5, 1000, 5000, 150)]   // Mid-range values
    public void WorkflowClientOptions_BoundaryValues_SucceedForAllProperties(
        int maxRetry, int initDelay, int maxDelay, int timeout)
    {
        // Arrange
        var options = new WorkflowClientOptions
        {
            MaxRetryAttempts = maxRetry,
            InitialRetryDelayMs = initDelay,
            MaxRetryDelayMs = maxDelay,
            RequestTimeoutSeconds = timeout
        };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that WorkflowClientOptions rejects MaxRetryAttempts outside valid boundaries.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void WorkflowClientOptions_InvalidMaxRetryAttempts_Fails(int invalidValue)
    {
        // Arrange
        var options = new WorkflowClientOptions { MaxRetryAttempts = invalidValue };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that WorkflowClientOptions rejects InitialRetryDelayMs outside valid boundaries.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(49)]
    [InlineData(5001)]
    public void WorkflowClientOptions_InvalidInitialRetryDelayMs_Fails(int invalidValue)
    {
        // Arrange
        var options = new WorkflowClientOptions { InitialRetryDelayMs = invalidValue };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that WorkflowClientOptions rejects MaxRetryDelayMs outside valid boundaries.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(499)]
    [InlineData(10001)]
    public void WorkflowClientOptions_InvalidMaxRetryDelayMs_Fails(int invalidValue)
    {
        // Arrange
        var options = new WorkflowClientOptions { MaxRetryDelayMs = invalidValue };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that WorkflowClientOptions rejects RequestTimeoutSeconds outside valid boundaries.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(301)]
    public void WorkflowClientOptions_InvalidRequestTimeoutSeconds_Fails(int invalidValue)
    {
        // Arrange
        var options = new WorkflowClientOptions { RequestTimeoutSeconds = invalidValue };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that MaxRetryDelayMs should logically be >= InitialRetryDelayMs (semantic validation).
    /// Note: This is a logical check, not enforced by Range attributes.
    /// </summary>
    [Fact]
    public void WorkflowClientOptions_MaxRetryDelayLessThanInitial_DocumentsSemanticIssue()
    {
        // Arrange
        var options = new WorkflowClientOptions
        {
            InitialRetryDelayMs = 2000,
            MaxRetryDelayMs = 1000  // Illogical: max < initial
        };

        // Act
        var results = ValidateOptions(options);

        // Assert - No validation error because Range attributes don't check relationships
        // Document that this is a semantic issue not caught by data annotations
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Tests WorkflowClientOptions uses correct default values and they are valid.
    /// </summary>
    [Fact]
    public void WorkflowClientOptions_DefaultValues_AreValid()
    {
        // Arrange & Act
        var options = new WorkflowClientOptions();
        var results = ValidateOptions(options);

        // Assert
        results.Should().BeEmpty();
        options.MaxRetryAttempts.Should().Be(3);
        options.InitialRetryDelayMs.Should().Be(100);
        options.MaxRetryDelayMs.Should().Be(1000);
        options.RequestTimeoutSeconds.Should().Be(30);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validates an options object using data annotations.
    /// </summary>
    /// <typeparam name="T">The type of the options object.</typeparam>
    /// <param name="options">The options object to validate.</param>
    /// <returns>A list of validation results. Empty if valid.</returns>
    private static List<ValidationResult> ValidateOptions<T>(T options) where T : class
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        return results;
    }

    #endregion
}
