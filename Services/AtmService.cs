using BankingApp.Models;

namespace BankingApp.Services;

// მთავარი ბანკომატის ლოგიკა: ვალიდაცია და ყველა ოპერაცია ერთ კონკრეტულ ანგარიშზე.
// _allAccounts გვინდა მხოლოდ იმიტომ, რომ SaveChanges-მა ფაილში მთლიანი
// განახლებული სია ჩაიწეროს (Account არის class, ანუ reference type,
// ამიტომ _account-ზე გაკეთებული ცვლილება ავტომატურად ჩაიწერება სიაშიც).
public class AtmService
{
    private readonly Account _account;
    private readonly List<Account> _allAccounts;
    private readonly JsonStorageService _storage;

    public AtmService(Account account, List<Account> allAccounts, JsonStorageService storage)
    {
        _account = account;
        _allAccounts = allAccounts;
        _storage = storage;
    }

    public string HolderName => _account.FullName;

    public string CheckBalance()
    {
        Log("BalanceCheck", 0, 0, 0);

        return $"GEL: {_account.GelBalance:0.00}{Environment.NewLine}" +
               $"USD: {_account.UsdBalance:0.00}{Environment.NewLine}" +
               $"EUR: {_account.EurBalance:0.00}";
    }

    public (bool Ok, string Message) Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            return (false, "Amount must be a positive number.");
        }

        if (amount > _account.GelBalance)
        {
            return (false, "Insufficient funds in the account.");
        }

        _account.GelBalance -= amount;
        Log("Withdraw", amount, 0, 0);
        Persist();

        return (true, $"Withdrawn {amount:0.00} GEL. New balance: {_account.GelBalance:0.00} GEL");
    }

    public List<Transaction> RecentHistory(int count = 5)
    {
        return _account.History
            .OrderByDescending(entry => entry.OccurredAt)
            .Take(count)
            .ToList();
    }

    public (bool Ok, string Message) Deposit(string currency, decimal amount)
    {
        if (amount <= 0)
        {
            return (false, "Amount must be a positive number.");
        }

        switch (currency.Trim().ToUpper())
        {
            case "GEL":
                _account.GelBalance += amount;
                Log("Deposit", amount, 0, 0);
                Persist();
                return (true, $"Deposited {amount:0.00} GEL");

            case "USD":
                _account.UsdBalance += amount;
                Log("Deposit", 0, amount, 0);
                Persist();
                return (true, $"Deposited {amount:0.00} USD");

            case "EUR":
                _account.EurBalance += amount;
                Log("Deposit", 0, 0, amount);
                Persist();
                return (true, $"Deposited {amount:0.00} EUR");

            default:
                return (false, "Unsupported currency. Use GEL, USD or EUR.");
        }
    }

    public (bool Ok, string Message) ChangePin(string currentPin, string newPin)
    {
        if (currentPin.Trim() != _account.PinCode)
        {
            return (false, "Current PIN is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(newPin) || newPin.Length != 4 || !newPin.All(char.IsDigit))
        {
            return (false, "New PIN must consist of exactly 4 digits.");
        }

        _account.PinCode = newPin;
        Log("PinChange", 0, 0, 0);
        Persist();

        return (true, "PIN code changed successfully.");
    }

    public (bool Ok, string Message) Convert(string fromCurrency, string toCurrency, decimal amount)
    {
        if (amount <= 0)
        {
            return (false, "Amount must be a positive number.");
        }

        var source = fromCurrency.Trim().ToUpper();
        var target = toCurrency.Trim().ToUpper();

        if (source == target)
        {
            return (false, "Currencies must be different.");
        }

        switch (source)
        {
            case "GEL" when target == "USD":
                return DoConvert(amount, _account.GelBalance, "GEL", "USD",
                    CurrencyService.GelToUsd,
                    (gelDelta, usdDelta) =>
                    {
                        _account.GelBalance -= gelDelta;
                        _account.UsdBalance += usdDelta;
                    });

            case "GEL" when target == "EUR":
                return DoConvert(amount, _account.GelBalance, "GEL", "EUR",
                    CurrencyService.GelToEur,
                    (gelDelta, eurDelta) =>
                    {
                        _account.GelBalance -= gelDelta;
                        _account.EurBalance += eurDelta;
                    });

            case "USD" when target == "GEL":
                return DoConvert(amount, _account.UsdBalance, "USD", "GEL",
                    CurrencyService.UsdToGel,
                    (usdDelta, gelDelta) =>
                    {
                        _account.UsdBalance -= usdDelta;
                        _account.GelBalance += gelDelta;
                    });

            case "EUR" when target == "GEL":
                return DoConvert(amount, _account.EurBalance, "EUR", "GEL",
                    CurrencyService.EurToGel,
                    (eurDelta, gelDelta) =>
                    {
                        _account.EurBalance -= eurDelta;
                        _account.GelBalance += gelDelta;
                    });

            case "USD" when target == "EUR":
                return DoConvert(amount, _account.UsdBalance, "USD", "EUR",
                    CurrencyService.UsdToEur,
                    (usdDelta, eurDelta) =>
                    {
                        _account.UsdBalance -= usdDelta;
                        _account.EurBalance += eurDelta;
                    });

            case "EUR" when target == "USD":
                return DoConvert(amount, _account.EurBalance, "EUR", "USD",
                    CurrencyService.EurToUsd,
                    (eurDelta, usdDelta) =>
                    {
                        _account.EurBalance -= eurDelta;
                        _account.UsdBalance += usdDelta;
                    });

            default:
                return (false, $"Conversion from {source} to {target} is not supported.");
        }
    }

    // ერთიანი დამხმარე მეთოდი ოთხივე კონვერტაციის შემთხვევისთვის,
    // რომ Convert-ში კოდი არ გამეორდეს.
    private (bool Ok, string Message) DoConvert(
        decimal amount,
        decimal availableBalance,
        string fromCode,
        string toCode,
        Func<decimal, decimal> rateFunc,
        Action<decimal, decimal> applyBalances)
    {
        if (amount > availableBalance)
        {
            return (false, $"Insufficient {fromCode} balance for conversion.");
        }

        var converted = rateFunc(amount);
        applyBalances(amount, converted);

        var gelDelta = fromCode == "GEL" ? -amount : (toCode == "GEL" ? converted : 0);
        var usdDelta = fromCode == "USD" ? -amount : (toCode == "USD" ? converted : 0);
        var eurDelta = fromCode == "EUR" ? -amount : (toCode == "EUR" ? converted : 0);

        Log("Conversion", gelDelta, usdDelta, eurDelta);
        Persist();

        return (true, $"{amount:0.00} {fromCode} -> {converted:0.00} {toCode}");
    }

    private void Log(string type, decimal gel, decimal usd, decimal eur)
    {
        _account.History.Add(new Transaction
        {
            OccurredAt = DateTime.UtcNow,
            Type = type,
            Gel = gel,
            Usd = usd,
            Eur = eur
        });
    }

    private void Persist()
    {
        _storage.SaveAccounts(_allAccounts);
        Logger.Info($"Account data updated for {_account.FullName} and saved to JSON file.");
    }
}
