namespace AF.ECT.Tests.Builders;

/// <summary>
/// Enhanced RequestBuilder with fluent API for flexible test request construction.
/// Supports chainable WithXXX() methods and Build() pattern for cleaner test code.
/// </summary>
public class FluentRequestBuilder<T> where T : class, new()
{
    private readonly T _request = new();
    private readonly Dictionary<string, object?> _properties = new();

    /// <summary>
    /// Sets a property value on the request object.
    /// </summary>
    /// <param name="propertyName">The property name to set</param>
    /// <param name="value">The value to set</param>
    /// <returns>This builder for chaining</returns>
    public FluentRequestBuilder<T> With(string propertyName, object? value)
    {
        _properties[propertyName] = value;
        return this;
    }

    /// <summary>
    /// Builds the request object with all configured properties.
    /// </summary>
    /// <returns>The constructed request object</returns>
    public T Build()
    {
        foreach (var kvp in _properties)
        {
            var property = typeof(T).GetProperty(kvp.Key);
            if (property != null && property.CanWrite)
            {
                property.SetValue(_request, kvp.Value);
            }
        }
        return _request;
    }

    /// <summary>
    /// Creates a new fluent builder for the request type.
    /// </summary>
    /// <returns>A new FluentRequestBuilder instance</returns>
    public static FluentRequestBuilder<T> Create() => new();
}

/// <summary>
/// Specialized fluent builder for GetReinvestigationRequestsRequest.
/// </summary>
public class ReinvestigationRequestBuilder
{
    private int? _userId = 1;
    private bool? _sarc = true;

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    public ReinvestigationRequestBuilder WithUserId(int? userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Sets the SARC flag.
    /// </summary>
    public ReinvestigationRequestBuilder WithSarc(bool? sarc)
    {
        _sarc = sarc;
        return this;
    }

    /// <summary>
    /// Builds the request.
    /// </summary>
    public GetReinvestigationRequestsRequest Build() =>
        new() { UserId = _userId ?? 1, Sarc = _sarc ?? true };
}

/// <summary>
/// Specialized fluent builder for GetUserNameRequest.
/// </summary>
public class UserNameRequestBuilder
{
    private string? _firstName = "John";
    private string? _lastName = "Doe";

    /// <summary>
    /// Sets the first name.
    /// </summary>
    public UserNameRequestBuilder WithFirstName(string? firstName)
    {
        _firstName = firstName;
        return this;
    }

    /// <summary>
    /// Sets the last name.
    /// </summary>
    public UserNameRequestBuilder WithLastName(string? lastName)
    {
        _lastName = lastName;
        return this;
    }

    /// <summary>
    /// Builds the request.
    /// </summary>
    public GetUserNameRequest Build() =>
        new() { First = _firstName, Last = _lastName };
}

/// <summary>
/// Specialized fluent builder for GetManagedUsersRequest.
/// </summary>
public class ManagedUsersRequestBuilder
{
    private int? _userId = 1;
    private string? _ssn = "123456789";
    private string? _name = "John Doe";
    private int? _status = 1;
    private int? _role = 1;
    private int? _srchUnit = 1;
    private bool? _showAllUsers = true;

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    public ManagedUsersRequestBuilder WithUserId(int? userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Sets the SSN.
    /// </summary>
    public ManagedUsersRequestBuilder WithSsn(string? ssn)
    {
        _ssn = ssn;
        return this;
    }

    /// <summary>
    /// Sets the name.
    /// </summary>
    public ManagedUsersRequestBuilder WithName(string? name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the status.
    /// </summary>
    public ManagedUsersRequestBuilder WithStatus(int? status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// Sets the role.
    /// </summary>
    public ManagedUsersRequestBuilder WithRole(int? role)
    {
        _role = role;
        return this;
    }

    /// <summary>
    /// Sets the search unit.
    /// </summary>
    public ManagedUsersRequestBuilder WithSrchUnit(int? srchUnit)
    {
        _srchUnit = srchUnit;
        return this;
    }

    /// <summary>
    /// Sets whether to show all users.
    /// </summary>
    public ManagedUsersRequestBuilder WithShowAllUsers(bool? showAllUsers)
    {
        _showAllUsers = showAllUsers;
        return this;
    }

    /// <summary>
    /// Builds the request.
    /// </summary>
    public GetManagedUsersRequest Build() =>
        new()
        {
            Userid = _userId ?? 1,
            Ssn = _ssn,
            Name = _name,
            Status = _status ?? 1,
            Role = _role ?? 1,
            SrchUnit = _srchUnit ?? 1,
            ShowAllUsers = _showAllUsers ?? true
        };
}

/// <summary>
/// Specialized fluent builder for SearchMemberDataRequest.
/// </summary>
public class SearchMemberDataRequestBuilder
{
    private int? _userId = 1;
    private string? _ssn;
    private string? _lastName;
    private string? _firstName;
    private string? _middleName;
    private int? _srchUnit = 1;
    private int? _rptView = 1;

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    public SearchMemberDataRequestBuilder WithUserId(int? userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Sets the SSN.
    /// </summary>
    public SearchMemberDataRequestBuilder WithSsn(string? ssn)
    {
        _ssn = ssn;
        return this;
    }

    /// <summary>
    /// Sets the last name.
    /// </summary>
    public SearchMemberDataRequestBuilder WithLastName(string? lastName)
    {
        _lastName = lastName;
        return this;
    }

    /// <summary>
    /// Sets the first name.
    /// </summary>
    public SearchMemberDataRequestBuilder WithFirstName(string? firstName)
    {
        _firstName = firstName;
        return this;
    }

    /// <summary>
    /// Sets the middle name.
    /// </summary>
    public SearchMemberDataRequestBuilder WithMiddleName(string? middleName)
    {
        _middleName = middleName;
        return this;
    }

    /// <summary>
    /// Sets the search unit.
    /// </summary>
    public SearchMemberDataRequestBuilder WithSrchUnit(int? srchUnit)
    {
        _srchUnit = srchUnit;
        return this;
    }

    /// <summary>
    /// Sets the report view.
    /// </summary>
    public SearchMemberDataRequestBuilder WithRptView(int? rptView)
    {
        _rptView = rptView;
        return this;
    }

    /// <summary>
    /// Builds the request.
    /// </summary>
    public SearchMemberDataRequest Build() =>
        new()
        {
            UserId = _userId ?? 1,
            Ssn = _ssn,
            LastName = _lastName,
            FirstName = _firstName,
            MiddleName = _middleName,
            SrchUnit = _srchUnit ?? 1,
            RptView = _rptView ?? 1
        };
}
