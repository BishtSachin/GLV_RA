// In Data/Models/PagedResult.cs
namespace GoldLoanReappraisal.Data.Models
{
    // This is a generic class that can hold a paged result for any type of data
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
    }
}