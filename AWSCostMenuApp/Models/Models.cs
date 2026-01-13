namespace AWSCostMenuApp.Models;

public record DailyCost(
    DateOnly Date,
    string AccountId,
    string AccountName,
    string Service,
    decimal Cost,
    string Currency
);

public record CostComparison(
    string Label,
    decimal CurrentPeriod,
    decimal PreviousPeriod,
    decimal Difference,
    decimal PercentageChange
);

public record DayComparison(
    int DayOfMonth,
    decimal ThisMonth,
    decimal LastMonth,
    decimal Difference,
    decimal PercentageChange
);

public record AccountSummary(
    string AccountId,
    string AccountName,
    decimal TotalCost,
    Dictionary<string, decimal> CostByService
);

public record ServiceAccountSummary(
    string Name,
    decimal MtdCost,
    decimal LastMonthSameDayCost,
    decimal MtdDifferencePercent,
    bool MtdIsUp,
    decimal LastFullMonthCost,
    decimal PreviousFullMonthCost,
    decimal FullMonthDifferencePercent,
    bool FullMonthIsUp
);

// View models for DataGrid binding
public class ServiceAccountRow {
    public string Name { get; set; } = "";
    public decimal MtdCost { get; set; }
    public decimal LastMtdCost { get; set; }
    public string MtdDiffPercent { get; set; } = "";
    public string MtdDirection { get; set; } = "";
    public bool MtdIsUp { get; set; }
    public decimal Rolling30Cost { get; set; }
    public decimal Prev30Cost { get; set; }
    public string FullDiffPercent { get; set; } = "";
    public string FullDirection { get; set; } = "";
    public bool FullIsUp { get; set; }

    public static ServiceAccountRow FromSummary(ServiceAccountSummary s) => new() {
        Name = s.Name.Length > 40 ? s.Name[..37] + "..." : s.Name,
        MtdCost = s.MtdCost,
        LastMtdCost = s.LastMonthSameDayCost,
        MtdDiffPercent = s.LastMonthSameDayCost > 0 ? $"{s.MtdDifferencePercent:+0.0;-0.0}%" : "-",
        MtdDirection = s.LastMonthSameDayCost > 0 ? (s.MtdIsUp ? "↑" : "↓") : "-",
        MtdIsUp = s.MtdIsUp,
        Rolling30Cost = s.LastFullMonthCost,
        Prev30Cost = s.PreviousFullMonthCost,
        FullDiffPercent = s.PreviousFullMonthCost > 0 ? $"{s.FullMonthDifferencePercent:+0.0;-0.0}%" : "-",
        FullDirection = s.PreviousFullMonthCost > 0 ? (s.FullMonthIsUp ? "↑" : "↓") : "-",
        FullIsUp = s.FullMonthIsUp
    };
}

public class DayComparisonRow {
    public string Day { get; set; } = "";
    public decimal ThisMonth { get; set; }
    public decimal LastMonth { get; set; }
    public string Difference { get; set; } = "";
    public string PercentChange { get; set; } = "";
    public bool IsUp { get; set; }
    public bool IsTotal { get; set; }

    public static DayComparisonRow FromComparison(DayComparison d) => new() {
        Day = d.DayOfMonth.ToString(),
        ThisMonth = d.ThisMonth,
        LastMonth = d.LastMonth,
        Difference = d.Difference != 0 ? $"{(d.Difference >= 0 ? "↑" : "↓")} ${Math.Abs(d.Difference):N2}" : "-",
        PercentChange = d.LastMonth > 0 ? $"{d.PercentageChange:+0.0;-0.0}%" : "-",
        IsUp = d.Difference >= 0,
        IsTotal = false
    };
}
