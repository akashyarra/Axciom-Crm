using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            ICrmService crmService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _crmService = crmService;
            _userManager = userManager;
        }

        // GET: Reports
        public async Task<IActionResult> Index(string tab = "pipeline")
        {
            var vm = new ReportsIndexViewModel { ActiveTab = tab };

            // 1. Pipeline Report (Stage-wise & Owner-wise)
            var opps = await _crmService.GetScopedOpportunities(User).ToListAsync();
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };

            vm.PipelineByStage = stages.Select(stg =>
            {
                var stageOpps = opps.Where(o => o.Stage.Equals(stg, StringComparison.OrdinalIgnoreCase)).ToList();
                return new PipelineReportItem
                {
                    Stage = stg,
                    Count = stageOpps.Count,
                    TotalAmount = stageOpps.Sum(o => o.Amount),
                    WeightedAmount = stageOpps.Sum(o => o.WeightedAmount)
                };
            }).ToList();

            var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
            vm.PipelineByOwner = users.Select(u =>
            {
                var ownerOpps = opps.Where(o => o.AssignedTo == u.Id).ToList();
                return new OwnerPipelineReportItem
                {
                    OwnerName = u.FullName,
                    OpportunityCount = ownerOpps.Count,
                    TotalAmount = ownerOpps.Sum(o => o.Amount),
                    WeightedAmount = ownerOpps.Sum(o => o.WeightedAmount)
                };
            }).Where(p => p.OpportunityCount > 0).ToList();

            // 2. Conversion Report
            var leads = await _crmService.GetScopedLeads(User).ToListAsync();
            var totalLeads = leads.Count;
            var convertedLeads = leads.Count(l => l.Status == "Converted");
            var wonOpps = opps.Where(o => o.Stage == "Won" || o.Status == "Won").ToList();
            var lostOpps = opps.Where(o => o.Stage == "Lost" || o.Status == "Lost").ToList();
            var decidedOppsCount = wonOpps.Count + lostOpps.Count;

            vm.ConversionStats = new ConversionReportViewModel
            {
                TotalLeads = totalLeads,
                ConvertedLeads = convertedLeads,
                LeadConversionRate = totalLeads > 0 ? Math.Round(((decimal)convertedLeads / totalLeads) * 100m, 1) : 0,
                TotalOpportunities = opps.Count,
                WonOpportunities = wonOpps.Count,
                LostOpportunities = lostOpps.Count,
                OpportunityWinRate = decidedOppsCount > 0 ? Math.Round(((decimal)wonOpps.Count / decidedOppsCount) * 100m, 1) : 0,
                TotalWonRevenue = wonOpps.Sum(o => o.Amount)
            };

            // 3. User Activity Report (Managers & Admins)
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                var activities = await _context.Activities.ToListAsync();
                var followUpsAll = await _context.FollowUps.ToListAsync();
                var allUsers = await _userManager.Users.ToListAsync();

                var activityItems = new List<UserActivityReportItem>();
                foreach (var u in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    activityItems.Add(new UserActivityReportItem
                    {
                        UserName = u.FullName,
                        Role = roles.FirstOrDefault() ?? "SalesExecutive",
                        TotalActivities = activities.Count(a => a.AssignedTo == u.Id),
                        FollowUpsHandled = followUpsAll.Count(f => f.AssignedTo == u.Id),
                        OpportunitiesHandled = opps.Count(o => o.AssignedTo == u.Id),
                        LeadsHandled = leads.Count(l => l.AssignedTo == u.Id)
                    });
                }
                vm.UserActivities = activityItems;
            }

            // 4. Detailed Data for tabular views
            vm.Customers = await _crmService.GetScopedCustomers(User).Take(50).ToListAsync();
            vm.Leads = await _crmService.GetScopedLeads(User).Take(50).ToListAsync();
            vm.Opportunities = opps.Take(50).ToList();
            vm.FollowUps = await _crmService.GetScopedFollowUps(User).Take(50).ToListAsync();
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                vm.AuditLogs = await _context.AuditLogs.OrderByDescending(a => a.Timestamp).Take(50).ToListAsync();
            }

            return View(vm);
        }

        // GET: Reports/ExportCsv
        public async Task<IActionResult> ExportCsv(string type)
        {
            var sb = new StringBuilder();
            string fileName = $"Report_{type}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            if (type.Equals("customers", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("CustomerId,CustomerCode,CustomerName,Email,Phone,CompanyName,Status,CreatedDate");
                var customers = await _crmService.GetScopedCustomers(User).ToListAsync();
                foreach (var c in customers)
                {
                    sb.AppendLine($"\"{c.CustomerId}\",\"{c.CustomerCode}\",\"{EscapeCsv(c.CustomerName)}\",\"{c.Email}\",\"{c.Phone}\",\"{EscapeCsv(c.CompanyName)}\",\"{c.Status}\",\"{c.CreatedDate:yyyy-MM-dd}\"");
                }
            }
            else if (type.Equals("leads", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("LeadId,LeadCode,LeadName,Email,Phone,CompanyName,Source,Status,Priority,ExpectedValue,CreatedDate");
                var leads = await _crmService.GetScopedLeads(User).ToListAsync();
                foreach (var l in leads)
                {
                    sb.AppendLine($"\"{l.LeadId}\",\"{l.LeadCode}\",\"{EscapeCsv(l.LeadName)}\",\"{l.Email}\",\"{l.Phone}\",\"{EscapeCsv(l.CompanyName)}\",\"{l.Source}\",\"{l.Status}\",\"{l.Priority}\",\"{l.ExpectedValue}\",\"{l.CreatedDate:yyyy-MM-dd}\"");
                }
            }
            else if (type.Equals("opportunities", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("OpportunityId,OpportunityName,Amount,Stage,Probability,WeightedAmount,ExpectedCloseDate,Status");
                var opps = await _crmService.GetScopedOpportunities(User).ToListAsync();
                foreach (var o in opps)
                {
                    sb.AppendLine($"\"{o.OpportunityId}\",\"{EscapeCsv(o.OpportunityName)}\",\"{o.Amount}\",\"{o.Stage}\",\"{o.Probability}\",\"{o.WeightedAmount}\",\"{o.ExpectedCloseDate:yyyy-MM-dd}\",\"{o.Status}\"");
                }
            }
            else
            {
                sb.AppendLine("ReportType,ExportDate");
                sb.AppendLine($"{type},{DateTime.UtcNow}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
        }

        private static string EscapeCsv(string? value)
        {
            return (value ?? "").Replace("\"", "\"\"");
        }
    }
}
