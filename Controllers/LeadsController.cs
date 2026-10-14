using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(
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

        // GET: Leads
        public async Task<IActionResult> Index(string? search, string? status, string? priority, int page = 1)
        {
            var query = _crmService.GetScopedLeads(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(s) ||
                                         l.Email.ToLower().Contains(s) ||
                                         l.Phone.Contains(s) ||
                                         (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)) ||
                                         l.LeadCode.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(l => l.Priority == priority);
            }

            const int pageSize = 10;
            var totalCount = await query.CountAsync();
            var leads = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(leads);
        }

        // GET: Leads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var lead = await _crmService.GetScopedLeads(User)
                .Include(l => l.AssignedUser)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null) return NotFound();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "Lead" && a.RecordId == lead.LeadId.ToString())
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();

            ViewBag.AuditLogs = auditLogs;
            return View(lead);
        }

        // GET: Leads/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUserDropdown();
            return View(new Lead
            {
                LeadCode = $"LEAD-{1000 + await _context.Leads.CountAsync() + 1}",
                AssignedTo = _userManager.GetUserId(User)
            });
        }

        // POST: Leads/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lead lead)
        {
            if (lead.ExpectedValue < 0)
            {
                ModelState.AddModelError("ExpectedValue", "Expected value cannot be negative.");
            }

            var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
            if (!validStatuses.Contains(lead.Status))
            {
                ModelState.AddModelError("Status", "Invalid Lead status selected.");
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                lead.CreatedDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(lead.AssignedTo))
                {
                    lead.AssignedTo = userId;
                }

                _context.Add(lead);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "Lead",
                    action: "Create",
                    entityName: "Lead",
                    recordId: lead.LeadId.ToString(),
                    newValue: $"Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}",
                    result: "Success",
                    details: $"Lead '{lead.LeadName}' created."
                );

                TempData["SuccessMessage"] = "Lead created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUserDropdown(lead.AssignedTo);
            return View(lead);
        }

        // GET: Leads/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lead = await _crmService.GetScopedLeads(User).FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            await PopulateUserDropdown(lead.AssignedTo);
            return View(lead);
        }

        // POST: Leads/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lead lead)
        {
            if (id != lead.LeadId) return NotFound();

            var existing = await _crmService.GetScopedLeads(User).AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == id);
            if (existing == null) return Forbid();

            var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
            if (!validStatuses.Contains(lead.Status))
            {
                ModelState.AddModelError("Status", "Invalid Lead status selected.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(lead);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "Lead",
                    action: "Update",
                    entityName: "Lead",
                    recordId: lead.LeadId.ToString(),
                    oldValue: $"Status: {existing.Status}, Value: {existing.ExpectedValue}",
                    newValue: $"Status: {lead.Status}, Value: {lead.ExpectedValue}",
                    result: "Success",
                    details: $"Lead '{lead.LeadName}' updated."
                );

                TempData["SuccessMessage"] = "Lead updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUserDropdown(lead.AssignedTo);
            return View(lead);
        }

        // GET: Leads/Convert/5
        public async Task<IActionResult> Convert(int? id)
        {
            if (id == null) return NotFound();

            var lead = await _crmService.GetScopedLeads(User).FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "This lead is already converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new LeadConversionViewModel
            {
                LeadId = lead.LeadId,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                CreateOpportunity = true,
                OpportunityName = $"{lead.LeadName} - Opportunity",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 5000,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            return View(model);
        }

        // POST: Leads/Convert/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConversionViewModel model)
        {
            var currentUserId = _userManager.GetUserId(User) ?? "";
            var (success, message, customer, opportunity) = await _crmService.ConvertLeadAsync(
                model.LeadId,
                model.CreateOpportunity ? model.OpportunityName : null,
                model.CreateOpportunity ? model.OpportunityAmount : null,
                model.CreateOpportunity ? model.ExpectedCloseDate : null,
                currentUserId
            );

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Lead successfully converted to Customer and Opportunity!";
            return RedirectToAction("Details", "Customers", new { id = customer!.CustomerId });
        }

        // POST: Leads/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lead = await _crmService.GetScopedLeads(User).FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "Lead",
                action: "Delete",
                entityName: "Lead",
                recordId: id.ToString(),
                oldValue: $"Name: {lead.LeadName}",
                result: "Success",
                details: $"Lead '{lead.LeadName}' deleted."
            );

            TempData["SuccessMessage"] = "Lead deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUserDropdown(string? selectedId = null)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
        }
    }
}
