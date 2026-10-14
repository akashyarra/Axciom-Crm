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
    [Route("api/followups")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public FollowUpsApiController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        /// <summary>
        /// Get list of authorized follow-ups
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FollowUpDto>>> GetFollowUps([FromQuery] string? status)
        {
            var query = _crmService.GetScopedFollowUps(User);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            var list = await query.OrderBy(f => f.FollowUpDate)
                .Select(f => new FollowUpDto
                {
                    FollowUpId = f.FollowUpId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                    LeadId = f.LeadId,
                    LeadName = f.Lead != null ? f.Lead.LeadName : null,
                    OpportunityId = f.OpportunityId,
                    FollowUpDate = f.FollowUpDate,
                    FollowUpType = f.FollowUpType,
                    Subject = f.Subject,
                    Remarks = f.Remarks,
                    Status = f.Status,
                    AssignedTo = f.AssignedTo,
                    AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : null
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Create a new follow-up
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<FollowUpDto>> CreateFollowUp([FromBody] CreateFollowUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.Today)
            {
                return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                OpportunityId = dto.OpportunityId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = dto.FollowUpType ?? "Call",
                Subject = dto.Subject.Trim(),
                Remarks = dto.Remarks?.Trim(),
                Status = dto.Status ?? "Planned",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = currentUserId
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "API FollowUp",
                action: "Create",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                result: "Success",
                details: $"Follow-up '{followUp.Subject}' created via REST API."
            );

            var resultDto = new FollowUpDto
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Subject = followUp.Subject,
                Remarks = followUp.Remarks,
                Status = followUp.Status,
                AssignedTo = followUp.AssignedTo
            };

            return CreatedAtAction(nameof(GetFollowUps), new { id = followUp.FollowUpId }, resultDto);
        }
    }
}
