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
    [Route("api/opportunities")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public OpportunitiesApiController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        /// <summary>
        /// Get list of authorized opportunities
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] string? search, [FromQuery] string? stage)
        {
            var query = _crmService.GetScopedOpportunities(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(s) ||
                                         (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            var list = await query.OrderByDescending(o => o.CreatedDate)
                .Select(o => new OpportunityDto
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                    LeadId = o.LeadId,
                    Amount = o.Amount,
                    Stage = o.Stage,
                    Probability = o.Probability,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    Source = o.Source,
                    Notes = o.Notes,
                    AssignedTo = o.AssignedTo,
                    AssignedUserName = o.AssignedUser != null ? o.AssignedUser.FullName : null,
                    CreatedDate = o.CreatedDate
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Create a new opportunity
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.Amount <= 0)
            {
                return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(new { message = "Probability must be between 0 and 100." });
            }

            bool isActive = dto.Status == "Open" && dto.Stage != "Won" && dto.Stage != "Lost";
            if (isActive && dto.ExpectedCloseDate.Date < DateTime.Today)
            {
                return BadRequest(new { message = "Expected Close Date cannot be in the past." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName.Trim(),
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage ?? "Qualification",
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = dto.Status ?? "Open",
                Source = dto.Source,
                Notes = dto.Notes,
                CreatedDate = DateTime.UtcNow,
                AssignedTo = currentUserId
            };

            if (opp.Stage == "Won") opp.Status = "Won";
            else if (opp.Stage == "Lost") opp.Status = "Lost";

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "API Opportunity",
                action: "Create",
                entityName: "Opportunity",
                recordId: opp.OpportunityId.ToString(),
                result: "Success",
                details: $"Opportunity '{opp.OpportunityName}' created via REST API."
            );

            var resultDto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                Source = opp.Source,
                Notes = opp.Notes,
                CreatedDate = opp.CreatedDate,
                AssignedTo = opp.AssignedTo
            };

            return CreatedAtAction(nameof(GetOpportunities), new { id = opp.OpportunityId }, resultDto);
        }
    }
}
