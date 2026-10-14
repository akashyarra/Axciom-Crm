using System.Security.Claims;
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
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(
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

        // GET: Opportunities
        public async Task<IActionResult> Index(string? search, string? stage, string? status, int page = 1)
        {
            var query = _crmService.GetScopedOpportunities(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(s) ||
                                         (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(s)) ||
                                         (o.Lead != null && o.Lead.LeadName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            const int pageSize = 10;
            var totalCount = await query.CountAsync();
            var opportunities = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Stage = stage;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(opportunities);
        }

        // GET: Opportunities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var opportunity = await _crmService.GetScopedOpportunities(User)
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .Include(o => o.FollowUps)
                .Include(o => o.Activities)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return NotFound();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "Opportunity" && a.RecordId == opportunity.OpportunityId.ToString())
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();

            ViewBag.AuditLogs = auditLogs;
            return View(opportunity);
        }

        // GET: Opportunities/Create
        public async Task<IActionResult> Create(int? customerId, int? leadId)
        {
            await PopulateDropdowns(customerId, leadId);
            return View(new Opportunity
            {
                CustomerId = customerId,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                AssignedTo = _userManager.GetUserId(User)
            });
        }

        // POST: Opportunities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Opportunity opportunity)
        {
            // Business Validation Rules
            if (opportunity.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            bool isActive = opportunity.Status == "Open" && opportunity.Stage != "Won" && opportunity.Stage != "Lost";
            if (isActive && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                opportunity.CreatedDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(opportunity.AssignedTo))
                {
                    opportunity.AssignedTo = userId;
                }

                if (opportunity.Stage == "Won") opportunity.Status = "Won";
                else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";

                _context.Add(opportunity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "Opportunity",
                    action: "Create",
                    entityName: "Opportunity",
                    recordId: opportunity.OpportunityId.ToString(),
                    newValue: $"Name: {opportunity.OpportunityName}, Stage: {opportunity.Stage}, Amount: {opportunity.Amount}",
                    result: "Success",
                    details: $"Opportunity '{opportunity.OpportunityName}' created."
                );

                TempData["SuccessMessage"] = "Opportunity created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(opportunity);
        }

        // GET: Opportunities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var opportunity = await _crmService.GetScopedOpportunities(User).FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null) return NotFound();

            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(opportunity);
        }

        // POST: Opportunities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Opportunity opportunity)
        {
            if (id != opportunity.OpportunityId) return NotFound();

            var existing = await _crmService.GetScopedOpportunities(User).AsNoTracking().FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (existing == null) return Forbid();

            // Business Validation Rules
            if (opportunity.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            bool isActive = opportunity.Status == "Open" && opportunity.Stage != "Won" && opportunity.Stage != "Lost";
            if (isActive && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                if (opportunity.Stage == "Won") opportunity.Status = "Won";
                else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";

                _context.Update(opportunity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "Opportunity",
                    action: "Update",
                    entityName: "Opportunity",
                    recordId: opportunity.OpportunityId.ToString(),
                    oldValue: $"Stage: {existing.Stage}, Amount: {existing.Amount}, Prob: {existing.Probability}%",
                    newValue: $"Stage: {opportunity.Stage}, Amount: {opportunity.Amount}, Prob: {opportunity.Probability}%",
                    result: "Success",
                    details: $"Opportunity '{opportunity.OpportunityName}' updated."
                );

                TempData["SuccessMessage"] = "Opportunity updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(opportunity);
        }

        // POST: Opportunities/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var opportunity = await _crmService.GetScopedOpportunities(User).FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null) return NotFound();

            _context.Opportunities.Remove(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "Opportunity",
                action: "Delete",
                entityName: "Opportunity",
                recordId: id.ToString(),
                oldValue: $"Name: {opportunity.OpportunityName}",
                result: "Success",
                details: $"Opportunity '{opportunity.OpportunityName}' deleted."
            );

            TempData["SuccessMessage"] = "Opportunity deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(int? customerId = null, int? leadId = null, string? selectedUserId = null)
        {
            var customers = await _crmService.GetScopedCustomers(User).OrderBy(c => c.CustomerName).ToListAsync();
            var leads = await _crmService.GetScopedLeads(User).OrderBy(l => l.LeadName).ToListAsync();
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            ViewBag.CustomerId = new SelectList(customers, "CustomerId", "CustomerName", customerId);
            ViewBag.LeadId = new SelectList(leads, "LeadId", "LeadName", leadId);
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
        }
    }
}
