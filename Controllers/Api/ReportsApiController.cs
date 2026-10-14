using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.DTOs;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ICrmService _crmService;

        public ReportsApiController(ICrmService crmService)
        {
            _crmService = crmService;
        }

        /// <summary>
        /// Get Pipeline Report data (Stage-wise summary and total weighted value)
        /// </summary>
        [HttpGet("pipeline")]
        public async Task<ActionResult<PipelineReportApiDto>> GetPipelineReport()
        {
            var opps = await _crmService.GetScopedOpportunities(User).ToListAsync();
            var openOpps = opps.Where(o => o.Status == "Open" || (o.Stage != "Won" && o.Stage != "Lost")).ToList();

            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var byStage = stages.Select(stg =>
            {
                var stageOpps = opps.Where(o => o.Stage.Equals(stg, StringComparison.OrdinalIgnoreCase)).ToList();
                return new StageSummaryDto
                {
                    Stage = stg,
                    Count = stageOpps.Count,
                    TotalAmount = stageOpps.Sum(o => o.Amount),
                    WeightedAmount = stageOpps.Sum(o => o.WeightedAmount)
                };
            }).ToList();

            var dto = new PipelineReportApiDto
            {
                TotalPipelineValue = openOpps.Sum(o => o.Amount),
                TotalWeightedPipelineValue = openOpps.Sum(o => o.WeightedAmount),
                TotalOpenOpportunities = openOpps.Count,
                ByStage = byStage
            };

            return Ok(dto);
        }
    }
}
