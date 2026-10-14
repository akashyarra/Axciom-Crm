using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
        public string? OwnerId { get; set; }
        public string? OwnerName { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(100)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
    }

    public class CreateLeadDto
    {
        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(100)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public string? Notes { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }

        [Required]
        [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; }

        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public string? Source { get; set; }
        public string? Notes { get; set; }
        public decimal WeightedAmount => (Amount * Probability) / 100m;
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
        public string Status { get; set; } = "Open";
        public string? Source { get; set; }
        public string? Notes { get; set; }
    }

    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public int? OpportunityId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = "Call";
        public string Subject { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned";
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
    }

    public class CreateFollowUpDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        [Required]
        public string FollowUpType { get; set; } = "Call";

        [Required]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned";
    }

    public class PipelineReportApiDto
    {
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedPipelineValue { get; set; }
        public int TotalOpenOpportunities { get; set; }
        public List<StageSummaryDto> ByStage { get; set; } = new();
    }

    public class StageSummaryDto
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }
}
