using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string module,
            string action,
            string? entityName = null,
            string? recordId = null,
            string? oldValue = null,
            string? newValue = null,
            string result = "Success",
            string? details = null,
            string? userId = null,
            string? userName = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var currentUserId = userId ?? httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserName = userName ?? httpContext?.User?.Identity?.Name;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var auditEntry = new AuditLog
            {
                UserId = currentUserId,
                UserName = currentUserName,
                Module = module,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                Result = result,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditEntry);
            await _context.SaveChangesAsync();
        }
    }
}
