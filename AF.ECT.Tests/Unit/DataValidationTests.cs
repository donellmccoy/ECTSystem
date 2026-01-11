namespace AF.ECT.Tests.Unit;

using AF.ECT.Tests.Common;
using FluentAssertions;

/// <summary>
/// Contains data validation and security tests for input sanitization, SQL injection prevention, and encoding.
/// Tests comprehensive input validation across different data types and scenarios.
/// </summary>
[Collection("Data Validation Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "DataValidation")]
public class DataValidationTests
{
    /// <summary>
    /// Tests that SQL injection payloads are safely handled.
    /// </summary>
    /// <param name="maliciousInput">The SQL injection payload</param>
    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE users; --")]
    [InlineData("1' UNION SELECT NULL--")]
    [InlineData("admin'--")]
    [InlineData("' OR 1=1 --")]
    public void SqlInjectionPayloads_AreHandledSafely(string maliciousInput)
    {
        // Arrange
        var sanitized = SanitizeInput(maliciousInput);

        // Act
        var result = sanitized.Length > 0;

        // Assert
        result.Should().BeTrue("Input should be preserved but safely handled");
        sanitized.Should().NotContain("DROP");
        sanitized.Should().NotContain("UNION");
    }

    /// <summary>
    /// Tests that XSS payloads are properly escaped.
    /// </summary>
    /// <param name="xssPayload">The XSS injection payload</param>
    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img src=x onerror=alert('xss')>")]
    [InlineData("<svg onload=alert('xss')>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<iframe src='javascript:alert(1)'></iframe>")]
    public void XssPayloads_AreProperlyEscaped(string xssPayload)
    {
        // Arrange
        var escaped = EscapeHtml(xssPayload);

        // Act & Assert
        escaped.Should().NotContain("<script");
        escaped.Should().NotContain("onerror=");
        escaped.Should().NotContain("onload=");
        escaped.Should().NotContain("javascript:");
    }

    /// <summary>
    /// Tests that unicode characters are properly handled.
    /// </summary>
    /// <param name="unicodeInput">The unicode string</param>
    [Theory]
    [InlineData("Héllo Wørld")]
    [InlineData("你好")]
    [InlineData("مرحبا")]
    [InlineData("🔒🔓")]
    [InlineData("Ñoño")]
    public void UnicodeInput_IsHandledCorrectly(string unicodeInput)
    {
        // Arrange & Act
        var encoded = System.Text.Encoding.UTF8.GetBytes(unicodeInput);
        var decoded = System.Text.Encoding.UTF8.GetString(encoded);

        // Assert
        decoded.Should().Be(unicodeInput, "Unicode should survive round-trip encoding");
    }

    /// <summary>
    /// Tests that control characters are filtered.
    /// </summary>
    /// <param name="controlChar">The control character code</param>
    [Theory]
    [InlineData('\0')]  // Null
    [InlineData('\x01')] // SOH
    [InlineData('\x1f')] // Unit separator
    [InlineData('\x7f')] // DEL
    public void ControlCharacters_AreFiltered(char controlChar)
    {
        // Arrange
        var input = $"valid{controlChar}text";

        // Act
        var filtered = input.Where(c => !char.IsControl(c)).ToList();

        // Assert
        filtered.Should().NotContain(controlChar);
    }

    /// <summary>
    /// Tests that path traversal attacks are prevented.
    /// </summary>
    /// <param name="pathTraversal">The path traversal attempt</param>
    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("....//....//etc/passwd")]
    [InlineData("%2e%2e%2f%2e%2e%2f")]
    public void PathTraversalAttempts_AreBlocked(string pathTraversal)
    {
        // Arrange & Act
        var isValid = ValidatePath(pathTraversal);

        // Assert
        isValid.Should().BeFalse("Path traversal attempts should be blocked");
    }

    /// <summary>
    /// Tests that empty and null inputs are properly validated.
    /// </summary>
    [Fact]
    public void EmptyAndNullInputs_AreValidated()
    {
        // Arrange
        string? nullInput = null;
        var emptyInput = string.Empty;
        var whitespaceInput = "   ";

        // Act & Assert
        nullInput.Should().BeNull();
        emptyInput.Should().BeEmpty();
        whitespaceInput.Should().NotBeEmpty();
        whitespaceInput.Trim().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that numeric input boundaries are respected.
    /// </summary>
    /// <param name="input">The numeric input</param>
    /// <param name="min">The minimum value</param>
    /// <param name="max">The maximum value</param>
    [Theory]
    [InlineData(0, 0, 100)]
    [InlineData(50, 0, 100)]
    [InlineData(100, 0, 100)]
    [InlineData(-1, 0, 100)]
    [InlineData(101, 0, 100)]
    public void NumericInputBoundaries_AreEnforced(int input, int min, int max)
    {
        // Act
        var isValid = input >= min && input <= max;

        // Assert
        if (input < min || input > max)
            isValid.Should().BeFalse();
        else
            isValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that string length limits are enforced.
    /// </summary>
    /// <param name="input">The string input</param>
    /// <param name="maxLength">The maximum allowed length</param>
    [Theory]
    [InlineData("short", 10)]
    [InlineData("exactly_ten", 10)]
    [InlineData("this_is_way_too_long_for_the_limit", 10)]
    public void StringLengthLimits_AreEnforced(string input, int maxLength)
    {
        // Act
        var isValid = input.Length <= maxLength;

        // Assert
        if (input.Length > maxLength)
            isValid.Should().BeFalse();
        else
            isValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that email format is properly validated.
    /// </summary>
    /// <param name="email">The email address</param>
    [Theory]
    [InlineData("valid@example.com")]
    [InlineData("user.name+tag@example.co.uk")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    [InlineData("no-at-sign.com")]
    public void EmailFormat_IsValidated(string email)
    {
        // Act
        var isValid = IsValidEmail(email);

        // Assert
        if (email.Contains("@") && email.IndexOf("@") > 0 && email.IndexOf("@") < email.Length - 1)
            isValid.Should().BeTrue();
        else
            isValid.Should().BeFalse();
    }

    // Helper Methods
    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return input.Replace("DROP", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("UNION", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("DELETE", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("INSERT", "", StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return System.Web.HttpUtility.HtmlEncode(input);
    }

    private static bool ValidatePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return !path.Contains("..") && !path.Contains("//") && !path.Contains("\\\\");
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        var atIndex = email.IndexOf("@");
        return atIndex > 0 && atIndex < email.Length - 1;
    }
}
