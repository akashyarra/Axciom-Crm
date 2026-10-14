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
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
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

        // GET: FollowUps
        public async Task<IActionResult> Index(string? status, string? type, string? viewType = "all", int page = 1)
        {
            var query = _crmService.GetScopedFollowUps(User);

            if (viewType == "overdue")
            {
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate.Date < DateTime.Today);
            }
            else if (viewType == "upcoming")
            {
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate.Date >= DateTime.Today);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.FollowUpType == type);
            }

            const int pageSize = 10;
            var totalCount = await query.CountAsync();
            var followUps = await query
                .OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Type = type;
            ViewBag.ViewType = viewType;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(followUps);
        }

        // GET: FollowUps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var followUp = await _crmService.GetScopedFollowUps(User).FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return NotFound();

            return View(followUp);
        }

        // GET: FollowUps/Create
        public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
        {
            await PopulateDropdowns(customerId, leadId, opportunityId);
            return View(new FollowUp
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                FollowUpDate = DateTime.Today.AddDays(1),
                AssignedTo = _userManager.GetUserId(User)
            });
        }

        // POST: FollowUps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUp followUp)
        {
            // Business rule: Follow-up date cannot be earlier than today for a new/planned follow-up
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                followUp.CreatedDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(followUp.AssignedTo))
                {
                    followUp.AssignedTo = userId;
                }

                _context.Add(followUp);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "FollowUp",
                    action: "Create",
                    entityName: "FollowUp",
                    recordId: followUp.FollowUpId.ToString(),
                    newValue: $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:d}, Type: {followUp.FollowUpType}",
                    result: "Success",
                    details: $"Follow-up '{followUp.Subject}' scheduled."
                );

                TempData["SuccessMessage"] = "Follow-up scheduled successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
            return View(followUp);
        }

        // GET: FollowUps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var followUp = await _crmService.GetScopedFollowUps(User).FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return NotFound();

            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
            return View(followUp);
        }

        // POST: FollowUps/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FollowUp followUp)
        {
            if (id != followUp.FollowUpId) return NotFound();

            var existing = await _crmService.GetScopedFollowUps(User).AsNoTracking().FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (existing == null) return Forbid();

            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(followUp);
                await _context.SaveChangesAsync();

                var isRescheduled = existing.FollowUpDate.Date != followUp.FollowUpDate.Date;
                var actionName = isRescheduled ? "Reschedule" : "Update";

                await _auditService.LogAsync(
                    module: "FollowUp",
                    action: actionName,
                    entityName: "FollowUp",
                    recordId: followUp.FollowUpId.ToString(),
                    oldValue: $"Date: {existing.FollowUpDate:d}, Status: {existing.Status}",
                    newValue: $"Date: {followUp.FollowUpDate:d}, Status: {followUp.Status}",
                    result: "Success",
                    details: $"Follow-up '{followUp.Subject}' {actionName.ToLower()}d."
                );

                TempData["SuccessMessage"] = $"Follow-up {actionName.ToLower()}d successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
            return View(followUp);
        }

        // POST: FollowUps/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? completionRemarks)
        {
            var followUp = await _crmService.GetScopedFollowUps(User).FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return NotFound();

            followUp.Status = "Completed";
            followUp.CompletedDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(completionRemarks))
            {
                followUp.Remarks = (followUp.Remarks != null ? followUp.Remarks + "\n" : "") + $"Completed: {completionRemarks}";
            }

            // Create corresponding Activity record
            var activity = new Activity
            {
                ActivityType = followUp.FollowUpType,
                Subject = $"Completed: {followUp.Subject}",
                Description = followUp.Remarks ?? "Follow-up completed.",
                ActivityDate = DateTime.UtcNow,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                AssignedTo = followUp.AssignedTo,
                Status = "Completed"
            };
            _context.Activities.Add(activity);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "FollowUp",
                action: "Complete",
                entityName: "FollowUp",
                recordId: id.ToString(),
                result: "Success",
                details: $"Follow-up '{followUp.Subject}' marked completed."
            );

            TempData["SuccessMessage"] = "Follow-up marked as Completed and logged to Activities!";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUps/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var followUp = await _crmService.GetScopedFollowUps(User).FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return NotFound();

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "FollowUp",
                action: "Delete",
                entityName: "FollowUp",
                recordId: id.ToString(),
                oldValue: $"Subject: {followUp.Subject}",
                result: "Success",
                details: $"Follow-up '{followUp.Subject}' deleted."
            );

            TempData["SuccessMessage"] = "Follow-up deleted successfully!";
            return RedirectToAction(nameof(Index));
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
