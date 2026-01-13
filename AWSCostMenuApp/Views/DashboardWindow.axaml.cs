using Avalonia.Controls;
using AWSCostMenuApp.ViewModels;

namespace AWSCostMenuApp.Views;

public partial class DashboardWindow : Window {
    public DashboardWindow() {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
