using Avalonia.Controls;
using AWSCostMenuApp.ViewModels;

namespace AWSCostMenuApp.Views;

public partial class AccountSummaryWindow : Window {
    public AccountSummaryWindow() {
        InitializeComponent();
        DataContext = new SummaryViewModel(isServiceView: false);
    }
}
