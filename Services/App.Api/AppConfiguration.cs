namespace App.Api;

public class AppConfiguration
{
    public string SupabaseId { get; set; } = string.Empty;
    public string SupabaseHost { get; set; } = string.Empty;
    public string SupabasePort { get; set; } = string.Empty;    
    public string SupabaseDatabasePassword { get; set; } = string.Empty;
    public string SupabaseAnonKey { get; set; } = string.Empty;
    public string SupabaseServiceRoleKey { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
}