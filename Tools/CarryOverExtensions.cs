using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace OdinCore.Tools
{
    public static class CarryOverExtensions
    {
        public static Dictionary<string, decimal> GetCarryOverSums<TEntity>(
            this IQueryable<TEntity> orderedQuery,
            int skip,
            params (string Key, Expression<Func<TEntity, decimal>> Selector)[] sums)
        {
            var result = new Dictionary<string, decimal>();

            if (sums == null || sums.Length == 0)
                return result;

            if (skip <= 0)
            {
                foreach (var s in sums)
                    result[s.Key] = 0;
                return result;
            }

            var beforePage = orderedQuery.Take(skip);

            foreach (var (key, selector) in sums)
                result[key] = beforePage.Sum(selector);

            return result;
        }

        public static Dictionary<string, decimal> AddRunningTotals(
            Dictionary<string, decimal> carryOver,
            Dictionary<string, decimal> pageSums)
        {
            var result = new Dictionary<string, decimal>();
            foreach (var key in carryOver.Keys)
                result[key] = carryOver[key] + (pageSums.TryGetValue(key, out var v) ? v : 0);
            return result;
        }
    }
}
