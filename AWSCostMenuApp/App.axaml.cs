using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using AWSCostMenuApp.Services;
using AWSCostMenuApp.Settings;
using AWSCostMenuApp.Views;

namespace AWSCostMenuApp;

public partial class App : Application {
    private TrayIcon? _trayIcon;
    private AppSettings _settings = new();
    private CostRepository? _repository;
    private CostAnalysisService? _analysisService;
    
    public static App? Instance { get; private set; }
    public CostAnalysisService? AnalysisService => _analysisService;
    public AppSettings Settings => _settings;

    public override void Initialize() {
        Instance = this;
        AvaloniaXamlLoader.Load(this);
        LoadSettings();
        InitializeServices();
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            // Don't show main window on startup - we're a tray app
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            SetupTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void LoadSettings() {
        try {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath)) {
                var json = File.ReadAllText(configPath);
                _settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        } catch {
            _settings = new AppSettings();
        }
    }

    private void InitializeServices() {
        try {
            var dbPath = Path.IsPathRooted(_settings.DatabasePath)
                ? _settings.DatabasePath
                : Path.Combine(AppContext.BaseDirectory, _settings.DatabasePath);

            _repository = new CostRepository(dbPath);
            _analysisService = new CostAnalysisService(_repository);
        } catch (Exception ex) {
            Console.WriteLine($"Failed to initialize services: {ex.Message}");
        }
    }

    private void SetupTrayIcon() {
        var menu = new NativeMenu();

        var dashboardItem = new NativeMenuItem("Dashboard Overview");
        dashboardItem.Click += (_, _) => ShowWindow<DashboardWindow>();
        menu.Items.Add(dashboardItem);

        var accountsItem = new NativeMenuItem("Account Summary");
        accountsItem.Click += (_, _) => ShowWindow<AccountSummaryWindow>();
        menu.Items.Add(accountsItem);

        var servicesItem = new NativeMenuItem("Service Summary");
        servicesItem.Click += (_, _) => ShowWindow<ServiceSummaryWindow>();
        menu.Items.Add(servicesItem);

        var dayByDayItem = new NativeMenuItem("Day-by-Day");
        dayByDayItem.Click += (_, _) => ShowWindow<DayByDayWindow>();
        menu.Items.Add(dayByDayItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var refreshItem = new NativeMenuItem("Refresh Data");
        refreshItem.Click += async (_, _) => await RefreshDataAsync();
        menu.Items.Add(refreshItem);

        var fullSyncItem = new NativeMenuItem("Full Sync Data");
        fullSyncItem.Click += async (_, _) => await FullSyncAsync();
        menu.Items.Add(fullSyncItem);

        var creditsItem = new NativeMenuItem("Toggle Credits");
        creditsItem.Click += (_, _) => ToggleCredits();
        menu.Items.Add(creditsItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(quitItem);

        _trayIcon = new TrayIcon {
            ToolTipText = "AWS Cost Monitor",
            Menu = menu,
            Icon = new WindowIcon(GetIconStream()),
            IsVisible = true
        };

        _trayIcon.Clicked += (_, _) => ShowWindow<DashboardWindow>();
    }

    private Stream GetIconStream() {
        // Try to load custom icon, fall back to embedded resource
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
        if (File.Exists(iconPath)) {
            return File.OpenRead(iconPath);
        }
        
        var assembly = typeof(App).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        var resourceName = "AWSCostMenuApp.Assets.icon.png";
        var icon = assembly.GetManifestResourceStream(resourceName);
        if (icon == null) {
            var uri = new Uri("avares://AWSCostMenuApp/Assets/icon.png");
            return AssetLoader.Open(uri) ?? throw new InvalidOperationException("Icon not found");
        }
        throw new FileNotFoundException("Icon not found");
    }

    private void ShowWindow<T>() where T : Window, new() {
        var window = new T();
        window.Show();
        window.Activate();
    }

    private async Task RefreshDataAsync() {
        if (_analysisService == null || _repository == null) return;

        try {
            var syncService = new CostSyncService(_settings.Aws, _repository);
            await syncService.RefreshAsync();
            NotificationService.Instance.NotifyDataRefreshed();
        } catch (Exception ex) {
            Console.WriteLine($"Refresh failed: {ex.Message}");
        }
    }
    
    private async Task FullSyncAsync() {
        if (_analysisService == null || _repository == null) return;
        try {
            var syncService = new CostSyncService(_settings.Aws, _repository);
            await syncService.RefreshAsync(forceFullSync: true); 
            NotificationService.Instance.NotifyDataRefreshed();
        } catch (OperationCanceledException) {
            // Ignore
        } catch (Exception ex) {
            Console.WriteLine($"Refresh failed: {ex.Message}");
        }
    }


    private void ToggleCredits() {
        if (_analysisService == null) return;
        
        _analysisService.IncludeCredits = !_analysisService.IncludeCredits;
        NotificationService.Instance.NotifyCreditsToggled(_analysisService.IncludeCredits);
    }

    private void Shutdown() {
        _trayIcon?.Dispose();
        _repository?.Dispose();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.Shutdown();
        }
    }
}
