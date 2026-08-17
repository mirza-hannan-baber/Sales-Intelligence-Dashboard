namespace SalesIntelligence.Api.Utilities
{

    public static class ReportingPeriod
    {
        public const double MaturityThreshold = 0.6;

        public const decimal ImplausibleChangePercent = 100m;

        public static int LastCompleteIndex(
            IReadOnlyList<(DateTime EndExclusive, double Volume)> periods,
            DateTime observedThrough)
        {
            if (periods.Count == 0) return -1;

            var last = periods.Count - 1;
            while (last >= 0 && periods[last].EndExclusive > observedThrough)
            {
                last--;
            }

            while (last > 0)
            {
                var baseline = Median(periods.Take(last).Select(p => p.Volume).ToList());
                if (baseline <= 0) break;
                if (periods[last].Volume >= baseline * MaturityThreshold) break;
                last--;
            }

            return last;
        }

        public static decimal? PercentChange(decimal previous, decimal current)
        {
            if (previous <= 0) return null;
            return Math.Round((current - previous) / previous * 100m, 1);
        }

        public static bool IsImplausible(decimal? changePercent) =>
            changePercent.HasValue && Math.Abs(changePercent.Value) > ImplausibleChangePercent;

        private static double Median(List<double> values)
        {
            if (values.Count == 0) return 0;
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 1
                ? sorted[mid]
                : (sorted[mid - 1] + sorted[mid]) / 2.0;
        }
    }
}
