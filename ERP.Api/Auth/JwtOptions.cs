namespace ERP.Api.Auth;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ErpApi";
    public int ExpirationHours { get; set; } = 12;
}
