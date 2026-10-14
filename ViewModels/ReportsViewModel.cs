using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class PipelineReportItem
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class OwnerPipelineReportItem
    {
        public string OwnerName { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportViewModel
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public decimal LeadConversionRate { get; set; }
        public int TotalOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal OpportunityWinRate { get; set; }
        public decimal TotalWonRevenue { get; set; }
    }

    public class UserActivityReportItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public int FollowUpsHandled { get; set; }
        public int OpportunitiesHandled { get; set; }
        public int LeadsHandled { get; set; }
    }

    public class ReportsIndexViewModel
    {
        public string ActiveTab { get; set; } = "pipeline";
        public List<PipelineReportItem> PipelineByStage { get; set; } = new();
        public List<OwnerPipelineReportItem> PipelineByOwner { get; set; } = new();
        public ConversionReportViewModel ConversionStats { get; set; } = new();
        public List<UserActivityReportItem> UserActivities { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Lead> Leads { get; set; } = new();
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<AuditLog> AuditLogs { get; set; } = new();
    }
}
