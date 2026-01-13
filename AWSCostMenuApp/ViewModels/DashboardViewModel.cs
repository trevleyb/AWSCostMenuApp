using Avalonia.Media;
using AWSCostMenuApp.Models;
using AWSCostMenuApp.Services;
using AWSCostMenuApp.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AWSCostMenuApp.ViewModels;

public partial class DashboardViewModel : ViewModelBase {
    [ObservableProperty] private string _lastUpdated = "";
    [ObservableProperty] private string _creditsStatus = "";
    [ObservableProperty] private IBrush _creditsStatusColor = Brushes.Gray;
    
    [ObservableProperty] private ComparisonRow? _mtdComparison;
    [ObservableProperty] private ComparisonRow? _fullMonthComparison;
    
    [ObservableProperty] private string _mtdTotal = "$0.00";
    [ObservableProperty] private string _mtdVsLastMonth = "0%";
    [ObservableProperty] private IBrush _mtdVsLastMonthColor = Brushes.Gray;
    
    [ObservableProperty] private string _topAccountName = "-";
    [ObservableProperty] private string _topAccountCost = "$0.00";

    public DashboardViewModel() {
        LoadData();
        
        NotificationService.Instance.DataRefreshed += LoadData;
        NotificationService.Instance.CreditsToggled += _ => LoadData();
    }

    private void LoadData() {
        var service = App.Instance?.AnalysisService;
        if (service == null) return;

        LastUpdated = $"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        
        var includeCredits = service.IncludeCredits;
        CreditsStatus = includeCredits ? "Including Credits (Net Cost)" : "Excluding Credits (Gross Cost)";
        CreditsStatusColor = includeCredits ? Brushes.Green : Brushes.Orange;

        var mtd = service.GetMonthToDateComparison();
        MtdComparison = ComparisonRow.FromComparison(mtd);

        var full = service.GetFullMonthComparison();
        FullMonthComparison = ComparisonRow.FromComparison(full);

        MtdTotal = $"${mtd.CurrentPeriod:N2}";
        MtdVsLastMonth = $"{mtd.PercentageChange:+0.0;-0.0}%";
        MtdVsLastMonthColor = mtd.Difference >= 0 ? Brushes.Red : Brushes.Green;

        var accounts = service.GetAccountSummariesThisMonth().ToList();
        if (accounts.Any()) {
            var top = accounts.First();
            TopAccountName = top.AccountName;
            TopAccountCost = $"${top.TotalCost:N2}";
        }
    }

    [RelayCommand]
    private async Task Refresh() {
        var app = App.Instance;
        if (app?.AnalysisService == null) return;

        try {
            var syncService = new CostSyncService(app.Settings.Aws, 
                new CostRepository(GetDbPath()));
            await syncService.RefreshAsync();
            NotificationService.Instance.NotifyDataRefreshed();
        } catch (Exception ex) {
            Console.WriteLine($"Refresh failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ToggleCredits() {
        var service = App.Instance?.AnalysisService;
        if (service == null) return;

        service.IncludeCredits = !service.IncludeCredits;
        NotificationService.Instance.NotifyCreditsToggled(service.IncludeCredits);
    }

    private string GetDbPath() {
        var settings = App.Instance?.Settings ?? new AppSettings();
        return Path.IsPathRooted(settings.DatabasePath)
            ? settings.DatabasePath
            : Path.Combine(AppContext.BaseDirectory, settings.DatabasePath);
    }
}

public class ComparisonRow {
    public string Label { get; set; } = "";
    public string CurrentFormatted { get; set; } = "";
    public string PreviousFormatted { get; set; } = "";
    public string DifferenceFormatted { get; set; } = "";
    public string PercentFormatted { get; set; } = "";
    public IBrush DifferenceColor { get; set; } = Brushes.Gray;

    public static ComparisonRow FromComparison(CostComparison c) {
        var isUp = c.Difference >= 0;
        var arrow = isUp ? "↑" : "↓";
        
        return new ComparisonRow {
            Label = c.Label,
            CurrentFormatted = $"${c.CurrentPeriod:N2}",
            PreviousFormatted = $"${c.PreviousPeriod:N2}",
            DifferenceFormatted = $"{arrow} ${Math.Abs(c.Difference):N2}",
            PercentFormatted = $"{c.PercentageChange:+0.0;-0.0}%",
            DifferenceColor = isUp ? Brushes.Red : Brushes.Green
        };
    }
}
