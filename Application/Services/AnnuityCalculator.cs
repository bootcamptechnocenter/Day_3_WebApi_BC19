using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Services;

// Anuitas (cicilan tetap): A = P * r / (1 - (1 + r)^-n), r = bunga per bulan.
// Dibulatkan 2 desimal; angsuran terakhir menyerap selisih pembulatan
// sehingga total pokok tepat sama dengan plafon.
public static class AnnuityCalculator
{
    public static decimal MonthlyPayment(decimal principal, decimal annualRatePercent, int tenorMonths)
    {
        if (tenorMonths <= 0) throw new ArgumentOutOfRangeException(nameof(tenorMonths));

        var r = annualRatePercent / 100m / 12m;
        if (r == 0) return Math.Round(principal / tenorMonths, 2, MidpointRounding.AwayFromZero);

        var factor = (decimal)Math.Pow(1 + (double)r, -tenorMonths);
        return Math.Round(principal * r / (1 - factor), 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<CreditInstallment> BuildSchedule(
        decimal principal, decimal annualRatePercent, int tenorMonths, DateTime startDate)
    {
        var r = annualRatePercent / 100m / 12m;
        var payment = MonthlyPayment(principal, annualRatePercent, tenorMonths);
        var balance = principal;
        var schedule = new List<CreditInstallment>(tenorMonths);

        for (var no = 1; no <= tenorMonths; no++)
        {
            var interest = Math.Round(balance * r, 2, MidpointRounding.AwayFromZero);
            var principalPart = no == tenorMonths ? balance : payment - interest;
            balance -= principalPart;

            schedule.Add(new CreditInstallment
            {
                InstallmentNo = no,
                DueDate = startDate.Date.AddMonths(no),
                Amount = principalPart + interest
            });
        }

        return schedule;
    }
}
