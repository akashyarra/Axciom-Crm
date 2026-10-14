namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            string module,
            string action,
            string? entityName = null,
            string? recordId = null,
            string? oldValue = null,
            string? newValue = null,
            string result = "Success",
            string? details = null,
            string? userId = null,
            string? userName = null);
    }
}
