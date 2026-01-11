using AF.ECT.Data.ResultTypes;

namespace AF.ECT.Tests.Infrastructure;

/// <summary>
/// Provides fluent builder patterns for creating test data objects.
/// Reduces duplication in unit tests by centralizing object construction logic.
/// </summary>
public static class TestDataBuilders
{
    /// <summary>
    /// Creates a builder for constructing GetReinvestigationRequestsRequest objects.
    /// </summary>
    public static ReinvestigationRequestBuilder BuildReinvestigationRequest() => new();

    /// <summary>
    /// Creates a builder for constructing GetManagedUsersRequest objects.
    /// </summary>
    public static ManagedUsersRequestBuilder BuildManagedUsersRequest() => new();

    /// <summary>
    /// Fluent builder for ReinvestigationRequest objects.
    /// </summary>
    public class ReinvestigationRequestBuilder
    {
        private int _userId = 1;
        private bool _sarc = true;

        /// <summary>
        /// Sets the user ID for the request.
        /// </summary>
        public ReinvestigationRequestBuilder WithUserId(int userId)
        {
            _userId = userId;
            return this;
        }

        /// <summary>
        /// Sets the SARC flag for the request.
        /// </summary>
        public ReinvestigationRequestBuilder WithSarc(bool sarc)
        {
            _sarc = sarc;
            return this;
        }

        /// <summary>
        /// Builds the final GetReinvestigationRequestsRequest object.
        /// </summary>
        public GetReinvestigationRequestsRequest Build() => new() { UserId = _userId, Sarc = _sarc };
    }

    /// <summary>
    /// Fluent builder for ManagedUsersRequest objects.
    /// </summary>
    public class ManagedUsersRequestBuilder
    {
        private int _userid = 1;
        private string _ssn = "123456789";
        private string _name = "John Doe";
        private int _status = 1;
        private int _role = 1;
        private int _srchUnit = 1;
        private bool _showAllUsers = true;

        /// <summary>
        /// Sets the user ID.
        /// </summary>
        public ManagedUsersRequestBuilder WithUserId(int userid)
        {
            _userid = userid;
            return this;
        }

        /// <summary>
        /// Sets the SSN.
        /// </summary>
        public ManagedUsersRequestBuilder WithSsn(string ssn)
        {
            _ssn = ssn;
            return this;
        }

        /// <summary>
        /// Sets the name.
        /// </summary>
        public ManagedUsersRequestBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>
        /// Sets the status.
        /// </summary>
        public ManagedUsersRequestBuilder WithStatus(int status)
        {
            _status = status;
            return this;
        }

        /// <summary>
        /// Sets the role.
        /// </summary>
        public ManagedUsersRequestBuilder WithRole(int role)
        {
            _role = role;
            return this;
        }

        /// <summary>
        /// Sets the search unit.
        /// </summary>
        public ManagedUsersRequestBuilder WithSrchUnit(int srchUnit)
        {
            _srchUnit = srchUnit;
            return this;
        }

        /// <summary>
        /// Sets whether to show all users.
        /// </summary>
        public ManagedUsersRequestBuilder WithShowAllUsers(bool showAllUsers)
        {
            _showAllUsers = showAllUsers;
            return this;
        }

        /// <summary>
        /// Builds the final GetManagedUsersRequest object.
        /// </summary>
        public GetManagedUsersRequest Build() => new()
        {
            Userid = _userid,
            Ssn = _ssn,
            Name = _name,
            Status = _status,
            Role = _role,
            SrchUnit = _srchUnit,
            ShowAllUsers = _showAllUsers
        };
    }
}

/// <summary>
/// Provides builders for creating test data result objects returned from data service.
/// </summary>
public static class TestResultBuilders
{
    /// <summary>
    /// Creates a builder for constructing core_lod_sp_GetReinvestigationRequestsResult objects.
    /// </summary>
    public static ReinvestigationResultBuilder BuildReinvestigationResult() => new();

    /// <summary>
    /// Creates a builder for constructing core_user_sp_GetManagedUsersResult objects.
    /// </summary>
    public static ManagedUsersResultBuilder BuildManagedUsersResult() => new();

    /// <summary>
    /// Fluent builder for ReinvestigationResult objects.
    /// </summary>
    public class ReinvestigationResultBuilder
    {
        private int _requestId = 1;
        private string _memberName = "John Doe";
        private string _caseId = "CASE001";
        private string _status = "Active";
        private string _unitName = "ALOD";

        /// <summary>
        /// Sets the request ID.
        /// </summary>
        public ReinvestigationResultBuilder WithRequestId(int requestId)
        {
            _requestId = requestId;
            return this;
        }

        /// <summary>
        /// Sets the member name.
        /// </summary>
        public ReinvestigationResultBuilder WithMemberName(string memberName)
        {
            _memberName = memberName;
            return this;
        }

