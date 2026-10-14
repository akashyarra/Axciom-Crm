using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface ICrmService
    {
        Task<(bool isDuplicate, string reason)> CheckDuplicateCustomerAsync(string email, string phone, string name, string? company, int? excludeCustomerId = null);
        (bool isValid, string errorMessage) ValidateOpportunity(Opportunity opp);
        (bool isValid, string errorMessage) ValidateFollowUp(FollowUp followUp, bool isNew = false);
        Task<(bool success, string message, Customer? customer, Opportunity? opportunity)> ConvertLeadAsync(int leadId, string? opportunityName, decimal? amount, DateTime? expectedCloseDate, string currentUserId);
        
        IQueryable<Customer> GetScopedCustomers(ClaimsPrincipal user);
        IQueryable<Lead> GetScopedLeads(ClaimsPrincipal user);
        IQueryable<Opportunity> GetScopedOpportunities(ClaimsPrincipal user);
        IQueryable<FollowUp> GetScopedFollowUps(ClaimsPrincipal user);
        IQueryable<Activity> GetScopedActivities(ClaimsPrincipal user);

        Task<DashboardViewModel> GetDashboardMetricsAsync(ClaimsPrincipal user, string? dateRange = null);
    }
}
