namespace BankingApp.Services;

// კურსები ფიქსირებულია მაგალითისთვის
public static class CurrencyService
{
    private const decimal GelPerUsd = 0.37m;
    private const decimal GelPerEur = 0.34m;

    public static decimal GelToUsd(decimal gel) => Math.Round(gel * GelPerUsd, 2);
    public static decimal GelToEur(decimal gel) => Math.Round(gel * GelPerEur, 2);

    public static decimal UsdToGel(decimal usd) => Math.Round(usd / GelPerUsd, 2);
    public static decimal EurToGel(decimal eur) => Math.Round(eur / GelPerEur, 2);

    // USD <-> EUR პირდაპირი კონვერტაცია, GEL-ის გავლით:
    // ჯერ USD-ს ან EUR-ს ვაბრუნებთ GEL-ში, მერე GEL-დან მეორე ვალუტაში
    public static decimal UsdToEur(decimal usd) => GelToEur(UsdToGel(usd));
    public static decimal EurToUsd(decimal eur) => GelToUsd(EurToGel(eur));
}
