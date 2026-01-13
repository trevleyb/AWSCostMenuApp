using System.Collections.ObjectModel;
using Avalonia.Media;
using AWSCostMenuApp.Models;
using AWSCostMenuApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AWSCostMenuApp.ViewModels;

public partial class DayByDayViewModel : ViewModelBase {
    [ObservableProperty] private string _creditsStatus = "";
    [ObservableProperty] private IBrush _creditsStatusColor = Brushes.Gray;
    [ObservableProperty] private string _thisMonthTotal = "$0.00";
    [ObservableProperty] private string _lastMonthTotal = "$0.00";
    [ObservableProperty] private string _totalDifference = "$0.00";
    [ObservableProperty] private IBrush _totalDifferenceColor = Brushes.Gray;
    
    public ObservableCollection<DayComparisonRow> Days { get; } = new();

    public DayByDayViewModel() {
        LoadData();
        
        NotificationService.Instance.DataRefreshed += LoadData;
        NotificationService.Instance.CreditsToggled += _ => LoadData();
    }

    private void LoadData() {
        var service = App.Instance?.AnalysisService;
        if (service == null) return;

        var includeCredits = service.IncludeCredits;
        CreditsStatus = includeCredits ? "Including Credits (Net Cost)" : "Excluding Credits (Gross Cost)";
        CreditsStatusColor = includeCredits ? Brushes.Green : Brushes.Orange;

        Days.Clear();

        var days = service.GetDayByDayComparison().ToList();
        
        decimal runningThisMonth = 0;
        decimal runningLastMonth = 0;

        foreach (var day in days) {
            runningThisMonth += day.ThisMonth;
            runningLastMonth += day.LastMonth;
            Days.Add(DayComparisonRow.FromComparison(day));
        }

        ThisMonthTotal = $"${runningThisMonth:N2}";
        LastMonthTotal = $"${runningLastMonth:N2}";
        
        var diff = runningThisMonth - runningLastMonth;
        var arrow = diff >= 0 ? "↑" : "↓";
        TotalDifference = $"{arrow} ${Math.Abs(diff):N2}";
        TotalDifferenceColor = diff >= 0 ? Brushes.Red : Brushes.Green;
    }
}
