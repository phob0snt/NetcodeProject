namespace Unity.Services.Relay
{
    /// <summary>
    /// Enumerates the known error causes when communicating with the Relay Service.
    /// </summary>
    public enum RelayExceptionReason
    {
        /// <summary>
        /// Start of the range of error codes addressable by the Relay Allocations Service.
        /// </summary>
        Min = 15000,
        /// <summary>
        /// Default value of the enum. No error detected.
        /// </summary>
        NoError = 15000,
        /// <summary>
        /// InvalidRequest is returned when the request is not in a valid format.
        /// </summary>
        InvalidRequest = 15001,
        /// <summary>
        /// InactiveProject is returned when the Unity project is inactive or not found.
        /// Make sure it is active and assigned to your project and you enabled the Relay Service using the Unity Dashboard for that project.
        /// </summary>
        InactiveProject = 15006,
        /// <summary>
        /// RegionNotFound is returned when the region is not found in the current catalog.
        /// </summary>
        RegionNotFound = 15007,
        /// <summary>
        /// AllocationNotFound is returned when the allocation is not found possibly due to a timeout.
        /// To prevent such an unintended timeout, the game client should send periodic ping messages to the Relay server over UDP to keep the connection alive.
        /// </summary>
        AllocationNotFound = 15008,
        /// <summary>
        /// JoinCodeNotFound is returned when the join code is not found. Try getting a new join code.
        /// </summary>
        JoinCodeNotFound = 15009,
        /// <summary>
        /// RelaySelectionFailed is returned when no suitable relay could be selected during allocation. Try again later.
        /// </summary>
        NoSuitableRelay = 15010,

        /// <summary>
        /// The Relay Service could not understand the request due to an invalid value or syntax.
        /// </summary>
        InvalidArgument = 15400,
        /// <summary>
        /// The Relay Service could not determine the user identity.
        /// </summary>
        Unauthorized = 15401,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        PaymentRequired = 15402,
        /// <summary>
        /// The user does not have permission to access the requested resource.
        /// </summary>
        Forbidden = 15403,
        /// <summary>
        /// The requested entity (allocation, join code or region) does not exist.
        /// </summary>
        EntityNotFound = 15404,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        MethodNotAllowed = 15405,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        NotAcceptable = 15406,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        ProxyAuthenticationRequired = 15407,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        RequestTimeOut = 15408,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        Conflict = 15409,
        /// <summary>
        /// The requested resource has been permenantly deleted from the server.
        /// </summary>
        Gone = 15410,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        LengthRequired = 15411,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        PreconditionFailed = 15412,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        RequestEntityTooLarge = 15413,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        RequestUriTooLong = 15414,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        UnsupportedMediaType = 15415,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        RangeNotSatisfiable = 15416,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        ExpectationFailed = 15417,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        Teapot = 15418,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        Misdirected = 15421,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        UnprocessableTransaction = 15422,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        Locked = 15423,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        FailedDependency = 15424,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        TooEarly = 15425,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        UpgradeRequired = 15426,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        PreconditionRequired = 15428,
        /// <summary>
        /// The user has sent too many requests in a given amount of time and is now rate limited.
        /// </summary>
        RateLimited = 15429,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        RequestHeaderFieldsTooLarge = 15431,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        UnavailableForLegalReasons = 15451,
        /// <summary>
        /// The Relay Service has encountered a situation it doesn't know how to handle.
        /// </summary>
        InternalServerError = 15500,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        NotImplemented = 15501,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        BadGateway = 15502,
        /// <summary>
        /// The Relay Service is not ready to handle the request. Common causes are a server that is down for maintenance or that is overloaded. Try again later.
        /// </summary>
        ServiceUnavailable = 15503,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        GatewayTimeout = 15504,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        HttpVersionNotSupported = 15505,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        VariantAlsoNegotiates = 15506,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        InsufficientStorage = 15507,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        LoopDetected = 15508,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        NotExtended = 15510,
        /// <summary>
        /// Same as the HTTP Status.
        /// </summary>
        NetworkAuthenticationRequired = 15511,

        /// <summary>
        /// NetworkError is returned when the Allocation is unable to connect to the service due to a network error like when TLS Negotiation fails.
        /// </summary>
        NetworkError = 15998,
        /// <summary>
        /// Unknown is returned when a unrecognized error code is returned by the service. Check the inner exception to get more information.
        /// </summary>
        Unknown = 15999,
        /// <summary>
        /// End of the range of error codes addressable by the Relay Allocations Service.
        /// </summary>
        Max = 15999
    }
}