        /// <summary>
        /// Sets the case ID.
        /// </summary>
        public ReinvestigationResultBuilder WithCaseId(string caseId)
        {
            _caseId = caseId;
            return this;
        }

        /// <summary>
        /// Sets the status.
        /// </summary>
        public ReinvestigationResultBuilder WithStatus(string status)
        {
            _status = status;
            return this;
        }

        /// <summary>
        /// Sets the unit name.
        /// </summary>
        public ReinvestigationResultBuilder WithUnitName(string unitName)
        {
            _unitName = unitName;
            return this;
        }

        /// <summary>
        /// Builds the final core_lod_sp_GetReinvestigationRequestsResult object.
        /// </summary>
        public core_lod_sp_GetReinvestigationRequestsResult Build() => new()
        {
            request_id = _requestId,
            Member_Name = _memberName,
            Case_Id = _caseId,
            Status = _status,
            Unit_Name = _unitName,
            Receive_Date = DateTime.UtcNow.ToString("yyyy-MM-dd")
        };
    }

    /// <summary>
    /// Fluent builder for ManagedUsersResult objects.
    /// </summary>
    public class ManagedUsersResultBuilder
    {
        private int _id = 1;
        private string _firstName = "John";
        private string _lastName = "Doe";
        private byte _status = 1;
        private string _roleName = "User";
        private DateTime _expirationDate = DateTime.UtcNow;
        private byte _groupId = 1;

        /// <summary>
        /// Sets the user ID.
        /// </summary>
        public ManagedUsersResultBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        /// <summary>
        /// Sets the first name.
        /// </summary>
        public ManagedUsersResultBuilder WithFirstName(string firstName)
        {
            _firstName = firstName;
            return this;
        }

        /// <summary>
        /// Sets the last name.
        /// </summary>
        public ManagedUsersResultBuilder WithLastName(string lastName)
        {
            _lastName = lastName;
            return this;
        }

        /// <summary>
        /// Sets the status.
        /// </summary>
        public ManagedUsersResultBuilder WithStatus(byte status)
        {
            _status = status;
            return this;
        }

        /// <summary>
        /// Sets the role name.
        /// </summary>
        public ManagedUsersResultBuilder WithRoleName(string roleName)
        {
            _roleName = roleName;
            return this;
        }

        /// <summary>
        /// Sets the expiration date.
        /// </summary>
        public ManagedUsersResultBuilder WithExpirationDate(DateTime expirationDate)
        {
            _expirationDate = expirationDate;
            return this;
        }

        /// <summary>
        /// Sets the group ID.
        /// </summary>
        public ManagedUsersResultBuilder WithGroupId(byte groupId)
        {
            _groupId = groupId;
            return this;
        }

        /// <summary>
        /// Builds the final core_user_sp_GetManagedUsersResult object.
        /// </summary>
        public core_user_sp_GetManagedUsersResult Build() => new()
        {
            Id = _id,
            FirstName = _firstName,
            LastName = _lastName,
            Status = _status,
            RoleName = _roleName,
            expirationDate = _expirationDate,
            GroupId = _groupId
        };
    }
}

/// <summary>
/// Provides mock data factory methods for creating test scenarios.
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a list of mock reinvestigation requests.
    /// </summary>
    public static List<core_lod_sp_GetReinvestigationRequestsResult> CreateMockReinvestigationRequests(int count)
    {
        var requests = new List<core_lod_sp_GetReinvestigationRequestsResult>();
        for (int i = 1; i <= count; i++)
        {
            requests.Add(TestResultBuilders.BuildReinvestigationResult()
                .WithRequestId(i)
                .WithMemberName($"Member {i}")
                .WithCaseId($"CASE{i:D3}")
                .WithStatus(i % 2 == 0 ? "Active" : "Pending")
                .Build());
        }
        return requests;
    }

    /// <summary>
    /// Creates a list of mock managed users.
    /// </summary>
    public static List<core_user_sp_GetManagedUsersResult> CreateMockManagedUsers(int count)
    {
        var users = new List<core_user_sp_GetManagedUsersResult>();
        for (int i = 1; i <= count; i++)
        {
            users.Add(TestResultBuilders.BuildManagedUsersResult()
                .WithId(i)
                .WithFirstName($"User{i}")
                .WithLastName($"Last{i}")
                .WithStatus(1)
                .WithRoleName(i % 3 == 0 ? "Admin" : "User")
                .Build());
        }
        return users;
    }

    /// <summary>
    /// Creates a mock reinvestigation request with specified parameters.
    /// </summary>
    public static core_lod_sp_GetReinvestigationRequestsResult CreateMockReinvestigationRequest(
        int requestId, string memberName, string caseId, string status)
    {
        return TestResultBuilders.BuildReinvestigationResult()
            .WithRequestId(requestId)
            .WithMemberName(memberName)
            .WithCaseId(caseId)
            .WithStatus(status)
            .Build();
    }
}
