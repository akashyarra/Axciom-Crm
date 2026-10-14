using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class LeadConversionViewModel
    {
        public int LeadId { get; set; }

        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        [Display(Name = "Create Related Opportunity?")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Name")]
        public string? OpportunityName { get; set; }

        [Display(Name = "Opportunity Amount")]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal? OpportunityAmount { get; set; }

        [Display(Name = "Expected Close Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
    }
}
