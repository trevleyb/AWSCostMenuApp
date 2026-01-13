using System.Collections.ObjectModel;
using Avalonia.Media;
using AWSCostMenuApp.Models;
using AWSCostMenuApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AWSCostMenuApp.ViewModels;

public partial class SummaryViewModel : ViewModelBase {
    private readonly bool _isServiceView;
    
    [ObservableProperty] private string _dateRangeInfo = "";
    [ObservableProperty] private string _creditsStatus = "";
    [ObservableProperty] private IBrush _creditsStatusColor = Brushes.Gray;
    [ObservableProperty] private string _totalMtd = "$0.00";
    [ObservableProperty] private string _totalRolling30 = "$0.00";
    
    public ObservableCollection<ServiceAccountRow> Items { get; } = [];

    public SummaryViewModel() : this(true) { }

    public SummaryViewModel(bool isServiceView) {
        _isServiceView = isServiceView;
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

        var ranges = service.GetDateRanges();
        DateRangeInfo = $"MTD: {ranges.MtdRange} | Last MTD: {ranges.LastMtdRange} | Rolling 30d: {ranges.Rolling30Range} | Prev 30d: {ranges.Prev30Range}";

        Items.Clear();

        var data = _isServiceView 
            ? service.GetServiceComparison().Take(30)
            : service.GetAccountComparison();

        decimal totalMtd = 0;
        decimal totalRolling = 0;

        foreach (var item in data) {
            Items.Add(ServiceAccountRow.FromSummary(item));
            totalMtd += item.MtdCost;
            totalRolling += item.LastFullMonthCost;
        }

        TotalMtd = $"${totalMtd:N2}";
        TotalRolling30 = $"${totalRolling:N2}";
    }
}
