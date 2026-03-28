using System;
using BakeryAutomation.Models;

namespace BakeryAutomation.Services
{
    public sealed class CreditLimitEvaluation
    {
        public decimal CreditLimit { get; init; }
        public decimal ProjectedBalance { get; init; }
        public bool HasCreditLimit => CreditLimit > 0;
        public bool ExceedsLimit => HasCreditLimit && ProjectedBalance > CreditLimit;
        public decimal RemainingCredit => HasCreditLimit ? CreditLimit - ProjectedBalance : decimal.MaxValue;
    }

    public sealed class BranchPolicyService
    {
        public CreditLimitEvaluation EvaluateCreditLimit(Branch branch, decimal projectedBalance)
        {
            return new CreditLimitEvaluation
            {
                CreditLimit = branch.CreditLimit,
                ProjectedBalance = projectedBalance
            };
        }

        public string BuildCreditLimitWarning(
            Branch branch,
            CreditLimitEvaluation evaluation,
            DateTime referenceDate)
        {
            var termsSummary = FormatTermsSummary(branch);
            var nextCollectionDate = GetNextCollectionDate(branch, referenceDate);
            var nextCollectionText = nextCollectionDate.HasValue
                ? $"\nSonraki tahsilat: {nextCollectionDate:yyyy-MM-dd}"
                : string.Empty;
            var termsText = string.IsNullOrWhiteSpace(termsSummary)
                ? string.Empty
                : $"\nVade: {termsSummary}";

            return
                $"'{branch.Name}' kredi limitini asiyor.\n" +
                $"Mevcut limit: {evaluation.CreditLimit:n2}\n" +
                $"Kayit sonrasi bakiye: {evaluation.ProjectedBalance:n2}\n" +
                $"Asim tutari: {(evaluation.ProjectedBalance - evaluation.CreditLimit):n2}\n" +
                $"{termsText}" +
                $"{nextCollectionText}\n\nDevam etmek istiyor musunuz?";
        }

        public string FormatTermsSummary(Branch branch)
        {
            var paymentTerms = (branch.PaymentTerms ?? string.Empty).Trim();

            if (branch.PaymentDayOfMonth.HasValue && !string.IsNullOrWhiteSpace(paymentTerms))
            {
                return $"{paymentTerms} / Ayin {branch.PaymentDayOfMonth.Value}. gunu";
            }

            if (branch.PaymentDayOfMonth.HasValue)
            {
                return $"Ayin {branch.PaymentDayOfMonth.Value}. gunu";
            }

            return paymentTerms;
        }

        public DateTime? GetNextCollectionDate(Branch branch, DateTime referenceDate)
        {
            if (!branch.PaymentDayOfMonth.HasValue)
            {
                return null;
            }

            var day = branch.PaymentDayOfMonth.Value;
            var month = referenceDate.Month;
            var year = referenceDate.Year;
            var candidate = BuildDate(year, month, day);

            if (candidate < referenceDate.Date)
            {
                var nextMonth = referenceDate.AddMonths(1);
                candidate = BuildDate(nextMonth.Year, nextMonth.Month, day);
            }

            return candidate;
        }

        private static DateTime BuildDate(int year, int month, int day)
        {
            var safeDay = Math.Min(day, DateTime.DaysInMonth(year, month));
            return new DateTime(year, month, safeDay);
        }
    }
}
