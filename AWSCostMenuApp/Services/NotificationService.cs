namespace AWSCostMenuApp.Services;

public class NotificationService {
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    public event Action? DataRefreshed;
    public event Action<bool>? CreditsToggled;

    public void NotifyDataRefreshed() {
        DataRefreshed?.Invoke();
    }

    public void NotifyCreditsToggled(bool includeCredits) {
        CreditsToggled?.Invoke(includeCredits);
    }
}
