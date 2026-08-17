namespace SalesIntelligence.Api.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class RegisterUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // Superadmin or User
        public string Department { get; set; } = "Sales";
    }

    public class UpdateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string Department { get; set; } = "Sales";
        public bool IsActive { get; set; } = true;
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CompanyRevenuePredictRequest
    {
        // Backward-compatible fields (used by the Revenue Forecast page).
        public float Lag1 { get; set; } = 250000;
        public float Lag2 { get; set; } = 240000;
        public float Lag3 { get; set; } = 230000;
        public float RollingMean { get; set; } = 240000;

        // New lag-revenue-model true features (9 total). Nullable so the ML
        // service can derive them from the fields above when not supplied.
        public float? Lag6 { get; set; }
        public float? Lag12 { get; set; }
        public float? RollingMean3 { get; set; }
        public float? RollingStd3 { get; set; }
        public int? Month { get; set; }
        public int? QuarterNumber { get; set; }
    }

    public class WinRatePredictRequest
    {
        // New classifier inputs (numerical + categorical).
        public float DealValueProposed { get; set; } = 3000f;
        public float Employees { get; set; } = 3000f;
        public float SalesCycleDays { get; set; } = 60f;
        public int? EngageMonth { get; set; }
        public int? EngageQuarter { get; set; }
        public string? Sector { get; set; }
        public string? Product { get; set; }
        public string? RegionalOffice { get; set; }
        public string? OfficeLocation { get; set; }
        public string? SalesAgent { get; set; }

        // Backward-compatible fields (only used to derive engage month/quarter).
        public int Month { get; set; } = 3;
        public int Quarter { get; set; } = 1;
        public int IsQuarterEnd { get; set; } = 1;
    }

    public class EmployeePerformancePredictRequest
    {
        public string SalesAgent { get; set; } = string.Empty;

        // New quarterly-model true features (5 total). Nullable so they can be
        // derived from the backward-compatible fields below when not supplied.
        public float? DealsWorked { get; set; }
        public float? WinRate { get; set; }
        public float? AvgDealSize { get; set; }
        public float? AvgCycleDays { get; set; }
        public float? Revenue { get; set; }

        // Backward-compatible fields.
        public int TotalDealsLag1 { get; set; } = 10;
        public int WonDealsLag1 { get; set; } = 6;
        public int ClosedDealsLag1 { get; set; } = 10;
        public float TotalRevenueLag1 { get; set; } = 80000;
        public float AverageDealValueLag1 { get; set; } = 8000;
        public float AverageSalesCycleLag1 { get; set; } = 30;
        public float WinRateLag1 { get; set; } = 60;
        public float WinRateRolling3 { get; set; } = 58;
        public float RevenueRolling3 { get; set; } = 75000;
        public float DealsRolling3 { get; set; } = 9;
        public int MonthNumber { get; set; } = 3;
        public int Quarter { get; set; } = 1;
        public int Year { get; set; } = 2026;
    }

    public class EmployeeRevenuePredictRequest
    {
        public string SalesAgent { get; set; } = string.Empty;

        // New yearly-model true features (4 total).
        public float? Revenue { get; set; }
        public float? YearsActive { get; set; }
        public float? DealsWorked { get; set; }
        public float? WinRate { get; set; }

        // Backward-compatible fields.
        public float Lag1 { get; set; } = 70000;
        public float Lag2 { get; set; } = 65000;
        public float Lag3 { get; set; } = 60000;
        public float Lag6 { get; set; } = 55000;
        public float Lag12 { get; set; } = 50000;
        public float RollingMean3 { get; set; } = 65000;
        public float RollingMean6 { get; set; } = 60000;
        public float RollingMean12 { get; set; } = 55000;
        public int MonthNumber { get; set; } = 3;
        public int Quarter { get; set; } = 1;
        public int Year { get; set; } = 2026;
    }

    public class DashboardKpiDto
    {
        public string TotalRevenue { get; set; } = "$2.45M";
        public string PredictedRevenue { get; set; } = "$680K";
        public string WinRate { get; set; } = "74.8%";
        public string AiConfidence { get; set; } = "92.4%";
        public List<MonthlyRevenueChartDto> RevenueChart { get; set; } = new();
        public List<MonthlyWinRateChartDto> WinRateChart { get; set; } = new();
    }

    public class MonthlyRevenueChartDto
    {
        public string Month { get; set; } = string.Empty;

        // Null outside a series' own range so the chart can break the line there.
        // The boundary month carries BOTH values, which is what lets the actual and
        // forecast segments join into one continuous line with no gap.
        public decimal? ActualRevenue { get; set; }
        public decimal? PredictedRevenue { get; set; }
        public bool IsForecast { get; set; }
    }

    public class MonthlyWinRateChartDto
    {
        public string Month { get; set; } = string.Empty;
        public double WinRate { get; set; }
    }
}
