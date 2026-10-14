namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DateRangeFilter { get; set; } = "All"; // Today, ThisWeek, ThisMonth, All

        // KPI Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public int PendingFollowUps { get; set; }
        public int OverdueFollowUps { get; set; }
        public int TotalActivities { get; set; }

        // Chart 1: Lead Status
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusCounts { get; set; } = new();

        // Chart 2: Opportunity Pipeline by Stage
        public List<string> PipelineStageLabels { get; set; } = new();
        public List<int> PipelineStageCounts { get; set; } = new();
        public List<decimal> PipelineStageAmounts { get; set; } = new();

        // Chart 3: Monthly Sales / Won Totals
        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesValues { get; set; } = new();

        // Recent items for quick actions
        public List<RecentActivityItem> RecentActivities { get; set; } = new();
        public List<UpcomingFollowUpItem> UpcomingFollowUps { get; set; } = new();
    }

    public class RecentActivityItem
    {
        public string Subject { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? RelatedName { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UpcomingFollowUpItem
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? RelatedName { get; set; }
        public bool IsOverdue { get; set; }
    }
}
