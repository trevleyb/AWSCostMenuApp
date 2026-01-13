using Avalonia.Controls;
using AWSCostMenuApp.ViewModels;

namespace AWSCostMenuApp.Views;

public partial class ServiceSummaryWindow : Window {
    public ServiceSummaryWindow() {
        InitializeComponent();
        DataContext = new SummaryViewModel(isServiceView: true);
    }
}
