using BankingApp.Models;
using BankingApp.Services;

namespace BankingApp;

public static class Program
{
    private static readonly string DataFile = Path.Combine(AppContext.BaseDirectory, "Data", "bank_data.json");

    public static void Main()
    {
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            Logger.Error("Unhandled error while running the application", ex);
            Console.WriteLine();
            Console.WriteLine("An unexpected error occurred. The program will now close.");
        }
    }

    private static void Run()
    {
        var storage = new JsonStorageService(DataFile);

        while (true)
        {
            Console.Clear();
            ShowTitle("ATM Simulator");

            // ყოველი ციკლის დასაწყისში ანგარიშების სია თავიდან იტვირთება ფაილიდან
            var accounts = storage.LoadAccounts();

            var matchedAccount = AuthenticateCard(accounts);
            if (matchedAccount is null)
            {
                continue; // ბრუნდება საწყის ეკრანზე
            }

            var pinAccepted = AuthenticatePin(matchedAccount);
            if (!pinAccepted)
            {
                continue; // ბრუნდება საწყის ეკრანზე
            }

            var atm = new AtmService(matchedAccount, accounts, storage);
            ShowMenu(atm);
        }
    }

    // ბარათის ნომრისა და ვადის შემოწმება - შესაბამისი ანგარიშის მოძებნა სიაში
    private static Account? AuthenticateCard(List<Account> accounts)
    {
        Console.WriteLine("Please enter your card details.");
        Console.WriteLine();

        Console.Write("Card number (e.g. 1234-5678-9012-3456): ");
        var cardNumber = (Console.ReadLine() ?? string.Empty).Trim();

        Console.Write("Expiration date (e.g. 12/25): ");
        var expiresOn = (Console.ReadLine() ?? string.Empty).Trim();

        var match = accounts.FirstOrDefault(acc =>
            acc.CardDetails.Number == cardNumber &&
            acc.CardDetails.ExpiresOn == expiresOn);

        if (match is null)
        {
            Console.WriteLine();
            Console.WriteLine("Data is not valid. Please provide correct data.");
            Logger.Info("Card authentication failed - no matching account.");
            Pause();
            return null;
        }

        Logger.Info($"Card authentication succeeded for {match.FullName}.");
        return match;
    }

    // PIN კოდის შემოწმება
    private static bool AuthenticatePin(Account account)
    {
        Console.WriteLine();
        Console.Write("Please enter your PIN code: ");
        var pin = (Console.ReadLine() ?? string.Empty).Trim();

        if (pin != account.PinCode)
        {
            Console.WriteLine();
            Console.WriteLine("Incorrect PIN. You will be logged out.");
            Logger.Info($"PIN authentication failed for {account.FullName}.");
            Pause();
            return false;
        }

        Logger.Info($"PIN authentication succeeded for {account.FullName}.");
        return true;
    }

    private static void ShowMenu(AtmService atm)
    {
        var sessionActive = true;

        while (sessionActive)
        {
            Console.Clear();
            ShowTitle($"Hello, {atm.HolderName}");

            Console.WriteLine("1. Check balance");
            Console.WriteLine("2. Withdraw money");
            Console.WriteLine("3. Last 5 transactions");
            Console.WriteLine("4. Deposit money");
            Console.WriteLine("5. Change PIN");
            Console.WriteLine("6. Currency conversion");
            Console.WriteLine("0. Exit (eject card)");
            Console.WriteLine();
            Console.Write("Choose an option: ");

            var choice = (Console.ReadLine() ?? string.Empty).Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    RunCheckBalance(atm);
                    break;

                case "2":
                    RunWithdraw(atm);
                    break;

                case "3":
                    RunHistory(atm);
                    break;

                case "4":
                    RunDeposit(atm);
                    break;

                case "5":
                    RunChangePin(atm);
                    break;

                case "6":
                    RunConversion(atm);
                    break;

                case "0":
                    sessionActive = false;
                    Console.WriteLine("Thank you for using our ATM! Card ejected.");
                    Pause();
                    break;

                default:
                    Console.WriteLine("Invalid option. Please choose between 0 and 6.");
                    Pause();
                    break;
            }
        }
    }

    private static void RunCheckBalance(AtmService atm)
    {
        Console.WriteLine("Current balance:");
        Console.WriteLine(atm.CheckBalance());
        Pause();
    }

    private static void RunWithdraw(AtmService atm)
    {
        Console.Write("Enter the amount to withdraw (GEL): ");
        var input = Console.ReadLine() ?? string.Empty;

        if (!decimal.TryParse(input, out var amount))
        {
            Console.WriteLine("Invalid format. Please enter a number.");
            Pause();
            return;
        }

        var outcome = atm.Withdraw(amount);
        Console.WriteLine(outcome.Message);
        Pause();
    }

    private static void RunHistory(AtmService atm)
    {
        var entries = atm.RecentHistory();

        if (entries.Count == 0)
        {
            Console.WriteLine("Transaction history is empty.");
        }
        else
        {
            Console.WriteLine("Last 5 transactions:");
            Console.WriteLine();

            foreach (var entry in entries)
            {
                Console.WriteLine(
                    $"{entry.OccurredAt:yyyy-MM-dd HH:mm:ss} | {entry.Type} | " +
                    $"GEL: {entry.Gel:0.00}, USD: {entry.Usd:0.00}, EUR: {entry.Eur:0.00}");
            }
        }

        Pause();
    }

    private static void RunDeposit(AtmService atm)
    {
        Console.Write("Currency (GEL/USD/EUR): ");
        var currency = Console.ReadLine() ?? string.Empty;

        Console.Write("Enter amount: ");
        var input = Console.ReadLine() ?? string.Empty;

        if (!decimal.TryParse(input, out var amount))
        {
            Console.WriteLine("Invalid format. Please enter a number.");
            Pause();
            return;
        }

        var outcome = atm.Deposit(currency, amount);
        Console.WriteLine(outcome.Message);
        Pause();
    }

    private static void RunChangePin(AtmService atm)
    {
        Console.Write("Enter your current PIN: ");
        var currentPin = Console.ReadLine() ?? string.Empty;

        Console.Write("Enter your new PIN (4 digits): ");
        var newPin = Console.ReadLine() ?? string.Empty;

        var outcome = atm.ChangePin(currentPin, newPin);
        Console.WriteLine(outcome.Message);
        Pause();
    }

    private static void RunConversion(AtmService atm)
    {
        Console.WriteLine("Available currencies: GEL, USD, EUR");
        Console.Write("Convert from: ");
        var from = Console.ReadLine() ?? string.Empty;

        Console.Write("Convert to: ");
        var to = Console.ReadLine() ?? string.Empty;

        Console.Write($"Enter the amount in {from.Trim().ToUpper()} to convert: ");
        var input = Console.ReadLine() ?? string.Empty;

        if (!decimal.TryParse(input, out var amount))
        {
            Console.WriteLine("Invalid format. Please enter a number.");
            Pause();
            return;
        }

        var outcome = atm.Convert(from, to, amount);
        Console.WriteLine(outcome.Message);
        Pause();
    }

    private static void ShowTitle(string text)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine($"   {text}");
        Console.WriteLine("==============================================");
        Console.WriteLine();
    }

    private static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}
