using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;

namespace OdinCore.Tools;

public class PagingException : Exception
{
    public PagingException(string message) : base(message) { }
}
public static class PagingExtension
{
    public static PagedResult<T> ToPaged<T>(
        this IQueryable<T> query,
        IPagedRequest request,
        params (string Key, Expression<Func<T, decimal>> Selector)[] carrySums) where T : class
    {
        var page = request.page ?? 0;
        var pageSize = request.pageSize ?? 0;

        if (page < 1)
            throw new PagingException("شماره صفحه نامعتبر است.");

        if (pageSize < 1)
            throw new PagingException("تعداد آیتم در صفحه نامعتبر است.");

        var total = query.Count();
        var pageCount = (int)Math.Ceiling((double)total / pageSize);

        if (total > 0 && page > pageCount)
            throw new PagingException($"شماره صفحه درخواستی ({page}) بیشتر از تعداد صفحات موجود ({pageCount}) است.");

        long skipLong = (long)(page - 1) * pageSize;
        var skip = skipLong > int.MaxValue ? int.MaxValue : (int)skipLong;

        var result = new PagedResult<T>
        {
            page = page,
            pageSize = pageSize,
            total = total,
            items = query.Skip(skip).Take(pageSize).ToArray()
        };

        if (carrySums is { Length: > 0 })
        {
            result.BroughtForwardSums = new Dictionary<string, decimal>();
            result.TotalSums = new Dictionary<string, decimal>();

            foreach (var (key, selector) in carrySums)
            {
                var carryOver = skip > 0 ? query.Take(skip).Sum(selector) : 0m;
                var runningTotal = query.Take(skip + pageSize > 0 ? skip + pageSize : int.MaxValue).Sum(selector);

                result.BroughtForwardSums[key] = carryOver;
                result.TotalSums[key] = runningTotal;
            }
        }

        return result;
    }
}