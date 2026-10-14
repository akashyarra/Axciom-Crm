using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AuditLogs
        public async Task<IActionResult> Index(
            string? user,
            string? module,
            string? actionFilter,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(user))
            {
                var u = user.Trim().ToLower();
                query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(u)) ||
                                         (a.UserId != null && a.UserId.ToLower().Contains(u)));
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(a => a.Module == module);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.Timestamp <= end);
            }

            const int pageSize = 15;
            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.UserFilter = user;
            ViewBag.ModuleFilter = module;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // Available modules and actions for filter dropdowns
            ViewBag.Modules = await _context.AuditLogs.Select(a => a.Module).Distinct().ToListAsync();
            ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();

            return View(logs);
        }
    }
}
