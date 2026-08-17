using Microsoft.AspNetCore.Identity;

namespace SalesIntelligence.Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = "Sales";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }

    public class Deal
    {
        public int Id { get; set; }
        public string OpportunityId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public decimal ProposedValue { get; set; }
        public decimal Value { get; set; }
        // Account revenue as recorded on THIS deal row. The employee models were
        // trained on the per-row `revenue` column, which varies between rows of the
        // same account, so it cannot be recovered from the Accounts table.
        public decimal AccountRevenue { get; set; }
        public double Probability { get; set; }
        public string Status { get; set; } = "In Progress"; // Won, Lost, In Progress, Negotiation, At Risk
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CloseDate { get; set; }
        public int DealDurationDays { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }

    public class Account
    {
        public int Id { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Employees { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int TotalDeals { get; set; }
    }

    public class Agent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = "Sales";
        public decimal TotalRevenue { get; set; }
        public int TotalDeals { get; set; }
        public int WonDeals { get; set; }
        public double WinRate { get; set; }
        public double PerformanceScore { get; set; }
        public string Status { get; set; } = "Active";
        public string Manager { get; set; } = string.Empty;
        public string RegionalOffice { get; set; } = string.Empty;
    }

    public class PredictionLog
    {
        public int Id { get; set; }
        public string PredictionType { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string RequestedByUsername { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string InputParametersJson { get; set; } = string.Empty;
        public string ResultJson { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = "1.0.0";
    }
}
