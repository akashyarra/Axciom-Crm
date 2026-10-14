using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class CrmService : ICrmService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CrmService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<(bool isDuplicate, string reason)> CheckDuplicateCustomerAsync(
            string email, string phone, string name, string? company, int? excludeCustomerId = null)
        {
            var query = _context.Customers.AsQueryable();
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            // Check email uniqueness
            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLower();
                var emailExists = await query.AnyAsync(c => c.Email.ToLower() == normalizedEmail);
                if (emailExists)
                {
                    return (true, $"A customer with email '{email}' already exists.");
                }
            }

            // Check phone uniqueness
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var cleanPhone = phone.Trim();
                var phoneExists = await query.AnyAsync(c => c.Phone == cleanPhone);
                if (phoneExists)
                {
                    return (true, $"A customer with phone number '{phone}' already exists.");
                }
            }

            // Check duplicate business identifying combination: Name and Company
            if (!string.IsNullOrWhiteSpace(name))
            {
                var cleanName = name.Trim().ToLower();
                var cleanCompany = company?.Trim().ToLower() ?? "";
                var nameCompanyExists = await query.AnyAsync(c => 
                    c.CustomerName.ToLower() == cleanName && (c.CompanyName ?? "").ToLower() == cleanCompany);
                if (nameCompanyExists)
                {
                    return (true, $"A customer with name '{name}' at company '{company ?? "None"}' already exists.");
                }
            }

            return (false, string.Empty);
        }

        public (bool isValid, string errorMessage) ValidateOpportunity(Opportunity opp)
        {
            if (opp.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.");
            }

            if (opp.Probability < 0 || opp.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.");
            }

            // Expected close date cannot be in the past for active opportunities
            bool isActive = opp.Status == "Open" && opp.Stage != "Won" && opp.Stage != "Lost";
            if (isActive && opp.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past for an active opportunity.");
            }

            return (true, string.Empty);
        }

        public (bool isValid, string errorMessage) ValidateFollowUp(FollowUp followUp, bool isNew = false)
        {
            if (isNew || followUp.Status == "Planned")
            {
                if (followUp.FollowUpDate.Date < DateTime.Today)
                {
                    return (false, "Follow-up date cannot be earlier than today for a new/planned follow-up.");
                }
            }
            return (true, string.Empty);
        }

        public async Task<(bool success, string message, Customer? customer, Opportunity? opportunity)> ConvertLeadAsync(
            int leadId, string? opportunityName, decimal? amount, DateTime? expectedCloseDate, string currentUserId)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null)
            {
                return (false, "Lead not found.", null, null);
            }

            if (lead.Status == "Converted")
            {
                return (false, "This lead has already been converted.", null, null);
            }

            // Check if customer already exists for email/phone
            var (isDup, dupReason) = await CheckDuplicateCustomerAsync(lead.Email, lead.Phone, lead.LeadName, lead.CompanyName);
            if (isDup)
            {
                return (false, $"Cannot convert lead: {dupReason}", null, null);
            }

            // Generate unique CustomerCode
            var count = await _context.Customers.CountAsync();
            var customerCode = $"CUST-{1000 + count + 1}";

            var customer = new Customer
            {
                CustomerCode = customerCode,
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserId,
                OwnerId = lead.AssignedTo ?? currentUserId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            Opportunity? opp = null;
            if (!string.IsNullOrWhiteSpace(opportunityName) || (amount.HasValue && amount.Value > 0))
            {
                var oppAmount = amount.GetValueOrDefault(lead.ExpectedValue > 0 ? lead.ExpectedValue : 1000m);
                var oppCloseDate = expectedCloseDate ?? DateTime.Today.AddDays(30);

                opp = new Opportunity
                {
                    OpportunityName = string.IsNullOrWhiteSpace(opportunityName) ? $"{lead.LeadName} - Opportunity" : opportunityName.Trim(),
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = oppAmount,
                    Stage = "Qualification",
                    Probability = 25,
                    ExpectedCloseDate = oppCloseDate,
                    Status = "Open",
                    Source = lead.Source,
                    Notes = $"Created automatically from Lead conversion (Lead: {lead.LeadCode})",
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = lead.AssignedTo ?? currentUserId
                };

                var (oppValid, oppError) = ValidateOpportunity(opp);
                if (!oppValid)
                {
                    opp.ExpectedCloseDate = DateTime.Today.AddDays(30);
                    if (opp.Amount <= 0) opp.Amount = 500m;
                }

                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();
            }

            // Update lead
            lead.Status = "Converted";
            lead.ConvertedCustomerId = customer.CustomerId;
            if (opp != null) lead.ConvertedOpportunityId = opp.OpportunityId;
            await _context.SaveChangesAsync();

            // Record conversion in audit log
            await _auditService.LogAsync(
                module: "Lead",
                action: "Convert",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: "Status: Qualified",
                newValue: $"Converted to CustomerId: {customer.CustomerId}, OppId: {opp?.OpportunityId}",
                result: "Success",
                details: $"Lead {lead.LeadName} successfully converted to Customer {customer.CustomerName}."
            );

            return (true, "Lead successfully converted to Customer and Opportunity.", customer, opp);
        }

        public IQueryable<Customer> GetScopedCustomers(ClaimsPrincipal user)
        {
            var query = _context.Customers.Include(c => c.Owner).AsQueryable();
            if (user.IsInRole("Admin") || user.IsInRole("Manager"))
            {
                return query;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return query.Where(c => c.OwnerId == userId);
        }

        public IQueryable<Lead> GetScopedLeads(ClaimsPrincipal user)
        {
            var query = _context.Leads.Include(l => l.AssignedUser).AsQueryable();
            if (user.IsInRole("Admin") || user.IsInRole("Manager"))
            {
                return query;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return query.Where(l => l.AssignedTo == userId);
        }

        public IQueryable<Opportunity> GetScopedOpportunities(ClaimsPrincipal user)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            if (user.IsInRole("Admin") || user.IsInRole("Manager"))
            {
                return query;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return query.Where(o => o.AssignedTo == userId);
        }

        public IQueryable<FollowUp> GetScopedFollowUps(ClaimsPrincipal user)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (user.IsInRole("Admin") || user.IsInRole("Manager"))
            {
                return query;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return query.Where(f => f.AssignedTo == userId);
        }

        public IQueryable<Activity> GetScopedActivities(ClaimsPrincipal user)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser)
                .AsQueryable();

            if (user.IsInRole("Admin") || user.IsInRole("Manager"))
            {
                return query;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return query.Where(a => a.AssignedTo == userId);
        }

        public async Task<DashboardViewModel> GetDashboardMetricsAsync(ClaimsPrincipal user, string? dateRange = null)
        {
            var customersQuery = GetScopedCustomers(user);
            var leadsQuery = GetScopedLeads(user);
            var oppsQuery = GetScopedOpportunities(user);
            var followUpsQuery = GetScopedFollowUps(user);
            var activitiesQuery = GetScopedActivities(user);

            // Apply date filters if specified
            DateTime? startDate = null;
            if (dateRange == "Today")
            {
                startDate = DateTime.Today;
            }
            else if (dateRange == "ThisWeek")
            {
                int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = DateTime.Today.AddDays(-1 * diff);
            }
            else if (dateRange == "ThisMonth")
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (startDate.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.CreatedDate >= startDate.Value);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startDate.Value);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate >= startDate.Value);
                activitiesQuery = activitiesQuery.Where(a => a.ActivityDate >= startDate.Value);
            }

            var customersCount = await customersQuery.CountAsync();
            var leads = await leadsQuery.ToListAsync();
            var opps = await oppsQuery.ToListAsync();
            var followUps = await followUpsQuery.ToListAsync();
            var activities = await activitiesQuery.OrderByDescending(a => a.ActivityDate).Take(10).ToListAsync();

            var openLeadsCount = leads.Count(l => l.Status != "Lost" && l.Status != "Converted");
            var openOpps = opps.Where(o => o.Status == "Open" || (o.Stage != "Won" && o.Stage != "Lost")).ToList();
            var wonOpps = opps.Where(o => o.Stage == "Won" || o.Status == "Won").ToList();
            var lostOpps = opps.Where(o => o.Stage == "Lost" || o.Status == "Lost").ToList();

            var totalPipelineValue = openOpps.Sum(o => o.Amount);
            var weightedPipelineValue = openOpps.Sum(o => o.WeightedAmount);

            var pendingFollowUps = followUps.Count(f => f.Status == "Planned");
            var overdueFollowUps = followUps.Count(f => f.Status == "Planned" && f.FollowUpDate.Date < DateTime.Today);

            // Chart 1: Lead Status
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Lost", "Converted" };
            var leadStatusLabels = new List<string>();
            var leadStatusCounts = new List<int>();
            foreach (var status in leadStatuses)
            {
                leadStatusLabels.Add(status);
                leadStatusCounts.Add(leads.Count(l => l.Status.Equals(status, StringComparison.OrdinalIgnoreCase)));
            }

            // Chart 2: Pipeline by Stage
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var pipelineLabels = new List<string>();
            var pipelineCounts = new List<int>();
            var pipelineAmounts = new List<decimal>();
            foreach (var stg in stages)
            {
                pipelineLabels.Add(stg);
                var stageOpps = opps.Where(o => o.Stage.Equals(stg, StringComparison.OrdinalIgnoreCase)).ToList();
                pipelineCounts.Add(stageOpps.Count);
                pipelineAmounts.Add(stageOpps.Sum(o => o.Amount));
            }

            // Chart 3: Monthly Sales (Past 6 Months)
            var monthlyLabels = new List<string>();
            var monthlyValues = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.Today.AddMonths(-i);
                var monthName = monthDate.ToString("MMM yyyy");
                monthlyLabels.Add(monthName);

                var monthWon = wonOpps
                    .Where(o => o.CreatedDate.Year == monthDate.Year && o.CreatedDate.Month == monthDate.Month)
                    .Sum(o => o.Amount);
                monthlyValues.Add(monthWon);
            }

            var vm = new DashboardViewModel
            {
                UserRole = user.IsInRole("Admin") ? "Admin" : user.IsInRole("Manager") ? "Manager" : "SalesExecutive",
                UserName = user.Identity?.Name ?? "User",
                DateRangeFilter = dateRange ?? "All",
                TotalCustomers = customersCount,
                TotalLeads = leads.Count,
                OpenLeads = openLeadsCount,
                TotalOpportunities = opps.Count,
                OpenOpportunities = openOpps.Count,
                WonOpportunities = wonOpps.Count,
                LostOpportunities = lostOpps.Count,
                TotalPipelineValue = totalPipelineValue,
                WeightedPipelineValue = weightedPipelineValue,
                PendingFollowUps = pendingFollowUps,
                OverdueFollowUps = overdueFollowUps,
                TotalActivities = activities.Count,
                LeadStatusLabels = leadStatusLabels,
                LeadStatusCounts = leadStatusCounts,
                PipelineStageLabels = pipelineLabels,
                PipelineStageCounts = pipelineCounts,
                PipelineStageAmounts = pipelineAmounts,
                MonthlySalesLabels = monthlyLabels,
                MonthlySalesValues = monthlyValues,
                RecentActivities = activities.Select(a => new RecentActivityItem
                {
                    Subject = a.Subject,
                    Type = a.ActivityType,
                    Date = a.ActivityDate,
                    Status = a.Status,
                    RelatedName = a.Customer?.CustomerName ?? a.Lead?.LeadName ?? a.Opportunity?.OpportunityName
                }).ToList(),
                UpcomingFollowUps = followUps
                    .Where(f => f.Status == "Planned")
                    .OrderBy(f => f.FollowUpDate)
                    .Take(5)
                    .Select(f => new UpcomingFollowUpItem
                    {
                        FollowUpId = f.FollowUpId,
                        Subject = f.Subject,
                        Type = f.FollowUpType,
                        Date = f.FollowUpDate,
                        RelatedName = f.Customer?.CustomerName ?? f.Lead?.LeadName ?? f.Opportunity?.OpportunityName,
                        IsOverdue = f.IsOverdue
                    }).ToList()
            };

            return vm;
        }
    }
}
