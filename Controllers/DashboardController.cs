using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ICrmService _crmService;

        public DashboardController(ICrmService crmService)
        {
            _crmService = crmService;
        }

        public async Task<IActionResult> Index(string? dateRange = "All")
        {
            var viewModel = await _crmService.GetDashboardMetricsAsync(User, dateRange);
            return View(viewModel);
        }
    }
}
