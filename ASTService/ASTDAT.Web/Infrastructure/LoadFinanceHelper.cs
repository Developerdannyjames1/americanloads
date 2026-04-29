using System;

namespace ASTDAT.Web.Infrastructure
{
    public static class LoadFinanceHelper
    {
        public static decimal? Profit(decimal? billedToCustomer, decimal? payToCarrier)
        {
            if (!billedToCustomer.HasValue && !payToCarrier.HasValue)
                return null;
            return (billedToCustomer ?? 0m) - (payToCarrier ?? 0m);
        }

        public static decimal? MarginPercent(decimal? billedToCustomer, decimal? payToCarrier)
        {
            if (!billedToCustomer.HasValue || billedToCustomer.Value == 0m)
                return null;
            var profit = Profit(billedToCustomer, payToCarrier);
            if (!profit.HasValue)
                return null;
            return Math.Round(profit.Value / billedToCustomer.Value * 100m, 2, MidpointRounding.AwayFromZero);
        }
    }
}
