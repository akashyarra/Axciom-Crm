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
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(
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

        // GET: Customers
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            var query = _crmService.GetScopedCustomers(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(s) ||
                                         c.Email.ToLower().Contains(s) ||
                                         c.Phone.Contains(s) ||
                                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)) ||
                                         c.CustomerCode.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            const int pageSize = 10;
            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _crmService.GetScopedCustomers(User)
                .Include(c => c.Owner)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Also load customer-specific audit history
            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "Customer" && a.RecordId == customer.CustomerId.ToString())
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();

            ViewBag.AuditLogs = auditLogs;
            return View(customer);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUserDropdown();
            return View(new Customer
            {
                CustomerCode = $"CUST-{1000 + await _context.Customers.CountAsync() + 1}",
                OwnerId = _userManager.GetUserId(User)
            });
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            // Business & server validations
            var (isDuplicate, dupReason) = await _crmService.CheckDuplicateCustomerAsync(
                customer.Email, customer.Phone, customer.CustomerName, customer.CompanyName);

            if (isDuplicate)
            {
                ModelState.AddModelError(string.Empty, dupReason);
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                customer.CreatedDate = DateTime.UtcNow;
                customer.CreatedBy = userId;
                if (string.IsNullOrEmpty(customer.OwnerId))
                {
                    customer.OwnerId = userId;
                }

                _context.Add(customer);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "Customer",
                    action: "Create",
                    entityName: "Customer",
                    recordId: customer.CustomerId.ToString(),
                    newValue: $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}",
                    result: "Success",
                    details: $"Customer '{customer.CustomerName}' created."
                );

                TempData["SuccessMessage"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUserDropdown(customer.OwnerId);
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _crmService.GetScopedCustomers(User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            await PopulateUserDropdown(customer.OwnerId);
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.CustomerId) return NotFound();

            // Authorization check
            var existing = await _crmService.GetScopedCustomers(User)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (existing == null)
            {
                return Forbid();
            }

            // Duplicate validation excluding self
            var (isDuplicate, dupReason) = await _crmService.CheckDuplicateCustomerAsync(
                customer.Email, customer.Phone, customer.CustomerName, customer.CompanyName, id);

            if (isDuplicate)
            {
                ModelState.AddModelError(string.Empty, dupReason);
            }

            if (ModelState.IsValid)
            {
                customer.ModifiedDate = DateTime.UtcNow;
                _context.Update(customer);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    module: "Customer",
                    action: "Update",
                    entityName: "Customer",
                    recordId: customer.CustomerId.ToString(),
                    oldValue: $"Name: {existing.CustomerName}, Email: {existing.Email}, Status: {existing.Status}",
                    newValue: $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}",
                    result: "Success",
                    details: $"Customer '{customer.CustomerName}' updated."
                );

                TempData["SuccessMessage"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUserDropdown(customer.OwnerId);
            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _crmService.GetScopedCustomers(User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "Customer",
                action: "Delete",
                entityName: "Customer",
                recordId: id.ToString(),
                oldValue: $"Name: {customer.CustomerName}, Email: {customer.Email}",
                result: "Success",
                details: $"Customer '{customer.CustomerName}' deleted."
            );

            TempData["SuccessMessage"] = "Customer deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUserDropdown(string? selectedId = null)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
        }
    }
}
