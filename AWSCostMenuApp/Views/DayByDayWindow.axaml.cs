using Avalonia.Controls;
using AWSCostMenuApp.ViewModels;

namespace AWSCostMenuApp.Views;

public partial class DayByDayWindow : Window {
    public DayByDayWindow() {
        InitializeComponent();
        DataContext = new DayByDayViewModel();
    }
}
