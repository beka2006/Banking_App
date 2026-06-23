using System.Text.Json;
using BankingApp.Models;

namespace BankingApp.Services;

// ეს კლასი პასუხისმგებელია bank_data.json-ში ანგარიშების სიის
// წაკითხვასა და ჩაწერაზე. ფაილში ინახება List<Account>, არა ერთი ანგარიში.
public class JsonStorageService
{
    private readonly string _path;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonStorageService(string path)
    {
        _path = path;
    }

    // ყველა ანგარიშის წამოღება ფაილიდან.
    // ფაილის არ-არსებობის დაზიანების ან ცარიელის შემთხვევაში ვაბრუნებთ
    // ერთ საწყის ტესტ-ანგარიშს შემცველ სიას, რომ აპლიკაცია არასოდეს არ გაჩერდეს.
    public List<Account> LoadAccounts()
    {
        try
        {
            if (!File.Exists(_path))
            {
                Logger.Info("bank_data.json not found, creating a default dataset.");
                var seeded = CreateSeedAccounts();
                SaveAccounts(seeded);
                return seeded;
            }

            var raw = File.ReadAllText(_path);
            var accounts = JsonSerializer.Deserialize<List<Account>>(raw, _jsonOptions);

            if (accounts is null || accounts.Count == 0)
            {
                Logger.Info("bank_data.json is empty or invalid, creating a default dataset.");
                var seeded = CreateSeedAccounts();
                SaveAccounts(seeded);
                return seeded;
            }

            return accounts;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to read account data", ex);
            return CreateSeedAccounts();
        }
    }

    // ანგარიშთა მთლიანი სიის ფაილში ჩაწერა
    public void SaveAccounts(List<Account> accounts)
    {
        try
        {
            var folder = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var json = JsonSerializer.Serialize(accounts, _jsonOptions);
            File.WriteAllText(_path, json);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save account data", ex);
        }
    }

    private List<Account> CreateSeedAccounts()
    {
        return new List<Account>
        {
            new Account
            {
                FirstName = "John",
                LastName = "Doe",
                CardDetails = new CardDetails
                {
                    Number = "1234-5678-9012-3456",
                    ExpiresOn = "12/25",
                    Cvc = "123"
                },
                PinCode = "1234",
                GelBalance = 100,
                UsdBalance = 0,
                EurBalance = 0,
                History = new List<Transaction>()
            }
        };
    }
}
