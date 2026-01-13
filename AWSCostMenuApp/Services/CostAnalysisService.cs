using AWSCostMenuApp.Models;

namespace AWSCostMenuApp.Services;

public class CostAnalysisService {
    private readonly CostRepository _repository;
    
    public bool IncludeCredits { get; set; } = true;

    public CostAnalysisService(CostRepository repository) {
        _repository = repository;
    }

    public CostComparison GetMonthToDateComparison() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dayOfMonth = today.Day;

        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthSameDay = new DateOnly(
            lastMonthStart.Year,
            lastMonthStart.Month,
            Math.Min(dayOfMonth, DateTime.DaysInMonth(lastMonthStart.Year, lastMonthStart.Month)));

        var thisMonthTotal = _repository.GetTotalForDateRange(thisMonthStart, today, IncludeCredits);
        var lastMonthTotal = _repository.GetTotalForDateRange(lastMonthStart, lastMonthSameDay, IncludeCredits);

        return CreateComparison("Month-to-Date", thisMonthTotal, lastMonthTotal);
    }

    public CostComparison GetFullMonthComparison() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var lastMonthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var lastMonthEnd = new DateOnly(today.Year, today.Month, 1).AddDays(-1);

        var previousMonthStart = lastMonthStart.AddMonths(-1);
        var previousMonthEnd = lastMonthStart.AddDays(-1);

        var lastMonthTotal = _repository.GetTotalForDateRange(lastMonthStart, lastMonthEnd, IncludeCredits);
        var previousMonthTotal = _repository.GetTotalForDateRange(previousMonthStart, previousMonthEnd, IncludeCredits);

        return CreateComparison($"{lastMonthStart:MMMM} vs {previousMonthStart:MMMM}", lastMonthTotal, previousMonthTotal);
    }

    public IEnumerable<DayComparison> GetDayByDayComparison() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var thisMonthTotals = _repository.GetDailyTotals(thisMonthStart, today, IncludeCredits)
            .ToDictionary(x => x.Date.Day, x => x.Total);

        var lastMonthEnd = thisMonthStart.AddDays(-1);
        var lastMonthTotals = _repository.GetDailyTotals(lastMonthStart, lastMonthEnd, IncludeCredits)
            .ToDictionary(x => x.Date.Day, x => x.Total);

        var maxDay = Math.Max(
            thisMonthTotals.Keys.DefaultIfEmpty(0).Max(),
            lastMonthTotals.Keys.DefaultIfEmpty(0).Max());

        for (var day = 1; day <= maxDay; day++) {
            var thisMonth = thisMonthTotals.GetValueOrDefault(day, 0);
            var lastMonth = lastMonthTotals.GetValueOrDefault(day, 0);
            var diff = thisMonth - lastMonth;
            var pct = lastMonth != 0 ? (diff / lastMonth) * 100 : (thisMonth != 0 ? 100 : 0);

            yield return new DayComparison(day, thisMonth, lastMonth, diff, pct);
        }
    }

    public IEnumerable<AccountSummary> GetAccountSummariesThisMonth() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);

        return _repository.GetAccountSummaries(thisMonthStart, today, IncludeCredits);
    }

    public IEnumerable<AccountSummary> GetAccountSummariesLastMonth() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);

        return _repository.GetAccountSummaries(lastMonthStart, lastMonthEnd, IncludeCredits);
    }

    public IEnumerable<ServiceAccountSummary> GetServiceAccountComparison(bool byService = true) {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dayOfMonth = today.Day;

        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthSameDay = new DateOnly(
            lastMonthStart.Year,
            lastMonthStart.Month,
            Math.Min(dayOfMonth, DateTime.DaysInMonth(lastMonthStart.Year, lastMonthStart.Month)));

        var yesterday = today.AddDays(-1);
        var lastFullMonthStart = yesterday.AddDays(-29);
        var previousFullMonthEnd = lastFullMonthStart.AddDays(-1);
        var previousFullMonthStart = previousFullMonthEnd.AddDays(-29);

        var mtdCosts = GetCostsByGrouping(thisMonthStart, today, byService);
        var lastMonthSameDayCosts = GetCostsByGrouping(lastMonthStart, lastMonthSameDay, byService);
        var lastFullMonthCosts = GetCostsByGrouping(lastFullMonthStart, yesterday, byService);
        var previousFullMonthCosts = GetCostsByGrouping(previousFullMonthStart, previousFullMonthEnd, byService);

        var allNames = mtdCosts.Keys
            .Union(lastMonthSameDayCosts.Keys)
            .Union(lastFullMonthCosts.Keys)
            .Union(previousFullMonthCosts.Keys)
            .OrderBy(x => x);

        foreach (var name in allNames) {
            var mtd = mtdCosts.GetValueOrDefault(name, 0);
            var lastSameDay = lastMonthSameDayCosts.GetValueOrDefault(name, 0);
            var lastFull = lastFullMonthCosts.GetValueOrDefault(name, 0);
            var prevFull = previousFullMonthCosts.GetValueOrDefault(name, 0);

            var mtdDiff = lastSameDay != 0 ? ((mtd - lastSameDay) / lastSameDay) * 100 : (mtd != 0 ? 100 : 0);
            var fullDiff = prevFull != 0 ? ((lastFull - prevFull) / prevFull) * 100 : (lastFull != 0 ? 100 : 0);

            yield return new ServiceAccountSummary(
                name,
                mtd,
                lastSameDay,
                mtdDiff,
                mtd >= lastSameDay,
                lastFull,
                prevFull,
                fullDiff,
                lastFull >= prevFull
            );
        }
    }

    public IEnumerable<ServiceAccountSummary> GetServiceComparison() {
        return GetServiceAccountComparison(byService: true)
            .OrderByDescending(x => x.MtdCost);
    }

    public IEnumerable<ServiceAccountSummary> GetAccountComparison() {
        return GetServiceAccountComparison(byService: false)
            .OrderByDescending(x => x.MtdCost);
    }

    public (decimal MtdCredits, decimal LastMonthCredits) GetCreditsSummary() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);

        var mtdCredits = _repository.GetCreditsForDateRange(thisMonthStart, today);
        var lastMonthCredits = _repository.GetCreditsForDateRange(lastMonthStart, lastMonthEnd);

        return (mtdCredits, lastMonthCredits);
    }

    public DateRangeInfo GetDateRanges() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var yesterday = today.AddDays(-1);
        var lastFullMonthStart = yesterday.AddDays(-29);
        var previousFullMonthEnd = lastFullMonthStart.AddDays(-1);
        var previousFullMonthStart = previousFullMonthEnd.AddDays(-29);

        return new DateRangeInfo(
            $"{thisMonthStart:MMM d} - {today:MMM d}",
            $"{lastMonthStart:MMM d} - {lastMonthStart.AddDays(today.Day - 1):MMM d}",
            $"{lastFullMonthStart:MMM d} - {yesterday:MMM d}",
            $"{previousFullMonthStart:MMM d} - {previousFullMonthEnd:MMM d}"
        );
    }

    private Dictionary<string, decimal> GetCostsByGrouping(DateOnly from, DateOnly to, bool byService) {
        var costs = _repository.GetCostsForDateRange(from, to, IncludeCredits);

        if (byService) {
            return costs
                .GroupBy(c => c.Service)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Cost));
        } else {
            return costs
                .GroupBy(c => c.AccountName)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Cost));
        }
    }

    private static CostComparison CreateComparison(string label, decimal current, decimal previous) {
        var diff = current - previous;
        var pct = previous != 0 ? (diff / previous) * 100 : (current != 0 ? 100 : 0);

        return new CostComparison(label, current, previous, diff, pct);
    }
}

public record DateRangeInfo(
    string MtdRange,
    string LastMtdRange,
    string Rolling30Range,
    string Prev30Range
);
