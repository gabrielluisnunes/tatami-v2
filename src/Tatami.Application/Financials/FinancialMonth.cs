using System.Globalization;

namespace Tatami.Application.Financials;

public static class FinancialMonth
{
    public static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(-3)).DateTime);

    public static DateOnly First(int year, int month)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12)
            throw new FinancialException("INVALID_MONTH", "Informe um mês válido (YYYY-MM).");
        return new DateOnly(year, month, 1);
    }

    public static DateOnly Parse(string? month, TimeProvider timeProvider)
    {
        if (month is null)
        {
            var today = Today(timeProvider);
            return First(today.Year, today.Month);
        }
        if (month.Length != 7 || !DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
            throw new FinancialException("INVALID_MONTH", "Informe um mês válido (YYYY-MM).");
        return first;
    }

    public static string Key(DateOnly month) => month.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public static DateOnly DueDateForMonth(int year, int month, int paymentDueDay) =>
        First(year, month).AddDays(Math.Clamp(paymentDueDay, 1, DateTime.DaysInMonth(year, month)) - 1);
}