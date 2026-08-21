namespace Api.Infrastructure.Context
{
    public interface IApiContext
    {
        string CurrentUserId { get; }
        string Culture { get; }

        /// <summary>
        /// True when the gateway attributed this request to an internal staff account. The gateway
        /// strips the header from anything inbound before stamping it, so it cannot be spoofed by a
        /// client going through it.
        /// </summary>
        bool IsStaff { get; }
    }

    public class ApiContext : IApiContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string CurrentUserId => _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("x-user-id", out var userId) ? userId.ToString() : throw new Exception("User id not found");
        public string Culture => _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("x-culture", out var culture) ? culture.ToString() : "en-US";

        public bool IsStaff =>
            _httpContextAccessor.HttpContext is { } context
            && context.Request.Headers.TryGetValue("x-user-type", out var userType)
            && string.Equals(userType.ToString(), "Staff", StringComparison.OrdinalIgnoreCase);
    }
}