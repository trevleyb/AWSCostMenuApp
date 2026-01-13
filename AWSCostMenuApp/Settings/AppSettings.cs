namespace AWSCostMenuApp.Settings;

public class AwsSettings {
    public string Profile { get; set; } = "default";
    public string Region { get; set; } = "us-east-1";
    public bool UseSso { get; set; } = true;
}

public class AppSettings {
    public AwsSettings Aws { get; set; } = new();
    public string DatabasePath { get; set; } = "costs.db";
    public int RefreshIntervalMinutes { get; set; } = 60;
}
