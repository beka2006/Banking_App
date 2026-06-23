namespace BankingApp.Models;

public class Transaction
{
    public DateTime OccurredAt { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Gel { get; set; }
    public decimal Usd { get; set; }
    public decimal Eur { get; set; }
}
