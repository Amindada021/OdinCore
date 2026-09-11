using System;

namespace OdinCore.Tools
{
    public class PagedResult<T> : PagedResult where T : class
    {
        public T[] items { get; set; } = System.Array.Empty<T>();
    }

    public abstract class PagedResult
    {
        public int page { get; set; }

        public int pageSize { get; set; }

        public int total { get; set; }

        public int pageCount => (int)Math.Ceiling((double)total / (double)pageSize);

        public int firstIndex => (page - 1) * pageSize + 1;

        public int lastIndex => Math.Min(page * pageSize, total);

        public Dictionary<string, decimal>? BroughtForwardSums { get; set; }
        public Dictionary<string, decimal>? TotalSums { get; set; }
    }

}
