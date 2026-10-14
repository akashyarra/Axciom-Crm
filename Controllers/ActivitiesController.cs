using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: Activities
        public async Task<IActionResult> Index(string? type, string? status, int page = 1)
        {
            var query = _crmService.GetScopedActivities(User);

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            const int pageSize = 10;
            var totalCount = await query.CountAsync();
            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Type = type;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(activities);
        }

        // GET: Activities/Create
        public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
        {
            await PopulateDropdowns(customerId, leadId, opportunityId);
            return View(new Activity
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                ActivityDate = DateTime.Now,
                AssignedTo = _userManager.GetUserId(User)
            });
        }

        // POST: Activities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(activity.AssignedTo))
                {
                    activity.AssignedTo = _userManager.GetUserId(User);
                }

                _context.Add(activity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "Activity",
                    action: "Create",
                    entityName: "Activity",
                    recordId: activity.ActivityId.ToString(),
                    result: "Success",
                    details: $"Activity '{activity.Subject}' ({activity.ActivityType}) logged."
                );

                TempData["SuccessMessage"] = "Activity logged successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(activity.CustomerId, activity.LeadId, activity.OpportunityId, activity.AssignedTo);
            return View(activity);
        }

        private async Task PopulateDropdowns(int? customerId = null, int? leadId = null, int? opportunityId = null, string? selectedUserId = null)
        {
            var customers = await _crmService.GetScopedCustomers(User).OrderBy(c => c.CustomerName).ToListAsync();
            var leads = await _crmService.GetScopedLeads(User).OrderBy(l => l.LeadName).ToListAsync();
            var opps = await _crmService.GetScopedOpportunities(User).OrderBy(o => o.OpportunityName).ToListAsync();
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            ViewBag.CustomerId = new SelectList(customers, "CustomerId", "CustomerName", customerId);
            ViewBag.LeadId = new SelectList(leads, "LeadId", "LeadName", leadId);
            ViewBag.OpportunityId = new SelectList(opps, "OpportunityId", "OpportunityName", opportunityId);
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
        }
    }
}
