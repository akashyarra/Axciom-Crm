using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/leads")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public LeadsApiController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        /// <summary>
        /// Get list of authorized leads with search and status filter
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] string? search, [FromQuery] string? status)
        {
            var query = _crmService.GetScopedLeads(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(s) ||
                                         l.Email.ToLower().Contains(s) ||
                                         l.Phone.Contains(s) ||
                                         (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            var list = await query.OrderByDescending(l => l.CreatedDate)
                .Select(l => new LeadDto
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    Priority = l.Priority,
                    ExpectedValue = l.ExpectedValue,
                    Notes = l.Notes,
                    CreatedDate = l.CreatedDate,
                    AssignedTo = l.AssignedTo,
                    AssignedUserName = l.AssignedUser != null ? l.AssignedUser.FullName : null
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Create a new lead
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeadDto>> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.ExpectedValue < 0)
            {
                return BadRequest(new { message = "Expected value cannot be negative." });
            }

            var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
            if (!validStatuses.Contains(dto.Status))
            {
                return BadRequest(new { message = "Invalid lead status." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = await _context.Leads.CountAsync();
            var lead = new Lead
            {
                LeadCode = $"LEAD-{1000 + count + 1}",
                LeadName = dto.LeadName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Source = dto.Source ?? "Website",
                Status = dto.Status ?? "New",
                Priority = dto.Priority ?? "Medium",
                ExpectedValue = dto.ExpectedValue,
                Notes = dto.Notes?.Trim(),
                CreatedDate = DateTime.UtcNow,
                AssignedTo = currentUserId
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "API Lead",
                action: "Create",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                result: "Success",
                details: $"Lead '{lead.LeadName}' created via REST API."
            );

            var resultDto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                Notes = lead.Notes,
                CreatedDate = lead.CreatedDate,
                AssignedTo = lead.AssignedTo
            };

            return CreatedAtAction(nameof(GetLeads), new { id = lead.LeadId }, resultDto);
        }
    }
}
