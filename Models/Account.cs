namespace BankingApp.Models;

using System.Text.Json.Serialization;

public class Account
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public CardDetails CardDetails { get; set; } = new();
    public string PinCode { get; set; } = string.Empty;

    public decimal GelBalance { get; set; }
    public decimal UsdBalance { get; set; }
    public decimal EurBalance { get; set; }

    public List<Transaction> History { get; set; } = new();

    [JsonIgnore]  // ეს არ იწერება json ფაილში
    public string FullName => $"{FirstName} {LastName}";
}
